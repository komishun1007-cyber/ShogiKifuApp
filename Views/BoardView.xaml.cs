using ShogiKifuApp.Models;

namespace ShogiKifuApp.Views;

public partial class BoardView : ContentView
{
    // board[y][x], 1-based indices for readability; we'll store as [1..9][1..9]
    private Label[,] cells = new Label[10,10];
    // internal board state: piece string or null
    private string?[,] board = new string?[10,10];
    // history stack for undo
    private Stack<(Move move, string? captured, (int f,int r) from, (int f,int r) to)> history = new();

    public BoardView()
    {
        InitializeComponent();
        BuildGrid();
        SetInitialPosition();
    }

    void BuildGrid()
    {
        BoardGrid.RowDefinitions.Clear();
        BoardGrid.ColumnDefinitions.Clear();
        for (int r=0;r<9;r++) BoardGrid.RowDefinitions.Add(new RowDefinition{ Height = GridLength.Star });
        for (int c=0;c<9;c++) BoardGrid.ColumnDefinitions.Add(new ColumnDefinition{ Width = GridLength.Star });

        for (int rank=1; rank<=9; rank++)
        {
            for (int file=1; file<=9; file++)
            {
                var lbl = new Label
                {
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    BackgroundColor = Colors.Beige,
                    FontSize = 18
                };
                cells[file,rank] = lbl;
                BoardGrid.Add(lbl, file-1, rank-1);
            }
        }
    }

    // シンプルな初期局面（先手が下）
    public void SetInitialPosition()
    {
        // clear
        for (int f=1;f<=9;f++) for (int r=1;r<=9;r++) board[f,r] = null;

        // pawns (歩) : sente on rank 7? Actually Shogi: sente's pawns on rank 7 (from sente perspective)
        // We'll set a very simple starting position as text abbreviations:
        // For simplicity: use single-char piece codes: '歩','香','桂','銀','金','角','飛','玉'
        // This is approximate; you can customize later.
        // Clear then set just a few pieces to test
        board[7,3] = "歩"; // placeholder (not exact standard)
        // For our purposes, better to start with an empty board to test moves
        UpdateUI();
    }

    void UpdateUI()
    {
        for (int f=1; f<=9; f++)
        for (int r=1; r<=9; r++)
        {
            var txt = board[f,r];
            cells[f,r].Text = txt ?? "";
        }
    }

    // Apply move (very simple): use from if present; otherwise move first matching piece found
    public void ApplyMove(Move m)
    {
        // choose source
        int sf=0,sr=0;
        if (m.FromFile.HasValue && m.FromRank.HasValue)
        {
            sf = m.FromFile.Value; sr = m.FromRank.Value;
            if (board[sf,sr] == null)
            {
                // nothing at origin -> attempt to find a piece
                sf = sr = 0;
            }
        }
        if (sf==0)
        {
            // naive search: find a piece with same Piece label
            bool found=false;
            for (int f=1; f<=9 && !found; f++)
            for (int r=1; r<=9 && !found; r++)
            {
                if (board[f,r] != null && board[f,r] == m.Piece)
                {
                    sf=f; sr=r; found=true; break;
                }
            }
        }

        var captured = (sf>0) ? board[m.ToFile,m.ToRank] : null;
        var fromPos = (sf>0) ? (sf,sr) : (0,0);
        var toPos = (m.ToFile, m.ToRank);

        string? moving = null;
        if (sf>0) { moving = board[sf,sr]; board[sf,sr] = null; }
        else moving = m.Piece; // place piece even if origin unknown

        board[m.ToFile,m.ToRank] = moving;

        history.Push((m, captured, fromPos, toPos));
        UpdateUI();
    }

    public void UndoMove()
    {
        if (history.Count==0) return;
        var (m,captured,from,to) = history.Pop();
        // clear destination
        board[to.f,to.r] = null;
        // restore captured if any
        if (captured != null) board[to.f,to.r] = captured;
        // restore mover to from if known
        if (from.f!=0)
            board[from.f,from.r] = m.Piece;
        UpdateUI();
    }

    public void ResetHistory()
    {
        history.Clear();
    }
}
