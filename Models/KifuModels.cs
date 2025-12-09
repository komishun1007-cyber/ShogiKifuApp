using System;
using System.Collections.Generic;  // ← 追加

namespace ShogiKifuApp.Models;

public class KifuModel
{
    public string Title { get; set; } = "";
    public string Sente { get; set; } = "";
    public string Gote { get; set; } = "";
    public DateTime Date { get; set; } = DateTime.Now;
    public string RawText { get; set; } = "";
    public List<Move> Moves { get; set; } = new();
}