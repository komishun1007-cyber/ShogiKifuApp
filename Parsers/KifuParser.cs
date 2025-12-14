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
        System.Console.WriteLine("[Parse] start");

        var kifData = ParseRaw(text);
        System.Console.WriteLine($"[Parse] ParseRaw done Header={kifData.Header.Count}, Moves={kifData.Moves.Count}");

        var model = new KifuModel { RawText = text };

        ExtractHeaderToModel(kifData, model);
        System.Console.WriteLine("[Parse] ExtractHeaderToModel done");

        ExtractMovesFromList(kifData, model);
        System.Console.WriteLine($"[Parse] ExtractMovesFromList done Moves={model.Moves.Count}");

        System.Console.WriteLine("[Parse] end");
        return model;
    }

    public static KifData ParseRaw(string text)
    {
        System.Console.WriteLine("[ParseRaw] start");

        var kifData = new KifData();
var lines = Regex.Split(text, @"(?=\n?\s*\d+\s)");
            System.Console.WriteLine($"[ParseRaw] line count={lines.Length}");

        bool inMoveSection = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            System.Console.WriteLine($"[ParseRaw] line='{line}'");

            if (string.IsNullOrWhiteSpace(line))
            {
                if (!inMoveSection && kifData.Header.Count > 0)
                {
                    inMoveSection = true;
                    System.Console.WriteLine("[ParseRaw] enter move section by empty line");
                }
                continue;
            }

            if (char.IsDigit(line[0]) || IsZenkakuDigit(line[0]))
            {
                inMoveSection = true;
                kifData.Moves.Add(line);
                System.Console.WriteLine($"[ParseRaw] add move '{line}'");
                continue;
            }

            if (!inMoveSection)
            {
                System.Console.WriteLine("[ParseRaw] header line");
                ParseHeaderLine(line, kifData.Header);
            }
        }

        System.Console.WriteLine($"[ParseRaw] end Header={kifData.Header.Count}, Moves={kifData.Moves.Count}");
        return kifData;
    }

    private static void ParseHeaderLine(string line, Dictionary<string, string> header)
    {
        System.Console.WriteLine($"[ParseHeaderLine] line='{line}'");

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
            System.Console.WriteLine($"[ParseHeaderLine] key='{key}', value='{value}'");

            if (!string.IsNullOrWhiteSpace(key))
                header[key] = value;
        }
        else
        {
            System.Console.WriteLine("[ParseHeaderLine] separator not found");
        }
    }

    private static void ExtractHeaderToModel(KifData kifData, KifuModel model)
    {
        System.Console.WriteLine("[ExtractHeaderToModel] start");

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

        System.Console.WriteLine("[ExtractHeaderToModel] end");
    }

    private static void ExtractMovesFromList(KifData kifData, KifuModel model)
    {
        System.Console.WriteLine("[ExtractMovesFromList] start");

        foreach (var moveText in kifData.Moves)
        {
            System.Console.WriteLine($"[ExtractMovesFromList] parse '{moveText}'");

            var move = ParseSingleMove(moveText);
            if (move != null)
            {
                model.Moves.Add(move);
                System.Console.WriteLine("[ExtractMovesFromList] added");
            }
            else
            {
                System.Console.WriteLine("[ExtractMovesFromList] null");
            }
        }

        System.Console.WriteLine("[ExtractMovesFromList] end");
    }

private static Move? ParseSingleMove(string text)
{
    System.Console.WriteLine($"[ParseSingleMove] start '{text}'");

    try
    {
        var match = Regex.Match(text, @"^\s*(\d+)\s+(.+)$");
        System.Console.WriteLine($"[ParseSingleMove] regex1 success={match.Success}");

        if (!match.Success)
            return null;

        var moveText = match.Groups[2].Value.Trim();
        System.Console.WriteLine($"[ParseSingleMove] moveText='{moveText}'");

        if (moveText.Contains("投了") || moveText.Contains("中断") ||
            moveText.Contains("時間切れ") || moveText.Contains("切れ負け"))
        {
            System.Console.WriteLine("[ParseSingleMove] end move");
            return null;
        }

        var move = new Move { Raw = text };

        var destPattern = @"^([１-９])([一二三四五六七八九１-９])|^同";
        var destMatch = Regex.Match(moveText, destPattern);
        System.Console.WriteLine($"[ParseSingleMove] destMatch success={destMatch.Success}");

        if (!destMatch.Success && !moveText.StartsWith("同"))
            return null;

        if (moveText.StartsWith("同"))
        {
            System.Console.WriteLine("[ParseSingleMove] 同 not implemented");
            return null;
        }

        var fileChar = destMatch.Groups[1].Value[0];
        var rankChar = destMatch.Groups[2].Value[0];
        System.Console.WriteLine($"[ParseSingleMove] to={fileChar}{rankChar}");

        if (!TryConvertToNumber(fileChar, out var toFile) ||
            !TryConvertToNumber(rankChar, out var toRank))
            return null;

        move.ToFile = toFile;
        move.ToRank = toRank;

        var pieceMatch = Regex.Match(moveText, @"[歩香桂銀金角飛王玉と杏圭全馬龍]");
        System.Console.WriteLine($"[ParseSingleMove] pieceMatch={pieceMatch.Success}");

        if (pieceMatch.Success)
            move.Piece = pieceMatch.Value;

        move.IsPromotion = moveText.Contains("成");

        // 移動元の座標をパース
        var fromPattern = @"\((\d)(\d)\)|\(([１-９])([一二三四五六七八九])\)";
        var fromMatch = Regex.Match(moveText, fromPattern);
        System.Console.WriteLine($"[ParseSingleMove] fromMatch={fromMatch.Success}");

        if (fromMatch.Success)
        {
            // 半角数字の場合
            if (!string.IsNullOrEmpty(fromMatch.Groups[1].Value))
            {
                if (int.TryParse(fromMatch.Groups[1].Value, out var fromFile) &&
                    int.TryParse(fromMatch.Groups[2].Value, out var fromRank))
                {
                    move.FromFile = fromFile;
                    move.FromRank = fromRank;
                    System.Console.WriteLine($"[ParseSingleMove] from={fromFile}{fromRank} (半角)");
                }
            }
            // 全角数字の場合
            else if (!string.IsNullOrEmpty(fromMatch.Groups[3].Value))
            {
                var fromFileChar = fromMatch.Groups[3].Value[0];
                var fromRankChar = fromMatch.Groups[4].Value[0];
                
                if (TryConvertToNumber(fromFileChar, out var fromFile) &&
                    TryConvertToNumber(fromRankChar, out var fromRank))
                {
                    move.FromFile = fromFile;
                    move.FromRank = fromRank;
                    System.Console.WriteLine($"[ParseSingleMove] from={fromFile}{fromRank} (全角)");
                }
            }
        }

        return move;
    }
    catch (Exception ex)
    {
        System.Console.WriteLine($"[ParseSingleMove] exception {ex}");
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
