using ShogiKifuApp.Models;
using ShogiKifuApp.Parsers;
using System.Text.RegularExpressions;

namespace ShogiKifuApp.Pages;

public partial class KifuDetailPage : ContentPage
{
    private KifuRecord _record;
    private KifuModel? _model;

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
    }

    private async void ParseKifuAndInitBoard()
    {
        try
        {
            _model = KifuParser.Parse(_record.KifuText ?? "");

            System.Console.WriteLine($"=== 棋譜パース結果 ===");
            System.Console.WriteLine($"先手: {_model.Sente}");
            System.Console.WriteLine($"後手: {_model.Gote}");
            System.Console.WriteLine($"手数: {_model.Moves.Count}");

            if (_model.Moves.Count > 0)
            {
                System.Console.WriteLine($"最初の手: {_model.Moves[0].Raw}");
            }
            else
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
    System.Console.WriteLine("=== ExtractAndDisplayKifuInfo 開始 ===");

    string text = _record.KifuText ?? "";
    System.Console.WriteLine($"KifuText length: {text.Length}");

        // 行ごとに分割
        var lines = Regex.Matches(text, @"^.*$", RegexOptions.Multiline).Select(m => m.Value).ToArray();
        System.Console.WriteLine($"lines count: {lines.Length}");

    // 先手情報（名前と棋力）
    string senteName = "不明";
    string senteRank = "";


        for (int i = 0; i < lines.Length; i++)
        {
            System.Console.WriteLine($"[{i}] '{lines[i]}'");
        }


        foreach (var line in lines)
        {
            if (Regex.IsMatch(line, @"^先手[:：]"))
            {
                System.Console.WriteLine($"先手行検出: {line}");

                // 名前
                var nameMatch = Regex.Match(line, @"^先手[:：]\s*([^\s　]+)");
                if (nameMatch.Success)
                {
                    senteName = nameMatch.Groups[1].Value.Trim();
                    System.Console.WriteLine($"先手名取得: {senteName}");
                }

                // 段位（「三段」「二段」など）
                var rankMatch = Regex.Match(line, @"\s(.*段)");
                if (rankMatch.Success)
                {
                    senteRank = rankMatch.Groups[1].Value.Trim();
                    System.Console.WriteLine($"先手棋力取得: {senteRank}");
                }
            }
        }

    SenteInfoLabel.Text = string.IsNullOrEmpty(senteRank)
        ? senteName
        : $"{senteName} ({senteRank})";
    System.Console.WriteLine($"SenteInfoLabel.Text = {SenteInfoLabel.Text}");

    // 後手情報（名前と棋力）
    string goteName = "不明";
    string goteRank = "";

    foreach (var line in lines)
    {
        if (Regex.IsMatch(line, @"^後手[:：]"))
        {
            System.Console.WriteLine($"後手行検出: {line}");
            var match = Regex.Match(line, @"^後手[:：](.+)$");
            if (match.Success)
            {
                goteName = match.Groups[1].Value.Trim();
                System.Console.WriteLine($"後手名取得: {goteName}");
            }
        }
        else if (Regex.IsMatch(line, @"^後手の棋力[:：]"))
        {
            System.Console.WriteLine($"後手棋力行検出: {line}");
            var match = Regex.Match(line, @"^後手の棋力[:：](.+)$");
            if (match.Success)
            {
                goteRank = match.Groups[1].Value.Trim();
                System.Console.WriteLine($"後手棋力取得: {goteRank}");
            }
        }
    }

    GoteInfoLabel.Text = string.IsNullOrEmpty(goteRank)
        ? goteName
        : $"{goteName} ({goteRank})";
    System.Console.WriteLine($"GoteInfoLabel.Text = {GoteInfoLabel.Text}");

    // 開始日時
    string startDate = "不明";
    foreach (var line in lines)
    {
        if (Regex.IsMatch(line, @"^開始日時[:：]"))
        {
            System.Console.WriteLine($"開始日時行検出: {line}");
            var match = Regex.Match(line, @"^開始日時[:：](.+)$");
            if (match.Success)
            {
                startDate = match.Groups[1].Value.Trim();
                System.Console.WriteLine($"開始日時取得: {startDate}");
                break;
            }
        }
    }
    StartDateLabel.Text = startDate;
    System.Console.WriteLine($"StartDateLabel.Text = {StartDateLabel.Text}");

    // 棋戦
    string eventName = "不明";
    foreach (var line in lines)
    {
        if (Regex.IsMatch(line, @"^棋戦[:：]"))
        {
            System.Console.WriteLine($"棋戦行検出: {line}");
            var match = Regex.Match(line, @"^棋戦[:：](.+)$");
            if (match.Success)
            {
                eventName = match.Groups[1].Value.Trim();
                System.Console.WriteLine($"棋戦取得: {eventName}");
                break;
            }
        }
    }
    EventLabel.Text = eventName;
    System.Console.WriteLine($"EventLabel.Text = {EventLabel.Text}");

    // 持ち時間
    string timeLimit = "不明";
    foreach (var line in lines)
    {
        if (Regex.IsMatch(line, @"^持ち時間[:：]"))
        {
            System.Console.WriteLine($"持ち時間行検出: {line}");
            var match = Regex.Match(line, @"^持ち時間[:：](.+)$");
            if (match.Success)
            {
                timeLimit = match.Groups[1].Value.Trim();
                System.Console.WriteLine($"持ち時間取得: {timeLimit}");
                break;
            }
        }
    }
    TimeLimitLabel.Text = timeLimit;
    System.Console.WriteLine($"TimeLimitLabel.Text = {TimeLimitLabel.Text}");

    // 総手数
    TotalMovesLabel.Text = _model?.Moves.Count.ToString() ?? "0";
    System.Console.WriteLine($"TotalMoves = {TotalMovesLabel.Text}");

    // 先手の手筋
    var senteTesujis = new List<string>();
    foreach (var line in lines)
    {
        if (Regex.IsMatch(line, @"^\*▲手筋[:：]"))
        {
            System.Console.WriteLine($"先手手筋行検出: {line}");
            var match = Regex.Match(line, @"^\*▲手筋[:：](.+)$");
            if (match.Success)
            {
                senteTesujis.Add(match.Groups[1].Value.Trim());
                System.Console.WriteLine($"先手手筋追加: {match.Groups[1].Value.Trim()}");
            }
        }
    }
    SenteTesujisLabel.Text = senteTesujis.Count > 0
        ? string.Join(", ", senteTesujis)
        : "なし";
    System.Console.WriteLine($"SenteTesujisLabel.Text = {SenteTesujisLabel.Text}");

    // 後手の手筋
    var goteTesujis = new List<string>();
    foreach (var line in lines)
    {
        if (Regex.IsMatch(line, @"^\*△手筋[:：]"))
        {
            System.Console.WriteLine($"後手手筋行検出: {line}");
            var match = Regex.Match(line, @"^\*△手筋[:：](.+)$");
            if (match.Success)
            {
                goteTesujis.Add(match.Groups[1].Value.Trim());
                System.Console.WriteLine($"後手手筋追加: {match.Groups[1].Value.Trim()}");
            }
        }
    }
    GoteTesujisLabel.Text = goteTesujis.Count > 0
        ? string.Join(", ", goteTesujis)
        : "なし";
    System.Console.WriteLine($"GoteTesujisLabel.Text = {GoteTesujisLabel.Text}");

    System.Console.WriteLine("=== ExtractAndDisplayKifuInfo 終了 ===");
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