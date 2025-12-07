// File: Pages/KifuListPage.xaml.cs
using ShogiKifuApp.ViewModels;

namespace ShogiKifuApp.Pages;

public partial class KifuListPage : ContentPage
{
    private readonly KifuListViewModel _vm;

    public KifuListPage()
    {
        InitializeComponent();
        _vm = new KifuListViewModel();
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.InitializeAsync();
    }
}
