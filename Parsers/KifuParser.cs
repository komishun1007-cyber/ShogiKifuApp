using System.Text.RegularExpressions;
using System.Collections.Generic;  // ← 追加
using ShogiKifuApp.Models;

namespace ShogiKifuApp.Parsers;

public static class KifuParser
{
    private static readonly Dictionary<char, int> ZenkakuDigits = new()
    {
        ['１'] = 1, ['２'] = 2, ['３'] = 3, ['４'] = 4, ['５'] = 5,
        ['６'] = 6, ['７'] = 7, ['８'] = 8, ['９'] = 9
    };

    private static readonly Dictionary<char, int> KanjiDigits = new()
    {
        ['一'] = 1, ['二'] = 2, ['三'] = 3, ['四'] = 4, ['五'] = 5,
        ['六'] = 6, ['七'] = 7, ['八'] = 8, ['九'] = 9
    };

    public static KifuModel Parse(string text)
    {
        var model = new KifuModel { RawText = text };

        // ヘッダー情報の抽出
        ExtractHeader(text, model);
        
        // 指し手の抽出
        ExtractMoves(text, model);

        return model;
    }

    private static void ExtractHeader(string text, KifuModel model)
    {
        // 先手・後手
        var mSente = Regex.Match(text, @"先手[:：]\s*(.+)", RegexOptions.Multiline);
        var mGote = Regex.Match(text, @"後手[:：]\s*(.+)", RegexOptions.Multiline);
        var mDate = Regex.Match(text, @"開始日時[:：]\s*(.+)", RegexOptions.Multiline);

        if (mSente.Success) 
            model.Sente = mSente.Groups[1].Value.Trim();
        if (mGote.Success) 
            model.Gote = mGote.Groups[1].Value.Trim();
        if (mDate.Success && DateTime.TryParse(mDate.Groups[1].Value.Trim(), out var d)) 
            model.Date = d;

        model.Title = $"{model.Date:yyyy/MM/dd} {model.Sente} vs {model.Gote}";
    }

    private static void ExtractMoves(string text, KifuModel model)
    {
        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        
        // "1 ７六歩(77)" のような行を抽出
        var movePattern = new Regex(@"^\s*(\d+)[\s\.]+(.+?)(?:\s*\(|$)");

        foreach (var line in lines)
        {
            var match = movePattern.Match(line);
            if (!match.Success) continue;

            var moveText = match.Groups[2].Value.Trim();
            var move = ParseSingleMove(moveText);
            
            if (move != null)
                model.Moves.Add(move);
        }
    }

    private static Move? ParseSingleMove(string text)
    {
        try
        {
            var move = new Move { Raw = text };

            // "７六歩(77)" または "同　歩成(32)" のパターン
            // 目的地: 全角数字2文字 or "同"
            var destPattern = @"^([１-９])([一二三四五六七八九１-９])|^同";
            var destMatch = Regex.Match(text, destPattern);

            if (!destMatch.Success && !text.StartsWith("同"))
                return null;

            // 目的地の座標
            if (text.StartsWith("同"))
            {
                // "同"の場合は前の手の座標を使う（ここでは未実装なのでスキップ）
                return null;
            }

            var fileChar = destMatch.Groups[1].Value[0];
            var rankChar = destMatch.Groups[2].Value[0];

            if (!TryConvertToNumber(fileChar, out var toFile) || 
                !TryConvertToNumber(rankChar, out var toRank))
                return null;

            move.ToFile = toFile;
            move.ToRank = toRank;

            // 駒の種類を抽出（"歩", "飛", "角" など）
            var pieceMatch = Regex.Match(text, @"[歩香桂銀金角飛王玉と杏圭全馬龍]");
            if (pieceMatch.Success)
                move.Piece = pieceMatch.Value;

            // 成り判定
            move.IsPromotion = text.Contains("成");

            // 移動元 "(77)" や "(7七)" のパターン
            var fromPattern = @"\((\d)(\d)\)|\(([１-９])([一二三四五六七八九])\)";
            var fromMatch = Regex.Match(text, fromPattern);

            if (fromMatch.Success)
            {
                if (fromMatch.Groups[1].Success) // 半角数字
                {
                    move.FromFile = int.Parse(fromMatch.Groups[1].Value);
                    move.FromRank = int.Parse(fromMatch.Groups[2].Value);
                }
                else if (fromMatch.Groups[3].Success) // 全角
                {
                    if (TryConvertToNumber(fromMatch.Groups[3].Value[0], out var ff) &&
                        TryConvertToNumber(fromMatch.Groups[4].Value[0], out var rr))
                    {
                        move.FromFile = ff;
                        move.FromRank = rr;
                    }
                }
            }

            return move;
        }
        catch
        {
            return null;
        }
    }

    private static bool TryConvertToNumber(char c, out int value)
    {
        if (ZenkakuDigits.TryGetValue(c, out value))
            return true;
        if (KanjiDigits.TryGetValue(c, out value))
            return true;
        if (char.IsDigit(c))
        {
            value = c - '0';
            return true;
        }
        value = 0;
        return false;
    }
}