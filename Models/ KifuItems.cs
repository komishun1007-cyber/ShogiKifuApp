using SQLite;

namespace ShogiKifuApp.Models
{
    public class KifuItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;// KIFデータ全体

        public DateTime Date { get; set; }

        public string Format { get; set; } = "KIF"; // 形式を保存（KIF, KIF2, JSON etc）
    }
}