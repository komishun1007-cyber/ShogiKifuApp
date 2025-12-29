using System.Text.RegularExpressions;
using System.Collections.Generic;
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

    public class KifData
    {
        public Dictionary<string, string> Header { get; set; } = new();
        public List<MoveWithComment> Moves { get; set; } = new();
    }

    public class MoveWithComment
    {
        public string MoveText { get; set; } = "";
        public List<string> Comments { get; set; } = new();
        public int MoveNumber { get; set; }
    }

    public static KifuModel Parse(string text)
    {
        var kifData = ParseRaw(text);
        var model = new KifuModel { RawText = text };

        ExtractHeaderToModel(kifData, model);
        ExtractMovesFromList(kifData, model);

        return model;
    }

    public static KifData ParseRaw(string text)
    {
        var kifData = new KifData();
        
        // 改行コードを統一
        text = text.Replace("\r\n", "\n").Replace("\r", "\n");
        var lines = text.Split('\n');

        bool inMoveSection = false;
        MoveWithComment? currentMove = null;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (string.IsNullOrWhiteSpace(trimmed))
            {
                if (!inMoveSection && kifData.Header.Count > 0)
                    inMoveSection = true;
                continue;
            }

            // コメント行の処理
            if (trimmed.StartsWith("*") || trimmed.StartsWith("#"))
            {
                if (currentMove != null)
                {
                    // 直前の指し手に関連するコメント
                    currentMove.Comments.Add(trimmed);
                }
                continue;
            }

            // 指し手行の判定（手数で始まる）
            var moveMatch = Regex.Match(trimmed, @"^\s*(\d+)\s+(.+?)(?:\s+\([\d:]+/[\d:]+\))?$");
            if (moveMatch.Success)
            {
                inMoveSection = true;
                
                var moveNumber = int.Parse(moveMatch.Groups[1].Value);
                var moveText = moveMatch.Groups[2].Value.Trim();
                
                // 「投了」などの終了記号は除外
                if (!moveText.Contains("投了") && 
                    !moveText.Contains("中断") && 
                    !moveText.Contains("時間切れ") &&
                    !moveText.Contains("切れ負け"))
                {
                    currentMove = new MoveWithComment
                    {
                        MoveNumber = moveNumber,
                        MoveText = moveText
                    };
                    kifData.Moves.Add(currentMove);
                }
                continue;
            }

            // ヘッダー行の処理
            if (!inMoveSection)
            {
                ParseHeaderLine(trimmed, kifData.Header);
            }
        }

        return kifData;
    }

    private static void ParseHeaderLine(string line, Dictionary<string, string> header)
    {
        var separators = new[] { '：', ':' };
        var separatorIndex = -1;

        foreach (var sep in separators)
        {
            var idx = line.IndexOf(sep);
            if (idx > 0)
            {
                separatorIndex = idx;
                break;
            }
        }

        if (separatorIndex > 0)
        {
            var key = line.Substring(0, separatorIndex).Trim();
            var value = line.Substring(separatorIndex + 1).Trim();

            if (!string.IsNullOrWhiteSpace(key))
                header[key] = value;
        }
    }

    private static void ExtractHeaderToModel(KifData kifData, KifuModel model)
    {
        if (kifData.Header.TryGetValue("先手", out var sente))
            model.Sente = ExtractName(sente);
        else if (kifData.Header.TryGetValue("下手", out sente))
            model.Sente = ExtractName(sente);

        if (kifData.Header.TryGetValue("後手", out var gote))
            model.Gote = ExtractName(gote);
        else if (kifData.Header.TryGetValue("上手", out gote))
            model.Gote = ExtractName(gote);

        if (kifData.Header.TryGetValue("開始日時", out var dateStr))
        {
            if (DateTime.TryParse(dateStr, out var date))
                model.Date = date;
        }

        model.Title = !string.IsNullOrEmpty(model.Sente) && !string.IsNullOrEmpty(model.Gote)
            ? $"{model.Date:yyyy/MM/dd} {model.Sente} vs {model.Gote}"
            : $"{model.Date:yyyy/MM/dd}";
    }

    private static string ExtractName(string fullText)
    {
        // 「名前 段位」形式から名前だけを抽出
        var match = Regex.Match(fullText, @"^([^\s]+)");
        return match.Success ? match.Groups[1].Value : fullText;
    }

    private static void ExtractMovesFromList(KifData kifData, KifuModel model)
    {
        foreach (var moveWithComment in kifData.Moves)
        {
            var move = ParseSingleMove(moveWithComment.MoveText);
            if (move != null)
            {
                move.Raw = moveWithComment.MoveText;
                move.Comments = moveWithComment.Comments;
                model.Moves.Add(move);
            }
        }
    }

    private static Move? ParseSingleMove(string text)
    {
        try
        {
            // 括弧内の情報を除去
            text = Regex.Replace(text, @"\([^)]+\)", "").Trim();

            var move = new Move { Raw = text };

            // 「同」の処理
            if (text.StartsWith("同"))
            {
                move.IsSame = true;
                
                // 駒の種類を抽出
                var samePieceMatch = Regex.Match(text, @"同\s*([歩香桂銀金角飛王玉と杏圭全馬龍])");
                if (samePieceMatch.Success)
                    move.Piece = samePieceMatch.Value;
                
                move.IsPromotion = text.Contains("成");
                return move;
            }

            // 移動先の座標を抽出
            var destPattern = @"^([１-９])([一二三四五六七八九])";
            var destMatch = Regex.Match(text, destPattern);

            if (!destMatch.Success)
                return null;

            var fileChar = destMatch.Groups[1].Value[0];
            var rankChar = destMatch.Groups[2].Value[0];

            if (!TryConvertToNumber(fileChar, out var toFile) ||
                !TryConvertToNumber(rankChar, out var toRank))
                return null;

            move.ToFile = toFile;
            move.ToRank = toRank;

            // 駒の種類を抽出
            var pieceMatch = Regex.Match(text, @"[歩香桂銀金角飛王玉と杏圭全馬龍]");
            if (pieceMatch.Success)
                move.Piece = pieceMatch.Value;

            // 成りの判定
            move.IsPromotion = text.Contains("成");

            // 移動元の座標を抽出
            var fromPattern = @"\((\d)(\d)\)";
            var fromMatch = Regex.Match(text, fromPattern);

            if (fromMatch.Success)
            {
                if (int.TryParse(fromMatch.Groups[1].Value, out var fromFile) &&
                    int.TryParse(fromMatch.Groups[2].Value, out var fromRank))
                {
                    move.FromFile = fromFile;
                    move.FromRank = fromRank;
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

    private static bool IsZenkakuDigit(char c)
    {
        return ZenkakuDigits.ContainsKey(c);
    }
}