using System.Text.RegularExpressions;
using ShogiKifuApp.Models;

namespace ShogiKifuApp.Parsers;

public static class KifuParser
{
    // 全角数字マップ '１'..'９' -> 1..9
    static readonly Dictionary<char,int> zenkakuDigits = new()
    {
        ['１'] = 1, ['２'] = 2, ['３'] = 3, ['４'] = 4, ['５'] = 5,
        ['６'] = 6, ['７'] = 7, ['８'] = 8, ['９'] = 9
    };

    public static KifuModel Parse(string text)
    {
        var model = new KifuModel { RawText = text };

        // ヘッダ抽出（ゆるいマッチ）
        var mSente = Regex.Match(text, @"先手[:：]\s*(.+)");
        var mGote = Regex.Match(text, @"後手[:：]\s*(.+)");
        var mDate = Regex.Match(text, @"開始日時[:：]\s*(.+)");

        if (mSente.Success) model.Sente = mSente.Groups[1].Value.Trim();
        if (mGote.Success) model.Gote = mGote.Groups[1].Value.Trim();
        if (mDate.Success && DateTime.TryParse(mDate.Groups[1].Value.Trim(), out var d)) model.Date = d;
        model.Title = $"{model.Date:yyyy/MM/dd} {model.Sente} vs {model.Gote}";

        // 手の抽出：行ごとに "1 ７六歩(77)" みたいな行を拾う
        var lines = text.Split(new[] { '\r','\n' }, StringSplitOptions.RemoveEmptyEntries);
        var moveRegex = new Regex(@"^\s*\d+\s+(.+)$"); // 行頭の手番番号を無視して残りを取る
        foreach (var line in lines)
        {
            var mm = moveRegex.Match(line);
            string body = mm.Success ? mm.Groups[1].Value.Trim() : line.Trim();

            // 例: "７六歩(77)" または "７六成銀(33)"
            // 先に原文を保存
            var raw = body;

            // 目的地の全角数字を探す（例: '７六'）
            var destMatch = Regex.Match(body, @"([１２３４５６７８９])([一二三四五六七八九]|[１２３４５６７８９])"); 
            // note: second digit sometimes kanji; we'll also try parentheses
            int toFile = -1, toRank = -1;
            if (destMatch.Success)
            {
                var fch = destMatch.Groups[1].Value[0];
                var rch = destMatch.Groups[2].Value[0];
                if (zenkakuDigits.TryGetValue(fch, out var f)) toFile = f;
                // rank may be kanji '六' etc convert simple map:
                var kanjiMap = new Dictionary<char,int>
                {
                    ['一']=1,['二']=2,['三']=3,['四']=4,['五']=5,['六']=6,['七']=7,['八']=8,['九']=9
                };
                if (zenkakuDigits.TryGetValue(rch, out var r)) toRank = r;
                else if (kanjiMap.TryGetValue(rch, out var r2)) toRank = r2;
            }

            // origin in parentheses like "(77)" or "(7七)"
            int? fromFile = null, fromRank = null;
            var originMatch = Regex.Match(body, @"\((\d)(\d)\)|\(([^)]{2})\)");
            if (originMatch.Success)
            {
                if (originMatch.Groups[1].Success && originMatch.Groups[2].Success)
                {
                    if (int.TryParse(originMatch.Groups[1].Value, out var ff) && int.TryParse(originMatch.Groups[2].Value, out var rr))
                    {
                        fromFile = ff; fromRank = rr;
                    }
                }
                else
                {
                    var two = originMatch.Groups[3].Value;
                    if (two.Length==2)
                    {
                        if (zenkakuDigits.TryGetValue(two[0], out var ff) && kanjiToInt(two[1], out var rr))
                        {
                            fromFile = ff; fromRank = rr;
                        }
                    }
                }
            }

            // piece: the character after destination, like "歩","飛" etc
            var pieceMatch = Regex.Match(body, @"[１２３４５６７８９一二三四五六七八九]\s*([^\d\(成不打寄])");
            string piece = "";
            if (pieceMatch.Success) piece = pieceMatch.Groups[1].Value.Trim();
            else
            {
                // try simple: the last non-paren kanji
                var pm = Regex.Match(body, @"([歩香桂銀金角飛王玉成])");
                if (pm.Success) piece = pm.Groups[1].Value;
            }

            var promote = body.Contains("成") || body.Contains("成り") || body.Contains("+");

            // if toFile/toRank weren't found, try capturing fullwidth two-chars like '７六'
            if ((toFile==-1 || toRank==-1))
            {
                var alt = Regex.Match(body, @"([１２３４５６７８９])([一二三四五六七八九])");
                if (alt.Success)
                {
                    if (zenkakuDigits.TryGetValue(alt.Groups[1].Value[0], out var ff)) toFile = ff;
                    if (kanjiToInt(alt.Groups[2].Value[0], out var rr)) toRank = rr;
                }
            }

            if (toFile>0 && toRank>0)
            {
                var move = new Move
                {
                    FromFile = fromFile,
                    FromRank = fromRank,
                    ToFile = toFile,
                    ToRank = toRank,
                    Piece = piece,
                    IsPromotion = promote,
                    Raw = raw
                };
                model.Moves.Add(move);
            }
        }

        return model;
    }

    static bool kanjiToInt(char c, out int v)
    {
        var map = new Dictionary<char, int>
        {
            ['一'] = 1,
            ['二'] = 2,
            ['三'] = 3,
            ['四'] = 4,
            ['五'] = 5,
            ['六'] = 6,
            ['七'] = 7,
            ['八'] = 8,
            ['九'] = 9
        };
        if (map.TryGetValue(c, out v)) return true;
        v = 0; return false;
    }
    
    private static Move ParseMove(string text)
{
    // 例: "７六歩(77)" → to=(7,6), from=(7,7), piece="歩"
    var regex = new Regex(@"([１-９])([一二三四五六七八九])(.+)\((\d)(\d)\)");
    var match = regex.Match(text);
    if (!match.Success) return new Move();

    int toX = "１２３４５６７８９".IndexOf(match.Groups[1].Value) + 1;
    int toY = "一二三四五六七八九".IndexOf(match.Groups[2].Value) + 1;
    int fromX = int.Parse(match.Groups[4].Value);
    int fromY = int.Parse(match.Groups[5].Value);
    string piece = match.Groups[3].Value.Replace("成", "").Trim();

    return new Move
    {
        FromX = fromX,
        FromY = fromY,
        ToX = toX,
        ToY = toY,
        Piece = piece,
        IsPromote = text.Contains("成")
    };
}
}
