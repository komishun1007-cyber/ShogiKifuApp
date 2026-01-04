using ShogiKifuApp.Models;
using ShogiKifuApp.Parsers;
using System.Text.RegularExpressions;

namespace ShogiKifuApp;

public partial class MainPage : ContentPage
{
    private List<KifuRecord> _allRecords = new List<KifuRecord>();
    
    public MainPage()
    {
        InitializeComponent();
        
        // Converterを追加
        Resources.Add("StringNotEmptyConverter", new StringNotEmptyConverter());
        
        // 日付ピッカーの初期値設定
        StartDatePicker.Date = DateTime.Now.AddYears(-1);
        EndDatePicker.Date = DateTime.Now;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAndFilterData();
    }

    private async Task LoadAndFilterData()
    {
        _allRecords = await App.Database.GetAllAsync();
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var filtered = _allRecords.AsEnumerable();
        
        // ユーザー名フィルター (完全一致)
        if (!string.IsNullOrWhiteSpace(UserNameFilterEntry.Text))
        {
            var userName = UserNameFilterEntry.Text.Trim();
            filtered = filtered.Where(r => 
                r.Sente == userName || r.Gote == userName
            );
        }
        
        // 日付範囲フィルター
        filtered = filtered.Where(r => 
            r.Date.Date >= StartDatePicker.Date.Date && 
            r.Date.Date <= EndDatePicker.Date.Date
        );
        
        // 並び替え
        var sorted = SortPicker.SelectedIndex switch
        {
            0 => filtered.OrderByDescending(r => r.Date), // 日付(新しい順)
            1 => filtered.OrderBy(r => r.Date),           // 日付(古い順)
            2 => filtered.OrderBy(r => r.Sente)           // ユーザー名(昇順)
                         .ThenBy(r => r.Gote),
            _ => filtered.OrderByDescending(r => r.Date)
        };
        
        KifuList.ItemsSource = sorted.ToList();
    }

    private void OnFilterChanged(object? sender, EventArgs e)
    {
        ApplyFilters();
    }

    private void OnClearFilterClicked(object? sender, EventArgs e)
    {
        UserNameFilterEntry.Text = "";
        StartDatePicker.Date = DateTime.Now.AddYears(-1);
        EndDatePicker.Date = DateTime.Now;
        SortPicker.SelectedIndex = 0;
        ApplyFilters();
    }

