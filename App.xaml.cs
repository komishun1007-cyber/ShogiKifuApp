using ShogiKifuApp.Data;

namespace ShogiKifuApp;

public partial class App : Application
{
    private static KifuDatabase? _database;
    private static UserProfileDatabase? _userProfileDatabase;

    public static KifuDatabase Database =>
        _database ??= new KifuDatabase();
    
    public static UserProfileDatabase UserProfileDatabase =>
        _userProfileDatabase ??= new UserProfileDatabase();

    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}