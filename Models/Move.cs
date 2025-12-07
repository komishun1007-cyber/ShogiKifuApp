namespace ShogiKifuApp.Models;

public class Move
{
    // 1..9 ファイル（横）、1..9 段（縦）
    public int? FromFile { get; set; }
    public int? FromRank { get; set; }

    public int ToFile { get; set; }
    public int ToRank { get; set; }

    public string Piece { get; set; } = ""; // 例: "歩","飛","角","金" 等
    public bool IsPromotion { get; set; } = false;
    public string Raw { get; set; } = ""; // 元のテキスト行

        public int FromX { get; set; }
    public int FromY { get; set; }
    public int ToX { get; set; }
    public int ToY { get; set; }
    public bool IsPromote { get; set; } = false;
}
