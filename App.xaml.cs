using ShogiKifuApp.Data;

namespace ShogiKifuApp;

public partial class App : Application
{
    private static KifuDatabase? _database;

    public static KifuDatabase Database =>
        _database ??= new KifuDatabase();

    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}