using H.NotifyIcon;
using H.NotifyIcon.Core;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace PcMeter.Navigation;

internal class TrayMenu : IDisposable
{
    public TrayMenu()
    {
        _cpuMenuItem = new MenuItem { Header = "CPU: ?", IsEnabled = false };
        _memMenuItem = new MenuItem { Header = "Memory: ?", IsEnabled = false };
        ConnectMenuItem = new MenuItem { Header = "_Connect", IsCheckable = true };
        SettingsMenuItem = new MenuItem { Header = "_Settings" };
        AboutMenuItem = new MenuItem { Header = "_About" };
        ExitMenuItem = new MenuItem { Header = "E_xit" };

        _trayIcon = CreateTrayIcon();
    }

    public MenuItem ConnectMenuItem { get; set; }
    public MenuItem SettingsMenuItem { get; set; }
    public MenuItem AboutMenuItem { get; set; }
    public MenuItem ExitMenuItem { get; set; }

    readonly TaskbarIcon _trayIcon;
    readonly MenuItem _cpuMenuItem;
    readonly MenuItem _memMenuItem;

    private TaskbarIcon CreateTrayIcon()
    {
        var contextMenu = new ContextMenu();
        contextMenu.Items.Add(_cpuMenuItem);
        contextMenu.Items.Add(_memMenuItem);
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(ConnectMenuItem);
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(SettingsMenuItem);
        contextMenu.Items.Add(AboutMenuItem);
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(ExitMenuItem);

        var icon = new TaskbarIcon
        {
            IconSource = new BitmapImage(new Uri("pack://application:,,,/Assets/pcmeter.ico")),
            ToolTipText = "PC Meter",
            ContextMenu = contextMenu,
            MenuActivation = PopupActivationMode.LeftOrRightClick
        };

        // Required when TaskbarIcon is created in code rather than being part of a visual tree.
        // Without this, the icon never registers with the Windows system tray.
        icon.ForceCreate(enablesEfficiencyMode: false);

        return icon;
    }

    public void UpdateCpuMem(int cpu, int mem)
    {
        _cpuMenuItem!.Header = $"CPU: {cpu}%";
        _memMenuItem!.Header = $"Memory: {mem}%";
    }

    public void RefreshMenuState(bool connected)
    {
        ConnectMenuItem!.Header = connected ? "_Connected" : "_Connect";
        ConnectMenuItem.IsChecked = connected;
        SettingsMenuItem!.IsEnabled = !connected;
    }

    public void ShowNotification(string message, NotificationIcon icon = NotificationIcon.Info)
    {
        _trayIcon.ShowNotification("PC Meter",
                    message,
                    icon);
    }

    #region IDisposable Support

    private bool disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // dispose managed state (managed objects)
                _trayIcon.Dispose();
            }

            // free unmanaged resources (unmanaged objects) and override finalizer
            // set large fields to null
            disposedValue = true;
        }
    }

    // // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~TrayMenu()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion

}
