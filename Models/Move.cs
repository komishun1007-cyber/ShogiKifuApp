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
    
    // 「同」の手かどうか
    public bool IsSame { get; set; }
    
    // 元のテキスト（デバッグ用）
    public string Raw { get; set; } = "";
    
    // コメント（手筋など）
    public List<string> Comments { get; set; } = new();
    
    // 表示用の短縮形式（例：「2四歩」）
    public string ShortDisplay
    {
        get
        {
            if (IsSame)
                return $"同{Piece}{(IsPromotion ? "成" : "")}";
            
            return $"{ToFile}{ConvertRankToKanji(ToRank)}{Piece}{(IsPromotion ? "成" : "")}";
        }
    }
    
    private static string ConvertRankToKanji(int rank)
    {
        return rank switch
        {
            1 => "一",
            2 => "二",
            3 => "三",
            4 => "四",
            5 => "五",
            6 => "六",
            7 => "七",
            8 => "八",
            9 => "九",
            _ => rank.ToString()
        };
    }
    
    public override string ToString() 
        => ShortDisplay;
}