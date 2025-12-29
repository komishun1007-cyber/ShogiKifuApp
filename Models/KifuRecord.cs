using SQLite;

namespace ShogiKifuApp.Models;

[Table("kifu_records")]
public class KifuRecord
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public DateTime Date { get; set; } = DateTime.Now;
    
    // 追加：戦法
    public string SenteStrategy { get; set; } = ""; // 先手戦法
    public string GoteStrategy { get; set; } = ""; // 後手戦法

    public string Sente { get; set; } = "";
    public string Gote { get; set; } = "";
    
    public int Moves { get; set; }
    public string Result { get; set; } = "";
    public string Winner { get; set; } = "";
    public string KifuText { get; set; } = "";
    public string Notes { get; set; } = "";
    public string Title { get; set; } = "";
    
    // 追加フィールド
    public string Tournament { get; set; } = ""; // 棋戦名
    public string TimeControl { get; set; } = ""; // 持ち時間

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

// 一覧表示用の追加情報
    [Ignore]
    public string DetailInfo
    {
        get
        {
            var parts = new List<string>();
            
            if (!string.IsNullOrEmpty(Tournament))
                parts.Add(Tournament);
            
            if (!string.IsNullOrEmpty(TimeControl))
                parts.Add($"持ち時間: {TimeControl}");
            
            if (!string.IsNullOrEmpty(SenteStrategy))
                parts.Add($"▲{SenteStrategy}");
            
            if (!string.IsNullOrEmpty(GoteStrategy))
                parts.Add($"△{GoteStrategy}");
            
            return parts.Count > 0 ? string.Join(" / ", parts) : "";
        }
    }
}