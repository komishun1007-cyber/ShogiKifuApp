namespace ShogiKifuApp.Models;

public class GameStatistics
{
    public int TotalGames { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Draws { get; set; }
    
    public double WinRate => TotalGames > 0 ? (double)Wins / TotalGames * 100 : 0;
    
    public Dictionary<string, TournamentStats> TournamentStats { get; set; } = new();
    public Dictionary<string, StrategyStats> StrategyStats { get; set; } = new();
    public Dictionary<string, MonthlyStats> MonthlyStats { get; set; } = new();
}

public class TournamentStats
{
    public string TournamentName { get; set; } = "";
    public int Games { get; set; }
    public int Wins { get; set; }
    public double WinRate => Games > 0 ? (double)Wins / Games * 100 : 0;
}

public class StrategyStats
{
    public string StrategyName { get; set; } = "";
    public int Games { get; set; }
    public int Wins { get; set; }
    public double WinRate => Games > 0 ? (double)Wins / Games * 100 : 0;
}

public class MonthlyStats
{
    public string Month { get; set; } = ""; // "2025-01"形式
    public int Games { get; set; }
    public int Wins { get; set; }
    public double WinRate => Games > 0 ? (double)Wins / Games * 100 : 0;
}