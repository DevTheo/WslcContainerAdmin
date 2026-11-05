using Avalonia.Controls;

namespace WslcContainerAdmin;

public partial class MainWindow : Window
{
    private WindowState _stateBeforeMinimize = WindowState.Normal;
    public MainWindow()
    {
        InitializeComponent();
        _stateBeforeMinimize = WindowState;
        PropertyChanged += (_, _) =>
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
            }
            else
            {
                _stateBeforeMinimize = WindowState;
            }
        };
    }

    public void RestoreFromTray()
    {
        WindowState = _stateBeforeMinimize;
        Show();
        Activate();
    }
}
