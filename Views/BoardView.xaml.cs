using System.Collections.Generic;
using System.Linq;
using ShogiKifuApp.Models;

namespace ShogiKifuApp.Views;

public partial class BoardView : ContentView
{
    // 盤面の状態 [file][rank] (1-9)
    private readonly string?[,] _board = new string?[10, 10];
    
    // 駒の所有者 [file][rank] (true=先手, false=後手)
    private readonly bool?[,] _pieceOwner = new bool?[10, 10];
    
    // 表示用のLabel [file][rank]
    private readonly Label[,] _cells = new Label[10, 10];
    
    // 最後の手の座標
    private (int file, int rank)? _lastMovePosition = null;
    
    // 履歴スタック
    private readonly Stack<BoardState> _history = new Stack<BoardState>();
    
    // 持ち駒
    private readonly Dictionary<string, int> _senteCaptured = new Dictionary<string, int>();
    private readonly Dictionary<string, int> _goteCaptured = new Dictionary<string, int>();
    
    // 全ての手のリスト
    private List<Move> _allMoves = new List<Move>();
    private int _currentMoveIndex = -1;
    private bool _isUpdatingSlider = false;

    // 色定義
    private readonly Color _normalCellColor = Color.FromArgb("#F5DEB3");
    private readonly Color _highlightCellColor = Color.FromArgb("#ADD8E6"); // 薄い青色

