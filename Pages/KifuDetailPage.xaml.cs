using ShogiKifuApp.Models;
using ShogiKifuApp.Parsers;
using ShogiKifuApp.Views;

namespace ShogiKifuApp.Pages;

public partial class KifuDetailPage : ContentPage
{
    private KifuRecord _record;
    private KifuModel? _model;
    private int _moveIndex = -1;

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
    
    private void OnPlayNextClicked(object sender, EventArgs e)
    {
        if (_model==null || _model.Moves.Count==0) { DisplayAlert("情報","手がありません","OK"); return; }
        if (_moveIndex + 1 >= _model.Moves.Count) { DisplayAlert("情報","もう最後の手です","OK"); return; }
        _moveIndex++;
        var mv = _model.Moves[_moveIndex];
        BoardView.ApplyMove(mv);
    }

    private void OnPlayPrevClicked(object sender, EventArgs e)
    {
        if (_moveIndex < 0) { DisplayAlert("情報","最初の局面です","OK"); return; }
        BoardView.UndoMove();
        _moveIndex--;
    }

    private void ParseKifuAndInitBoard()
    {
        _model = KifuParser.Parse(_record.KifuText ?? "");
        // TODO: set board initial position based on KIF header if present
        BoardView.ResetHistory();
        BoardView.SetInitialPosition(); // or set from model if available
        _moveIndex = -1;
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        try
        {
            // KifuEditor で本文を編集できるようにしているので、BindingContextのKifuTextを更新してからDBへ
            _record.KifuText = KifuEditor.Text ?? string.Empty;
            await App.Database.UpdateAsync(_record);

            await DisplayAlert("完了", "保存しました", "OK");
            // 戻るか一覧をリフレッシュしたい場合は呼ぶ
        }
        catch (Exception ex)
        {
            await DisplayAlert("エラー", ex.Message, "OK");
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

    // // 以下はプレースホルダ（次ステップでKIFパースをつなげる）
    // private int _currentMoveIndex = -1;
    // private List<string>? _moves; // 文字列で手リストを持つ

    // private void EnsureMovesParsed()
    // {
    //     if (_moves != null) return;
    //     // 仮実装：とりあえず KifuText の行から "1 " のような手番号で始まる行を抽出
    //     _moves = new List<string>();
    //     var lines = (_record.KifuText ?? string.Empty).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
    //     foreach (var line in lines)
    //     {
    //         if (System.Text.RegularExpressions.Regex.IsMatch(line, @"^\s*\d+\s"))
    //         {
    //             _moves.Add(line.Trim());
    //         }
    //     }
    // }

    // private void OnPlayNextClicked(object sender, EventArgs e)
    // {
    //     EnsureMovesParsed();
    //     if (_moves == null || _moves.Count == 0)
    //     {
    //         DisplayAlert("情報", "棋譜から手が見つかりません。", "OK");
    //         return;
    //     }
    //     if (_currentMoveIndex + 1 < _moves.Count)
    //     {
    //         _currentMoveIndex++;
    //         // TODO: ここで盤面に1手進める処理を呼ぶ（後で BoardView と連携）
    //         DisplayAlert("次の手", _moves[_currentMoveIndex], "OK");
    //     }
    // }

    // private void OnPlayPrevClicked(object sender, EventArgs e)
    // {
    //     EnsureMovesParsed();
    //     if (_moves == null || _moves.Count == 0)
    //     {
    //         DisplayAlert("情報", "棋譜から手が見つかりません。", "OK");
    //         return;
    //     }
    //     if (_currentMoveIndex - 1 >= 0)
    //     {
    //         _currentMoveIndex--;
    //         // TODO: ここで盤面を1手戻す処理を呼ぶ
    //         DisplayAlert("前の手", _moves[_currentMoveIndex], "OK");
    //     }
    // }

}
