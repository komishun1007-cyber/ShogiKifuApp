// File: ViewModels/KifuListViewModel.cs
using System.Collections.ObjectModel;
using ShogiKifuApp.Models;
using ShogiKifuApp.Data;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ShogiKifuApp.ViewModels;

public class KifuListViewModel : INotifyPropertyChanged
{
    public ObservableCollection<KifuRecord> Kifus { get; } = new();
    private readonly KifuDatabase _db = new();

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set { _isBusy = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public async Task InitializeAsync()
    {
        // 初回だけダミーデータをDBに投入（空なら）
        if (await _db.CountAsync() == 0)
        {
            var now = DateTime.Today;
            await _db.InsertAsync(new KifuRecord { Date = now.AddDays(-2), Black = "komi",   White = "nao",    Moves = 98,  Result = "先手勝ち" });
            await _db.InsertAsync(new KifuRecord { Date = now.AddDays(-5), Black = "tanaka", White = "suzuki", Moves = 76,  Result = "後手勝ち" });
            await _db.InsertAsync(new KifuRecord { Date = now.AddDays(-9), Black = "yamada", White = "sato",   Moves = 120, Result = "引き分け" });
        }

        await LoadAsync();
    }

    public async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            var items = await _db.GetAllAsync();
            Kifus.Clear();
            foreach (var k in items) Kifus.Add(k);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
