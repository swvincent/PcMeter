using System.Windows;
using System.Windows.Controls;

namespace PcMeter.TrayIcon;

public partial class TrayContextMenu : ResourceDictionary
{
    private void ConnectMenuItem_Click(object sender, RoutedEventArgs e)
        => ((App)Application.Current).OnConnectMenuClick();

    private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
        => ((App)Application.Current).OnSettingsMenuClick();

    private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        => ((App)Application.Current).OnAboutMenuClick();

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        => ((App)Application.Current).OnExitMenuClick();
}
