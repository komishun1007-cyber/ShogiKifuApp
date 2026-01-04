using SQLite;

namespace ShogiKifuApp.Models;

[Table("user_profiles")]
public class UserProfile
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public string UserName { get; set; } = "";
    
    public bool IsActive { get; set; } = false; // 現在選択中のユーザー
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    public string Notes { get; set; } = ""; // メモ(例: "将棋ウォーズ用")
}