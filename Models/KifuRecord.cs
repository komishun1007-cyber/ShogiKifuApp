using SQLite;

namespace ShogiKifuApp.Models;

[Table("kifu_records")]
public class KifuRecord
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public DateTime Date { get; set; } = DateTime.Now;

    public string Sente { get; set; } = "";
    public string Gote { get; set; } = "";
    
    public int Moves { get; set; }
    public string Result { get; set; } = "";
    public string Winner { get; set; } = ""; // "先手", "後手", ""
    public string KifuText { get; set; } = "";
    public string Notes { get; set; } = "";
    public string Title { get; set; } = "";

    [Ignore]
    public string DisplayTitle => string.IsNullOrEmpty(Title)
        ? (string.IsNullOrEmpty(Sente) || string.IsNullOrEmpty(Gote)
            ? $"{Date:yyyy/MM/dd}"
            : $"{Sente} vs {Gote}")
        : Title;
    
    [Ignore]
    public string Subtitle
    {
        get
        {
            var parts = new List<string>
            {
                $"{Date:yyyy/MM/dd}",
                $"{Moves}手"
            };
            
            if (!string.IsNullOrEmpty(Winner))
            {
                parts.Add($"{Winner}勝ち");
            }
            else if (!string.IsNullOrEmpty(Result))
            {
                parts.Add(Result);
            }
            
            return string.Join(" - ", parts);
        }
    }
    
    [Ignore]
    public string WinnerSymbol => Winner switch
    {
        "先手" => "▲",
        "後手" => "△",
        _ => ""
    };
}