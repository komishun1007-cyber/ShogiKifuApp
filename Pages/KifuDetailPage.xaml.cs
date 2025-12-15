using ShogiKifuApp.Models;
using ShogiKifuApp.Parsers;

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
    }
    private async void ParseKifuAndInitBoard()
    {
        try
        {
            _model = KifuParser.Parse(_record.KifuText ?? "");

            // パース結果をデバッグ出力
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