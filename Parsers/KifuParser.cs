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
        public List<string> Moves { get; set; } = new();
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
        var lines = Regex.Split(text, @"(?=\n?\s*\d+\s)");

        bool inMoveSection = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            if (string.IsNullOrWhiteSpace(line))
            {
                if (!inMoveSection && kifData.Header.Count > 0)
                    inMoveSection = true;

                continue;
            }

            if (char.IsDigit(line[0]) || IsZenkakuDigit(line[0]))
            {
                inMoveSection = true;
                kifData.Moves.Add(line);
                continue;
            }

            if (!inMoveSection)
                ParseHeaderLine(line, kifData.Header);
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
            model.Sente = sente;
        else if (kifData.Header.TryGetValue("下手", out sente))
            model.Sente = sente;

        if (kifData.Header.TryGetValue("後手", out var gote))
            model.Gote = gote;
        else if (kifData.Header.TryGetValue("上手", out gote))
            model.Gote = gote;

        if (kifData.Header.TryGetValue("開始日時", out var dateStr))
        {
            if (DateTime.TryParse(dateStr, out var date))
                model.Date = date;
        }

        model.Title = !string.IsNullOrEmpty(model.Sente) && !string.IsNullOrEmpty(model.Gote)
            ? $"{model.Date:yyyy/MM/dd} {model.Sente} vs {model.Gote}"
            : $"{model.Date:yyyy/MM/dd}";
    }

    private static void ExtractMovesFromList(KifData kifData, KifuModel model)
    {
        foreach (var moveText in kifData.Moves)
        {
            var move = ParseSingleMove(moveText);
            if (move != null)
                model.Moves.Add(move);
        }
    }

    private static Move? ParseSingleMove(string text)
    {
        try
        {
            var match = Regex.Match(text, @"^\s*(\d+)\s+(.+)$");
            if (!match.Success)
                return null;

            var moveText = match.Groups[2].Value.Trim();

            if (moveText.Contains("投了") ||
                moveText.Contains("中断") ||
                moveText.Contains("時間切れ") ||
                moveText.Contains("切れ負け"))
                return null;

            var move = new Move { Raw = text };

            var destPattern = @"^([１-９])([一二三四五六七八九１-９])|^同";
            var destMatch = Regex.Match(moveText, destPattern);

            if (!destMatch.Success && !moveText.StartsWith("同"))
                return null;

            if (moveText.StartsWith("同"))
                return null;

            var fileChar = destMatch.Groups[1].Value[0];
            var rankChar = destMatch.Groups[2].Value[0];

            if (!TryConvertToNumber(fileChar, out var toFile) ||
                !TryConvertToNumber(rankChar, out var toRank))
                return null;

            move.ToFile = toFile;
            move.ToRank = toRank;

            var pieceMatch = Regex.Match(moveText, @"[歩香桂銀金角飛王玉と杏圭全馬龍]");
            if (pieceMatch.Success)
                move.Piece = pieceMatch.Value;

            move.IsPromotion = moveText.Contains("成");

            var fromPattern = @"\((\d)(\d)\)|\(([１-９])([一二三四五六七八九])\)";
            var fromMatch = Regex.Match(moveText, fromPattern);

            if (fromMatch.Success)
            {
                if (!string.IsNullOrEmpty(fromMatch.Groups[1].Value))
                {
                    if (int.TryParse(fromMatch.Groups[1].Value, out var fromFile) &&
                        int.TryParse(fromMatch.Groups[2].Value, out var fromRank))
                    {
                        move.FromFile = fromFile;
                        move.FromRank = fromRank;
                    }
                }
                else if (!string.IsNullOrEmpty(fromMatch.Groups[3].Value))
                {
                    var fromFileChar = fromMatch.Groups[3].Value[0];
                    var fromRankChar = fromMatch.Groups[4].Value[0];

                    if (TryConvertToNumber(fromFileChar, out var fromFile) &&
                        TryConvertToNumber(fromRankChar, out var fromRank))
                    {
                        move.FromFile = fromFile;
                        move.FromRank = fromRank;
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

    private static bool IsZenkakuDigit(char c)
    {
        return ZenkakuDigits.ContainsKey(c);
    }
}
