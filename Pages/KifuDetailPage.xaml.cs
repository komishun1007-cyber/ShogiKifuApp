using ShogiKifuApp.Models;
using ShogiKifuApp.Parsers;

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
    
    private async void OnPlayNextClicked(object sender, EventArgs e)
    {
        try
        {
            if (_model == null || _model.Moves.Count == 0) 
            { 
                await DisplayAlert("情報", "手がありません", "OK"); 
                return; 
            }
            if (_moveIndex + 1 >= _model.Moves.Count) 
            { 
                await DisplayAlert("情報", "もう最後の手です", "OK"); 
                return; 
            }
            
            _moveIndex++;
            var mv = _model.Moves[_moveIndex];
            
            // デバッグ情報を表示
            var debugInfo = $"手番: {_moveIndex + 1}\n" +
                          $"移動先: {mv.ToFile}{mv.ToRank}\n" +
                          $"駒: {mv.Piece}\n" +
                          $"移動元: {mv.FromFile?.ToString() ?? "?"}{mv.FromRank?.ToString() ?? "?"}\n" +
                          $"成り: {mv.IsPromotion}";
            
            System.Diagnostics.Debug.WriteLine(debugInfo);
            
            BoardView.ApplyMove(mv);
            UpdateMoveLabel();
        }
        catch (Exception ex)
        {
            await DisplayAlert("エラー", $"手を進められません:\n{ex.Message}", "OK");
            System.Diagnostics.Debug.WriteLine($"Error: {ex}");
        }
    }

    private async void OnPlayPrevClicked(object sender, EventArgs e)
    {
        try
        {
            if (_moveIndex < 0) 
            { 
                await DisplayAlert("情報", "最初の局面です", "OK"); 
                return; 
            }
            BoardView.UndoMove();
            _moveIndex--;
            UpdateMoveLabel();
        }
        catch (Exception ex)
        {
            await DisplayAlert("エラー", $"手を戻せません:\n{ex.Message}", "OK");
        }
    }

    private void OnResetClicked(object sender, EventArgs e)
    {
        try
        {
            BoardView.ResetHistory();
            _moveIndex = -1;
            UpdateMoveLabel();
        }
        catch (Exception ex)
        {
            DisplayAlert("エラー", $"リセットできません:\n{ex.Message}", "OK");
        }
    }

    private async void ParseKifuAndInitBoard()
    {
        try
        {
            _model = KifuParser.Parse(_record.KifuText ?? "");
            
            // パース結果をデバッグ出力
            System.Diagnostics.Debug.WriteLine($"=== 棋譜パース結果 ===");
            System.Diagnostics.Debug.WriteLine($"先手: {_model.Sente}");
            System.Diagnostics.Debug.WriteLine($"後手: {_model.Gote}");
            System.Diagnostics.Debug.WriteLine($"手数: {_model.Moves.Count}");
            
            if (_model.Moves.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"最初の手: {_model.Moves[0].Raw}");
            }
            else
            {
                await DisplayAlert("警告", "棋譜から手が抽出できませんでした", "OK");
            }
            
            BoardView.ResetHistory();
            BoardView.SetInitialPosition();
            _moveIndex = -1;
            UpdateMoveLabel();
        }
        catch (Exception ex)
        {
            await DisplayAlert("エラー", $"棋譜の解析に失敗しました:\n{ex.Message}", "OK");
            System.Diagnostics.Debug.WriteLine($"Parse Error: {ex}");
        }
    }

    private void UpdateMoveLabel()
    {
        if (_model == null) return;
        if (MoveLabel != null)
        {
            MoveLabel.Text = $"{_moveIndex + 1}手目 / {_model.Moves.Count}手";
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        try
        {
            _record.KifuText = KifuEditor.Text ?? string.Empty;
            await App.Database.UpdateAsync(_record);
            await DisplayAlert("完了", "保存しました", "OK");
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
}