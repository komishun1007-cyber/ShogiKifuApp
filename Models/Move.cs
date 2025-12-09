namespace ShogiKifuApp.Models;

public class Move
{
    // 移動元（1-9, nullの場合は持ち駒）
    public int? FromFile { get; set; }
    public int? FromRank { get; set; }

    // 移動先（1-9）
    public int ToFile { get; set; }
    public int ToRank { get; set; }

    // 駒の種類
    public string Piece { get; set; } = "";
    
    // 成り・不成
    public bool IsPromotion { get; set; }
    
    // 元のテキスト（デバッグ用）
    public string Raw { get; set; } = "";
    
    public override string ToString() 
        => $"{ToFile}{ToRank}{Piece}{(IsPromotion ? "成" : "")}";
}