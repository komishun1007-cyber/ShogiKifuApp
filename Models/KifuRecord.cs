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
    public string KifuText { get; set; } = "";
    public string Notes { get; set; } = "";

    // Titleを保存可能なプロパティに変更
    public string Title { get; set; } = "";

    // 表示用の読み取り専用プロパティ
    [Ignore]
    public string DisplayTitle => string.IsNullOrEmpty(Title)
        ? (string.IsNullOrEmpty(Sente) || string.IsNullOrEmpty(Gote)
            ? $"{Date:yyyy/MM/dd}"
            : $"{Sente} vs {Gote}")
        : Title;
    
    [Ignore]
    public string Subtitle => $"{Date:yyyy/MM/dd} - {Moves}手{(string.IsNullOrEmpty(Result) ? "" : " - " + Result)}";
}