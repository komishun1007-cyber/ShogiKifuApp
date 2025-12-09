using System.Collections.Generic;
using ShogiKifuApp.Models;

namespace ShogiKifuApp.Views;

public partial class BoardView : ContentView
{
    // 盤面の状態 [file][rank] (1-9)
    private readonly string?[,] _board = new string?[10, 10];
    
    // 表示用のLabel [file][rank]
    private readonly Label[,] _cells = new Label[10, 10];
    
    // 履歴スタック
    private readonly Stack<BoardState> _history = new Stack<BoardState>();

    public BoardView()
    {
        InitializeComponent();
        InitializeBoard();
        SetInitialPosition();
    }

    private void InitializeBoard()
    {
        // 9x9のグリッド作成
        for (int i = 0; i < 9; i++)
        {
            BoardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            BoardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        }

        // セルの作成（将棋は9×9）
        for (int rank = 1; rank <= 9; rank++)
        {
            for (int file = 1; file <= 9; file++)
            {
                var label = new Label
                {
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    BackgroundColor = Color.FromArgb("#F5DEB3"),
                    FontSize = 18,
                    FontAttributes = FontAttributes.Bold,
                    Padding = 2,
                    LineBreakMode = LineBreakMode.NoWrap
                };

                // グリッドに配置
                BoardGrid.Add(label, 9 - file, rank - 1);
                _cells[file, rank] = label;
            }
        }
    }

    public void SetInitialPosition()
    {
        // 盤面クリア
        for (int f = 1; f <= 9; f++)
            for (int r = 1; r <= 9; r++)
                _board[f, r] = null;

        // 標準的な初期配置
        // 後手（上側）
        _board[1, 1] = "香"; _board[9, 1] = "香";
        _board[2, 1] = "桂"; _board[8, 1] = "桂";
        _board[3, 1] = "銀"; _board[7, 1] = "銀";
        _board[4, 1] = "金"; _board[6, 1] = "金";
        _board[5, 1] = "王";
        _board[2, 2] = "角";
        _board[8, 2] = "飛";
        for (int f = 1; f <= 9; f++)
            _board[f, 3] = "歩";

        // 先手（下側）
        for (int f = 1; f <= 9; f++)
            _board[f, 7] = "歩";
        _board[2, 8] = "飛";
        _board[8, 8] = "角";
        _board[1, 9] = "香"; _board[9, 9] = "香";
        _board[2, 9] = "桂"; _board[8, 9] = "桂";
        _board[3, 9] = "銀"; _board[7, 9] = "銀";
        _board[4, 9] = "金"; _board[6, 9] = "金";
        _board[5, 9] = "玉";

        UpdateDisplay();
    }

    public void ApplyMove(Move move)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"=== ApplyMove ===");
            System.Diagnostics.Debug.WriteLine($"To: {move.ToFile},{move.ToRank}");
            System.Diagnostics.Debug.WriteLine($"From: {move.FromFile},{move.FromRank}");
            System.Diagnostics.Debug.WriteLine($"Piece: {move.Piece}");
            
            // 現在の状態を保存
            var savedState = new BoardState(_board, move);
            _history.Push(savedState);

            // 移動元から駒を取得
            string? piece = null;
            if (move.FromFile.HasValue && move.FromRank.HasValue)
            {
                int fromF = move.FromFile.Value;
                int fromR = move.FromRank.Value;
                
                // 範囲チェック
                if (fromF < 1 || fromF > 9 || fromR < 1 || fromR > 9)
                {
                    System.Diagnostics.Debug.WriteLine($"ERROR: 移動元が範囲外 ({fromF},{fromR})");
                    return;
                }
                
                piece = _board[fromF, fromR];
                System.Diagnostics.Debug.WriteLine($"移動元の駒: {piece ?? "なし"}");
                _board[fromF, fromR] = null;
            }
            else
            {
                // 持ち駒から打つ
                piece = move.Piece;
                System.Diagnostics.Debug.WriteLine($"持ち駒から打つ: {piece}");
            }

            // 範囲チェック
            if (move.ToFile < 1 || move.ToFile > 9 || move.ToRank < 1 || move.ToRank > 9)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR: 移動先が範囲外 ({move.ToFile},{move.ToRank})");
                return;
            }

            // 成る場合
            if (move.IsPromotion && piece != null)
            {
                var promoted = PromotePiece(piece);
                System.Diagnostics.Debug.WriteLine($"成り: {piece} → {promoted}");
                piece = promoted;
            }

            // 移動先に配置
            System.Diagnostics.Debug.WriteLine($"移動先に配置: ({move.ToFile},{move.ToRank}) = {piece}");
            _board[move.ToFile, move.ToRank] = piece;

            UpdateDisplay();
            System.Diagnostics.Debug.WriteLine("=== ApplyMove 完了 ===");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR in ApplyMove: {ex}");
        }
    }

    public void UndoMove()
    {
        if (_history.Count == 0) return;

        var state = _history.Pop();
        state.RestoreTo(_board);
        UpdateDisplay();
    }

    public void ResetHistory()
    {
        _history.Clear();
        SetInitialPosition();
    }

    private void UpdateDisplay()
    {
        for (int file = 1; file <= 9; file++)
        {
            for (int rank = 1; rank <= 9; rank++)
            {
                var piece = _board[file, rank];
                var cell = _cells[file, rank];
                
                if (string.IsNullOrEmpty(piece))
                {
                    cell.Text = "";
                    cell.TextColor = Colors.Black;
                }
                else
                {
                    // 後手の駒（上側3段）
                    if (rank <= 3)
                    {
                        cell.Text = piece;
                        cell.TextColor = Colors.Red;
                    }
                    // 先手の駒（下側3段）
                    else if (rank >= 7)
                    {
                        cell.Text = piece;
                        cell.TextColor = Colors.Blue;
                    }
                    // 中段
                    else
                    {
                        cell.Text = piece;
                        cell.TextColor = Colors.Black;
                    }
                }
            }
        }
    }

    private string PromotePiece(string piece)
    {
        return piece switch
        {
            "歩" => "と",
            "香" => "杏",
            "桂" => "圭",
            "銀" => "全",
            "角" => "馬",
            "飛" => "龍",
            _ => piece
        };
    }

    private class BoardState
    {
        private readonly string?[,] _boardCopy = new string?[10, 10];
        public Move Move { get; }

        public BoardState(string?[,] board, Move move)
        {
            Move = move;
            // 配列を手動でコピー
            for (int f = 0; f < 10; f++)
                for (int r = 0; r < 10; r++)
                    _boardCopy[f, r] = board[f, r];
        }

        public void RestoreTo(string?[,] board)
        {
            for (int f = 0; f < 10; f++)
                for (int r = 0; r < 10; r++)
                    board[f, r] = _boardCopy[f, r];
        }
    }
}