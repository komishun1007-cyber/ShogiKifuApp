using ShogiKifuApp.Models;
using ShogiKifuApp.Parsers;
using System.Text.RegularExpressions;

namespace ShogiKifuApp.Pages;

public partial class KifuDetailPage : ContentPage
{
    private KifuRecord _record;
    private KifuModel? _model;
    private KifuParser.KifData? _kifData;

    public KifuDetailPage(KifuRecord record)
    {
        InitializeComponent();
        _record = record;
        BindingContext = _record;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ParseKifuAndInitBoard();
        ExtractAndDisplayKifuInfo();

        BoardView.MoveIndexChanged += OnMoveIndexChanged;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        BoardView.MoveIndexChanged -= OnMoveIndexChanged;
    }

    private void OnMoveIndexChanged(object? sender, int moveIndex)
    {
        UpdateCurrentTesujis(moveIndex);
    }

    private void UpdateCurrentTesujis(int moveIndex)
    {
        if (_model == null || moveIndex < 0 || moveIndex >= _model.Moves.Count)
        {
            TesujisFrame.IsVisible = false;
            return;
        }

        var move = _model.Moves[moveIndex];
        if (move.Comments.Count == 0)
        {
            TesujisFrame.IsVisible = false;
            return;
        }

        var tesujis = move.Comments
            .Where(c => c.Contains("手筋："))
            .Select(c =>
            {
                var match = Regex.Match(c, @"[▲△]手筋[:：](.+)$");
                return match.Success ? match.Groups[1].Value.Trim() : null;
            })
            .Where(t => t != null)
            .ToList();

        if (tesujis.Count == 0)
        {
            TesujisFrame.IsVisible = false;
            return;
        }

        CurrentTesujisLabel.Text = string.Join(", ", tesujis);
        TesujisFrame.IsVisible = true;
    }

    private async void ParseKifuAndInitBoard()
    {
        try
        {
            _kifData = KifuParser.ParseRaw(_record.KifuText ?? "");
            _model = KifuParser.Parse(_record.KifuText ?? "");

            if (_model.Moves.Count == 0)
            {
                await DisplayAlert("警告", "棋譜から手が抽出できませんでした", "OK");
            }

            BoardView.ResetHistory();
            BoardView.SetInitialPosition();
            BoardView.SetMoves(_model.Moves);
        }
        catch (Exception ex)
        {
            await DisplayAlert("エラー", $"棋譜の解析に失敗しました:\n{ex.Message}", "OK");
            System.Console.WriteLine($"Parse Error: {ex}");
        }
    }

    private void ExtractAndDisplayKifuInfo()
    {
        if (_kifData == null) return;

        var headers = _kifData.Header;

        // 先手情報
        string senteName = headers.GetValueOrDefault("先手", headers.GetValueOrDefault("下手", "不明"));
        SenteInfoLabel.Text = senteName;

        // 後手情報
        string goteName = headers.GetValueOrDefault("後手", headers.GetValueOrDefault("上手", "不明"));
        GoteInfoLabel.Text = goteName;

        // 開始日時
        string startDate = headers.GetValueOrDefault("開始日時", "不明");
        StartDateLabel.Text = startDate;
        
        // 棋戦名
        string tournament = headers.GetValueOrDefault("棋戦", "-");
        TournamentLabel.Text = tournament;
        
        // 持ち時間
        string timeControl = headers.GetValueOrDefault("持ち時間", "-");
        TimeControlLabel.Text = timeControl;

        // 結果
        string result = headers.GetValueOrDefault("結末", "");
        string winner = headers.GetValueOrDefault("勝者", "");

        string resultText = "";
        if (!string.IsNullOrEmpty(winner))
        {
            string winnerName = winner == "▲" ? "先手" : winner == "△" ? "後手" : winner;
            resultText = $"{winnerName}勝ち";
            if (!string.IsNullOrEmpty(result))
                resultText += $" ({result})";
        }
        else if (!string.IsNullOrEmpty(result))
        {
            resultText = result;
        }
        else
        {
            resultText = "不明";
        }

        ResultLabel.Text = resultText;
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        var yes = await DisplayAlert("確認", "この棋譜を削除しますか？", "削除", "キャンセル");
        if (!yes) return;

        try
        {
            await App.Database.DeleteAsync(_record);
            await DisplayAlert("完了", "削除しました", "OK");
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("エラー", ex.Message, "OK");
        }
    }
}