using ShogiKifuApp.Models;
using ShogiKifuApp.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace ShogiKifuApp.Pages;

public partial class StatisticsPage : ContentPage
{
    private readonly UserProfile _user;
    private readonly StatisticsService _statsService = new StatisticsService();

    public StatisticsPage(UserProfile user)
    {
        InitializeComponent();
        _user = user;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadStatistics();
    }

    private async Task LoadStatistics()
    {
        try
        {
            UserNameLabel.Text = $"ユーザー: {_user.UserName}";
            
            var allRecords = await App.Database.GetAllAsync();
            var stats = _statsService.CalculateStatistics(allRecords, _user.UserName);
            
            // 基本統計
            TotalGamesLabel.Text = $"{stats.TotalGames}局";
            WinsLabel.Text = $"{stats.Wins}勝";
            LossesLabel.Text = $"{stats.Losses}敗";
            WinRateLabel.Text = $"{stats.WinRate:F1}%";
            
            // 棋戦別統計
            var tournamentStats = stats.TournamentStats.Values
                .OrderByDescending(t => t.Games)
                .ToList();
            TournamentStatsList.ItemsSource = tournamentStats;
            
            // 戦法別統計
            var strategyStats = stats.StrategyStats.Values
                .OrderByDescending(s => s.Games)
                .ToList();
            StrategyStatsList.ItemsSource = strategyStats;
            
            // 月別推移グラフ
            CreateMonthlyChart(stats);
        }
        catch (Exception ex)
        {
            await DisplayAlert("エラー", $"統計の読み込みに失敗しました:\n{ex.Message}", "OK");
        }
    }

    private void CreateMonthlyChart(GameStatistics stats)
    {
        if (stats.MonthlyStats.Count == 0)
        {
            MonthlyChart.Series = Array.Empty<ISeries>();
            return;
        }
        
        var monthlyData = stats.MonthlyStats.Values
            .OrderBy(m => m.Month)
            .ToList();
        
        var winRates = monthlyData.Select(m => m.WinRate).ToArray();
        var months = monthlyData.Select(m => m.Month).ToArray();
        
        MonthlyChart.Series = new ISeries[]
        {
            new LineSeries<double>
            {
                Values = winRates,
                Name = "勝率 (%)",
                Fill = null,
                Stroke = new SolidColorPaint(SKColors.Blue) { StrokeThickness = 3 },
                GeometrySize = 10,
                GeometryFill = new SolidColorPaint(SKColors.Blue),
                GeometryStroke = new SolidColorPaint(SKColors.White) { StrokeThickness = 2 }
            }
        };
        
        MonthlyChart.XAxes = new[]
        {
            new Axis
            {
                Labels = months,
                LabelsRotation = 45,
                TextSize = 10
            }
        };
        
        MonthlyChart.YAxes = new[]
        {
            new Axis
            {
                Name = "勝率 (%)",
                MinLimit = 0,
                MaxLimit = 100,
                TextSize = 12
            }
        };
    }
}