    private async void OnUserManagementClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new Pages.UserManagementPage());
    }

    private async void OnStatisticsClicked(object? sender, EventArgs e)
    {
        var activeUser = await App.UserProfileDatabase.GetActiveUserAsync();
        
        if (activeUser == null)
        {
            var result = await DisplayAlert("確認", 
                "ユーザーが登録されていません。\nユーザー管理画面を開きますか?", 
                "はい", "キャンセル");
            
            if (result)
                await Navigation.PushAsync(new Pages.UserManagementPage());
            
            return;
        }
        
        await Navigation.PushAsync(new Pages.StatisticsPage(activeUser));
    }

    private async void OnAddClicked(object sender, EventArgs e)
    {
        var newRecord = new KifuRecord
        {
            Title = "新規棋譜",
            Moves = 0,
            Date = DateTime.Now
        };

        await App.Database.InsertAsync(newRecord);
        await LoadAndFilterData();
    }

    private async void OnImportClicked(object sender, EventArgs e)
    {
        try
        {
            var customFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.iOS, new[] { "public.text" } },
                { DevicePlatform.Android, new[] { "text/plain" } },
                { DevicePlatform.WinUI, new[] { ".txt", ".kif" } }
            });

            var result = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "棋譜ファイルを選択してください",
                FileTypes = customFileType
            });

            if (result == null) return;

            var text = await File.ReadAllTextAsync(result.FullPath);
            await SaveKifuFromText(text, Path.GetFileNameWithoutExtension(result.FileName));
        }
        catch (Exception ex)
        {
            await DisplayAlert("エラー", $"インポートに失敗しました:\n{ex.Message}", "OK");
        }
    }

    private async void OnPasteClicked(object sender, EventArgs e)
    {
        try
        {
            var hasText = await Clipboard.Default.GetTextAsync();
            if (string.IsNullOrWhiteSpace(hasText))
            {
                await DisplayAlert("エラー", "クリップボードにテキストがありません", "OK");
                return;
            }

            var pasted = await Clipboard.Default.GetTextAsync();
            
            if (string.IsNullOrWhiteSpace(pasted))
            {
                await DisplayAlert("エラー", "クリップボードが空です", "OK");
                return;
            }

            await SaveKifuFromText(pasted, "貼り付け棋譜");
        }
        catch (Exception ex)
        {
            await DisplayAlert("エラー", $"貼り付け処理に失敗しました:\n{ex.Message}", "OK");
        }
    }

    private async Task SaveKifuFromText(string text, string defaultTitle)
    {
        try
        {
            var model = KifuParser.Parse(text);
            var kifData = KifuParser.ParseRaw(text);

            string sente = ExtractName(kifData.Header.GetValueOrDefault("先手",
                                      kifData.Header.GetValueOrDefault("下手", "不明")));
            string gote = ExtractName(kifData.Header.GetValueOrDefault("後手",
                                     kifData.Header.GetValueOrDefault("上手", "不明")));
            string dateStr = kifData.Header.GetValueOrDefault("開始日時", "");
            string winner = kifData.Header.GetValueOrDefault("勝者", "");

            // 棋戦名・持ち時間を抽出
            string tournament = kifData.Header.GetValueOrDefault("棋戦", "");
            string timeControl = kifData.Header.GetValueOrDefault("持ち時間", "");

            // 戦法を抽出（複数の可能性のあるキーを試す）
            string senteStrategy = ExtractFirstStrategy(
                kifData.Header.GetValueOrDefault("先手戦型",
                kifData.Header.GetValueOrDefault("先手戦法",
                kifData.Header.GetValueOrDefault("▲戦型", "")))
            );
            string goteStrategy = ExtractFirstStrategy(
                kifData.Header.GetValueOrDefault("後手戦型",
                kifData.Header.GetValueOrDefault("後手戦法",
                kifData.Header.GetValueOrDefault("△戦型", "")))
            );

            string winnerText = winner switch
            {
                "▲" => "先手",
                "△" => "後手",
                _ => ""
            };

            var record = new KifuRecord
            {
                Title = defaultTitle,
                Sente = sente,
                Gote = gote,
                Date = DateTime.TryParse(dateStr, out var d) ? d : DateTime.Now,
                KifuText = text,
                Moves = model.Moves.Count,
                Winner = winnerText,
                Result = kifData.Header.GetValueOrDefault("結末", ""),
                Tournament = tournament,
                TimeControl = timeControl,
                SenteStrategy = senteStrategy,
                GoteStrategy = goteStrategy
            };

            await App.Database.InsertAsync(record);
            await LoadAndFilterData();

            await DisplayAlert("完了", $"{record.DisplayTitle} を保存しました。\n手数: {record.Moves}手", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("エラー", $"棋譜の解析に失敗しました:\n{ex.Message}", "OK");
            System.Console.WriteLine($"Error: {ex}");
        }
    }

    private string ExtractName(string fullText)
    {
        var match = Regex.Match(fullText, @"^([^\s]+)");
        return match.Success ? match.Groups[1].Value : fullText;
    }

    private string ExtractFirstStrategy(string strategies)
    {
        if (string.IsNullOrWhiteSpace(strategies))
            return "";
        
        var parts = strategies.Split(new[] { ',', '、', '，' }, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[0].Trim() : strategies.Trim();
    }

    private async void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection == null || e.CurrentSelection.Count == 0) return;
        var item = e.CurrentSelection[0] as KifuRecord;
        if (item == null) return;

        await Navigation.PushAsync(new Pages.KifuDetailPage(item));

        ((CollectionView)sender).SelectedItem = null;
    }
}

public class StringNotEmptyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        return !string.IsNullOrEmpty(value as string);
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}