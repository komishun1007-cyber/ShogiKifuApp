using ShogiKifuApp.Models;
using System.Text.RegularExpressions;

namespace ShogiKifuApp;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        KifuList.ItemsSource = await App.Database.GetAllAsync();
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
        KifuList.ItemsSource = await App.Database.GetAllAsync();
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
            string pasted = await DisplayPromptAsync("棋譜貼り付け", "棋譜の内容を貼り付けてください：", 
                                                     accept: "保存", cancel: "キャンセル", maxLength: 10000);

            if (string.IsNullOrWhiteSpace(pasted)) return;

            await SaveKifuFromText(pasted, "貼り付け棋譜");
        }
        catch (Exception ex)
        {
            await DisplayAlert("エラー", $"貼り付け処理に失敗しました:\n{ex.Message}", "OK");
        }
    }

    private async Task SaveKifuFromText(string text, string defaultTitle)
    {
        string sente = Regex.Match(text, @"先手[:：](.+)").Groups[1].Value.Trim();
        string gote = Regex.Match(text, @"後手[:：](.+)").Groups[1].Value.Trim();
        string date = Regex.Match(text, @"開始日時[:：](.+)").Groups[1].Value.Trim();

        var record = new KifuRecord
        {
            Title = defaultTitle,
            Sente = string.IsNullOrEmpty(sente) ? "不明" : sente,
            Gote = string.IsNullOrEmpty(gote) ? "不明" : gote,
            Date = DateTime.TryParse(date, out var d) ? d : DateTime.Now,
            KifuText = text,
            Moves = Regex.Matches(text, @"\d+\s").Count
        };

        await App.Database.InsertAsync(record);
        KifuList.ItemsSource = await App.Database.GetAllAsync();

        await DisplayAlert("完了", $"{record.Title} を保存しました。", "OK");
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