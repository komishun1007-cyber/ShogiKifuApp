using ShogiKifuApp.Models;

namespace ShogiKifuApp.Services;

public class StatisticsService
{
    public GameStatistics CalculateStatistics(List<KifuRecord> records, string userName)
    {
        var stats = new GameStatistics();
        
        // ユーザーが参加している棋譜のみフィルター
        var userGames = records.Where(r => 
            r.Sente == userName || r.Gote == userName
        ).ToList();
        
        stats.TotalGames = userGames.Count;
        
        foreach (var game in userGames)
        {
            bool isUserSente = game.Sente == userName;
            bool isWin = (isUserSente && game.Winner == "先手") || 
                        (!isUserSente && game.Winner == "後手");
            
            if (isWin)
                stats.Wins++;
            else if (!string.IsNullOrEmpty(game.Winner))
                stats.Losses++;
            else
                stats.Draws++;
            
            // 棋戦別統計
            if (!string.IsNullOrEmpty(game.Tournament))
            {
                if (!stats.TournamentStats.ContainsKey(game.Tournament))
                {
                    stats.TournamentStats[game.Tournament] = new TournamentStats
                    {
                        TournamentName = game.Tournament
                    };
                }
                
                stats.TournamentStats[game.Tournament].Games++;
                if (isWin)
                    stats.TournamentStats[game.Tournament].Wins++;
            }
            
            // 戦法別統計
            string strategy = isUserSente ? game.SenteStrategy : game.GoteStrategy;
            if (!string.IsNullOrEmpty(strategy))
            {
                if (!stats.StrategyStats.ContainsKey(strategy))
                {
                    stats.StrategyStats[strategy] = new StrategyStats
                    {
                        StrategyName = strategy
                    };
                }
                
                stats.StrategyStats[strategy].Games++;
                if (isWin)
                    stats.StrategyStats[strategy].Wins++;
            }
            
            // 月別統計
            string month = game.Date.ToString("yyyy-MM");
            if (!stats.MonthlyStats.ContainsKey(month))
            {
                stats.MonthlyStats[month] = new MonthlyStats
                {
                    Month = month
                };
            }
            
            stats.MonthlyStats[month].Games++;
            if (isWin)
                stats.MonthlyStats[month].Wins++;
        }
        
        return stats;
    }
}