    // イベント
    public event EventHandler<int>? MoveIndexChanged;

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
            BoardGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(35) });
            BoardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(35) });
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
                    BackgroundColor = _normalCellColor,
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
            {
                _board[f, r] = null;
                _pieceOwner[f, r] = null;
            }

        // 持ち駒クリア
        _senteCaptured.Clear();
        _goteCaptured.Clear();
        
        // ハイライトクリア
        _lastMovePosition = null;

        // 標準的な初期配置
        // 後手（上側）- 所有者をfalseに設定
        _board[1, 1] = "香"; _pieceOwner[1, 1] = false;
        _board[9, 1] = "香"; _pieceOwner[9, 1] = false;
        _board[2, 1] = "桂"; _pieceOwner[2, 1] = false;
        _board[8, 1] = "桂"; _pieceOwner[8, 1] = false;
        _board[3, 1] = "銀"; _pieceOwner[3, 1] = false;
        _board[7, 1] = "銀"; _pieceOwner[7, 1] = false;
        _board[4, 1] = "金"; _pieceOwner[4, 1] = false;
        _board[6, 1] = "金"; _pieceOwner[6, 1] = false;
        _board[5, 1] = "王"; _pieceOwner[5, 1] = false;
        _board[2, 2] = "角"; _pieceOwner[2, 2] = false;
        _board[8, 2] = "飛"; _pieceOwner[8, 2] = false;
        for (int f = 1; f <= 9; f++)
        {
            _board[f, 3] = "歩";
            _pieceOwner[f, 3] = false;
        }

        // 先手（下側）- 所有者をtrueに設定
        for (int f = 1; f <= 9; f++)
        {
            _board[f, 7] = "歩";
            _pieceOwner[f, 7] = true;
        }
        _board[2, 8] = "飛"; _pieceOwner[2, 8] = true;
        _board[8, 8] = "角"; _pieceOwner[8, 8] = true;
        _board[1, 9] = "香"; _pieceOwner[1, 9] = true;
        _board[9, 9] = "香"; _pieceOwner[9, 9] = true;
        _board[2, 9] = "桂"; _pieceOwner[2, 9] = true;
        _board[8, 9] = "桂"; _pieceOwner[8, 9] = true;
        _board[3, 9] = "銀"; _pieceOwner[3, 9] = true;
        _board[7, 9] = "銀"; _pieceOwner[7, 9] = true;
        _board[4, 9] = "金"; _pieceOwner[4, 9] = true;
        _board[6, 9] = "金"; _pieceOwner[6, 9] = true;
        _board[5, 9] = "玉"; _pieceOwner[5, 9] = true;

        UpdateDisplay();
    }

    public void SetMoves(List<Move> moves)
    {
        _allMoves = moves;
        _currentMoveIndex = -1;
        
        _isUpdatingSlider = true;
        MoveSlider.Maximum = moves.Count;
        MoveSlider.Value = 0;
        _isUpdatingSlider = false;
        
        UpdateMoveLabel();
        UpdateMoveSliderDisplay();
    }

    public void ApplyMove(Move move)
    {
        try
        {
            System.Console.WriteLine($"=== ApplyMove ===");
            System.Console.WriteLine($"To: {move.ToFile},{move.ToRank}");
            System.Console.WriteLine($"From: {move.FromFile},{move.FromRank}");
            System.Console.WriteLine($"Piece: {move.Piece}");
            
            // 現在の手番を判定（1手目=先手=true, 2手目=後手=false）
            bool isSenteMove = (_currentMoveIndex + 1) % 2 == 1;
            
            // 現在の状態を保存
            var savedState = new BoardState(_board, _pieceOwner, move, _senteCaptured, _goteCaptured, _lastMovePosition);
            _history.Push(savedState);

            // 移動先の駒を取得（取った駒）
            var capturedPiece = _board[move.ToFile, move.ToRank];
            var capturedOwner = _pieceOwner[move.ToFile, move.ToRank];
            
            if (!string.IsNullOrEmpty(capturedPiece) && capturedOwner.HasValue)
            {
                // 成駒を元に戻す
                var basePiece = UnpromotePiece(capturedPiece);
                
                // 相手の駒を取った場合のみ持ち駒に追加
                if (capturedOwner.Value != isSenteMove)
                {
                    if (isSenteMove)
                    {
                        if (!_senteCaptured.ContainsKey(basePiece))
                            _senteCaptured[basePiece] = 0;
                        _senteCaptured[basePiece]++;
                        System.Console.WriteLine($"先手が{basePiece}を取った");
                    }
                    else
                    {
                        if (!_goteCaptured.ContainsKey(basePiece))
                            _goteCaptured[basePiece] = 0;
                        _goteCaptured[basePiece]++;
                        System.Console.WriteLine($"後手が{basePiece}を取った");
                    }
                }
            }

            // 移動元から駒を取得
            string? piece = null;
            bool pieceOwner = isSenteMove;
            
            if (move.FromFile.HasValue && move.FromRank.HasValue)
            {
                int fromF = move.FromFile.Value;
                int fromR = move.FromRank.Value;
                
                // 範囲チェック
                if (fromF < 1 || fromF > 9 || fromR < 1 || fromR > 9)
                {
                    System.Console.WriteLine($"ERROR: 移動元が範囲外 ({fromF},{fromR})");
                    return;
                }
                
                piece = _board[fromF, fromR];
                pieceOwner = _pieceOwner[fromF, fromR] ?? isSenteMove;
                System.Console.WriteLine($"移動元の駒: {piece ?? "なし"} (所有者: {(pieceOwner ? "先手" : "後手")})");
                
                _board[fromF, fromR] = null;
                _pieceOwner[fromF, fromR] = null;
            }
            else
            {
                // 持ち駒から打つ
                piece = move.Piece;
                pieceOwner = isSenteMove;
                System.Console.WriteLine($"持ち駒から打つ: {piece}");
                
                // 持ち駒を減らす
                var captured = isSenteMove ? _senteCaptured : _goteCaptured;
                if (captured.ContainsKey(piece) && captured[piece] > 0)
                {
                    captured[piece]--;
                    if (captured[piece] == 0)
                        captured.Remove(piece);
                }
            }

            // 範囲チェック
            if (move.ToFile < 1 || move.ToFile > 9 || move.ToRank < 1 || move.ToRank > 9)
            {
                System.Console.WriteLine($"ERROR: 移動先が範囲外 ({move.ToFile},{move.ToRank})");
                return;
            }

            // 成る場合
            if (move.IsPromotion && piece != null)
            {
                var promoted = PromotePiece(piece);
                System.Console.WriteLine($"成り: {piece} → {promoted}");
                piece = promoted;
            }

            // 移動先に配置（所有者情報も保持）
            System.Console.WriteLine($"移動先に配置: ({move.ToFile},{move.ToRank}) = {piece} (所有者: {(pieceOwner ? "先手" : "後手")})");
            _board[move.ToFile, move.ToRank] = piece;
            _pieceOwner[move.ToFile, move.ToRank] = pieceOwner;
            
            // 最後の手の位置を更新
            _lastMovePosition = (move.ToFile, move.ToRank);

            UpdateDisplay();
            System.Console.WriteLine("=== ApplyMove 完了 ===");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"ERROR in ApplyMove: {ex}");
        }
    }

    public void UndoMove()
    {
        if (_history.Count == 0) return;

        var state = _history.Pop();
        state.RestoreTo(_board, _pieceOwner, _senteCaptured, _goteCaptured, out _lastMovePosition);
        UpdateDisplay();
    }

    public void ResetHistory()
    {
        _history.Clear();
        _currentMoveIndex = -1;
        SetInitialPosition();
        UpdateMoveLabel();
        UpdateMoveSliderDisplay();
        
        _isUpdatingSlider = true;
        MoveSlider.Value = 0;
        _isUpdatingSlider = false;
    }

    private void UpdateDisplay()
    {
        for (int file = 1; file <= 9; file++)
        {
            for (int rank = 1; rank <= 9; rank++)
            {
                var piece = _board[file, rank];
                var owner = _pieceOwner[file, rank];
                var cell = _cells[file, rank];
                
                // 背景色の設定（最後の手をハイライト）
                if (_lastMovePosition.HasValue && 
                    _lastMovePosition.Value.file == file && 
                    _lastMovePosition.Value.rank == rank)
                {
                    cell.BackgroundColor = _highlightCellColor;
                }
                else
                {
                    cell.BackgroundColor = _normalCellColor;
                }
                
                if (string.IsNullOrEmpty(piece) || !owner.HasValue)
                {
                    cell.Text = "";
                    cell.TextColor = Colors.Black;
                    cell.Rotation = 0;
                }
                else
                {
                    // 全て黒色で統一
                    cell.TextColor = Colors.Black;
                    cell.Text = piece;
                    
                    // 所有者に基づいて回転
                    // true=先手（回転なし）、false=後手（180度回転）
                    cell.Rotation = owner.Value ? 0 : 180;
                }
            }
        }
        
        UpdateCapturedDisplay();
    }

    private void UpdateCapturedDisplay()
    {
        // 先手の持ち駒
        if (_senteCaptured.Count == 0)
        {
            SenteCapturedLabel.Text = "なし";
        }
        else
        {
            var pieces = _senteCaptured
                .OrderBy(p => GetPieceOrder(p.Key))
                .Select(p => p.Value > 1 ? $"{p.Key}×{p.Value}" : p.Key);
            SenteCapturedLabel.Text = string.Join(" ", pieces);
        }

        // 後手の持ち駒
        if (_goteCaptured.Count == 0)
        {
            GoteCapturedLabel.Text = "なし";
        }
        else
        {
            var pieces = _goteCaptured
                .OrderBy(p => GetPieceOrder(p.Key))
                .Select(p => p.Value > 1 ? $"{p.Key}×{p.Value}" : p.Key);
            GoteCapturedLabel.Text = string.Join(" ", pieces);
        }
    }

    private int GetPieceOrder(string piece)
    {
        return piece switch
        {
            "飛" => 0,
            "角" => 1,
            "金" => 2,
            "銀" => 3,
            "桂" => 4,
            "香" => 5,
            "歩" => 6,
            _ => 99
        };
    }

    private void UpdateMoveLabel()
    {
        if (MoveCountLabel != null)
        {
            MoveCountLabel.Text = $"{_currentMoveIndex + 1}手目 / {_allMoves.Count}手";
        }
    }

    private void UpdateMoveSliderDisplay()
    {
        // 現在の手の前後2手を表示
        int currentIndex = _currentMoveIndex;
        
        // 各ラベルをクリア
        Move1Label.Text = "";
        Move2Label.Text = "";
        Move3Label.Text = "";
        Move4Label.Text = "";
        Move5Label.Text = "";
        
        // 前後2手の範囲を計算
        for (int offset = -2; offset <= 2; offset++)
        {
            int index = currentIndex + offset;
            Label? label = offset switch
            {
                -2 => Move1Label,
                -1 => Move2Label,
                0 => Move3Label,
                1 => Move4Label,
                2 => Move5Label,
                _ => null
            };
            
            if (label != null)
            {
                if (index >= 0 && index < _allMoves.Count)
                {
                    var move = _allMoves[index];
                    label.Text = $"{index + 1}.{FormatMoveText(move)}";
                }
                else if (index == -1)
                {
                    label.Text = "0.初期";
                }
            }
        }
    }

    private string FormatMoveText(Move move)
    {
        return move.ShortDisplay;
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

    private string UnpromotePiece(string piece)
    {
        return piece switch
        {
            "と" => "歩",
            "杏" => "香",
            "圭" => "桂",
            "全" => "銀",
            "馬" => "角",
            "龍" => "飛",
            _ => piece
        };
    }

    // スライダーイベント
    private void OnSliderValueChanged(object? sender, ValueChangedEventArgs e)
    {
        if (_isUpdatingSlider) return;
        
        int targetIndex = (int)Math.Round(e.NewValue) - 1;
        
        if (targetIndex == _currentMoveIndex) return;
        
        // 目標の手まで進めるか戻る
        if (targetIndex > _currentMoveIndex)
        {
            // 進める
            while (_currentMoveIndex < targetIndex && _currentMoveIndex + 1 < _allMoves.Count)
            {
                _currentMoveIndex++;
                ApplyMove(_allMoves[_currentMoveIndex]);
            }
        }
        else
        {
            // 戻る
            while (_currentMoveIndex > targetIndex && _currentMoveIndex >= 0)
            {
                UndoMove();
                _currentMoveIndex--;
            }
        }
        
        UpdateMoveLabel();
        UpdateMoveSliderDisplay();
        MoveIndexChanged?.Invoke(this, _currentMoveIndex);
    }

    // ボタンイベント
    private void OnResetClicked(object? sender, EventArgs e)
    {
        ResetHistory();
        MoveIndexChanged?.Invoke(this, _currentMoveIndex);
    }

    private void OnPrevClicked(object? sender, EventArgs e)
    {
        if (_currentMoveIndex < 0) return;
        
        UndoMove();
        _currentMoveIndex--;
        UpdateMoveLabel();
        UpdateMoveSliderDisplay();
        
        _isUpdatingSlider = true;
        MoveSlider.Value = _currentMoveIndex + 1;
        _isUpdatingSlider = false;
        
        MoveIndexChanged?.Invoke(this, _currentMoveIndex);
    }

    private void OnNextClicked(object? sender, EventArgs e)
    {
        if (_currentMoveIndex + 1 >= _allMoves.Count) return;
        
        _currentMoveIndex++;
        ApplyMove(_allMoves[_currentMoveIndex]);
        UpdateMoveLabel();
        UpdateMoveSliderDisplay();
        
        _isUpdatingSlider = true;
        MoveSlider.Value = _currentMoveIndex + 1;
        _isUpdatingSlider = false;
        
        MoveIndexChanged?.Invoke(this, _currentMoveIndex);
    }

    private void OnLastClicked(object? sender, EventArgs e)
    {
        while (_currentMoveIndex + 1 < _allMoves.Count)
        {
            _currentMoveIndex++;
            ApplyMove(_allMoves[_currentMoveIndex]);
        }
        UpdateMoveLabel();
        UpdateMoveSliderDisplay();
        
        _isUpdatingSlider = true;
        MoveSlider.Value = _currentMoveIndex + 1;
        _isUpdatingSlider = false;
        
        MoveIndexChanged?.Invoke(this, _currentMoveIndex);
    }

    private class BoardState
    {
        private readonly string?[,] _boardCopy = new string?[10, 10];
        private readonly bool?[,] _ownerCopy = new bool?[10, 10];
        private readonly Dictionary<string, int> _senteCapturedCopy = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _goteCapturedCopy = new Dictionary<string, int>();
        private readonly (int file, int rank)? _lastMovePositionCopy;
        public Move Move { get; }

        public BoardState(string?[,] board, bool?[,] owner, Move move, 
            Dictionary<string, int> senteCaptured, Dictionary<string, int> goteCaptured,
            (int file, int rank)? lastMovePosition)
        {
            Move = move;
            _lastMovePositionCopy = lastMovePosition;
            
            // 配列を手動でコピー
            for (int f = 0; f < 10; f++)
                for (int r = 0; r < 10; r++)
                {
                    _boardCopy[f, r] = board[f, r];
                    _ownerCopy[f, r] = owner[f, r];
                }
            
            // 持ち駒をコピー
            foreach (var kvp in senteCaptured)
                _senteCapturedCopy[kvp.Key] = kvp.Value;
            foreach (var kvp in goteCaptured)
                _goteCapturedCopy[kvp.Key] = kvp.Value;
        }

        public void RestoreTo(string?[,] board, bool?[,] owner,
            Dictionary<string, int> senteCaptured, Dictionary<string, int> goteCaptured,
            out (int file, int rank)? lastMovePosition)
        {
            for (int f = 0; f < 10; f++)
                for (int r = 0; r < 10; r++)
                {
                    board[f, r] = _boardCopy[f, r];
                    owner[f, r] = _ownerCopy[f, r];
                }
            
            senteCaptured.Clear();
            foreach (var kvp in _senteCapturedCopy)
                senteCaptured[kvp.Key] = kvp.Value;
            
            goteCaptured.Clear();
            foreach (var kvp in _goteCapturedCopy)
                goteCaptured[kvp.Key] = kvp.Value;
            
            lastMovePosition = _lastMovePositionCopy;
        }
    }
}