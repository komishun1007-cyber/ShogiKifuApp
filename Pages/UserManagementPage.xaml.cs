using ShogiKifuApp.Models;

namespace ShogiKifuApp.Pages;

public partial class UserManagementPage : ContentPage
{
    public UserManagementPage()
    {
        InitializeComponent();
        
        // Converters追加
        Resources.Add("ActiveMarkConverter", new ActiveMarkConverter());
        Resources.Add("StringNotEmptyConverter", new StringNotEmptyConverter());
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadUsers();
    }

    private async Task LoadUsers()
    {
        var users = await App.UserProfileDatabase.GetAllAsync();
        UserList.ItemsSource = users;
    }

    private async void OnAddUserClicked(object? sender, EventArgs e)
    {
        string userName = await DisplayPromptAsync("新規ユーザー登録", 
            "ユーザー名を入力してください", 
            placeholder: "例: user1234");
        
        if (string.IsNullOrWhiteSpace(userName)) return;
        
        string notes = await DisplayPromptAsync("メモ (任意)", 
            "このユーザーのメモを入力してください", 
            placeholder: "例: 将棋ウォーズ用",
            cancel: "スキップ") ?? "";
        
        var newUser = new UserProfile
        {
            UserName = userName.Trim(),
            Notes = notes.Trim(),
            IsActive = false
        };
        
        // 最初のユーザーは自動的にアクティブに
        var existingUsers = await App.UserProfileDatabase.GetAllAsync();
        if (existingUsers.Count == 0)
        {
            newUser.IsActive = true;
        }
        
        await App.UserProfileDatabase.InsertAsync(newUser);
        await LoadUsers();
        
        await DisplayAlert("完了", $"ユーザー「{userName}」を登録しました", "OK");
    }

    private async void OnEditClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button || button.CommandParameter is not UserProfile user)
            return;
        
        string newName = await DisplayPromptAsync("ユーザー名編集", 
            "新しいユーザー名を入力してください", 
            initialValue: user.UserName);
        
        if (string.IsNullOrWhiteSpace(newName)) return;
        
        string newNotes = await DisplayPromptAsync("メモ編集", 
            "新しいメモを入力してください", 
            initialValue: user.Notes,
            cancel: "スキップ") ?? user.Notes;
        
        user.UserName = newName.Trim();
        user.Notes = newNotes.Trim();
        
        await App.UserProfileDatabase.UpdateAsync(user);
        await LoadUsers();
        
        await DisplayAlert("完了", "ユーザー情報を更新しました", "OK");
    }

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button || button.CommandParameter is not UserProfile user)
            return;
        
        bool confirm = await DisplayAlert("確認", 
            $"ユーザー「{user.UserName}」を削除しますか?", 
            "削除", "キャンセル");
        
        if (!confirm) return;
        
        await App.UserProfileDatabase.DeleteAsync(user);
        await LoadUsers();
        
        await DisplayAlert("完了", "ユーザーを削除しました", "OK");
    }

    private async void OnUserSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection == null || e.CurrentSelection.Count == 0) return;
        
        var selectedUser = e.CurrentSelection[0] as UserProfile;
        if (selectedUser == null) return;
        
        // 選択中ユーザーを切り替え
        await App.UserProfileDatabase.SetActiveUserAsync(selectedUser.Id);
        await LoadUsers();
        
        await DisplayAlert("完了", $"「{selectedUser.UserName}」を選択しました", "OK");
        
        ((CollectionView)sender).SelectedItem = null;
    }
}

// Converters
public class ActiveMarkConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        return (value is bool isActive && isActive) ? "✓" : "";
    }
    
    public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
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