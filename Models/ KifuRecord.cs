// File: Models/KifuRecord.cs
using SQLite;

namespace ShogiKifuApp.Models;

[Table("kifu_records")]
public class KifuRecord
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public DateTime Date { get; set; } = DateTime.Today;

    [Indexed]
    public string Black { get; set; } = "";   // 先手

    [Indexed]
    public string White { get; set; } = "";   // 後手

    public int Moves { get; set; }            // 手数
    public string Result { get; set; } = "";  // 例: "先手勝ち", "後手勝ち"
    public string Notes { get; set; } = "";


    public string Sente { get; set; } = ""; // 先手
    public string Gote { get; set; } = "";   // 後手
    public string KifuText { get; set; } = "";  // 棋譜本文

    [Ignore] public string Title { get; set; } = "";
    [Ignore] public string Subtitle => $"{Moves}手 - {Result}";

}
