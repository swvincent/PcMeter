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
        _connectMenuItem = new MenuItem { Header = "_Connect", IsCheckable = true };
        _settingsMenuItem = new MenuItem { Header = "_Settings" };
        _aboutMenuItem = new MenuItem { Header = "_About" };
        _exitMenuItem = new MenuItem { Header = "E_xit" };

        _trayIcon = CreateTrayIcon();
    }

    public event Action? ConnectClicked;
    public event Action? SettingsClicked;
    public event Action? AboutClicked;
    public event Action? ExitClicked;

    readonly TaskbarIcon _trayIcon;
    readonly MenuItem _cpuMenuItem;
    readonly MenuItem _memMenuItem;
    readonly MenuItem _connectMenuItem;
    readonly MenuItem _settingsMenuItem;
    readonly MenuItem _aboutMenuItem;
    readonly MenuItem _exitMenuItem;

    private TaskbarIcon CreateTrayIcon()
    {
        var contextMenu = new ContextMenu();
        contextMenu.Items.Add(_cpuMenuItem);
        contextMenu.Items.Add(_memMenuItem);
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(_connectMenuItem);
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(_settingsMenuItem);
        contextMenu.Items.Add(_aboutMenuItem);
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(_exitMenuItem);

        _connectMenuItem.Click += (_, _) => ConnectClicked?.Invoke();
        _settingsMenuItem.Click += (_, _) => SettingsClicked?.Invoke();
        _aboutMenuItem.Click += (_, _) => AboutClicked?.Invoke();
        _exitMenuItem.Click += (_, _) => ExitClicked?.Invoke();

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
        _cpuMenuItem.Header = $"CPU: {cpu}%";
        _memMenuItem.Header = $"Memory: {mem}%";
    }

    public void RefreshMenuState(bool connected)
    {
        _connectMenuItem.Header = connected ? "_Connected" : "_Connect";
        _connectMenuItem.IsChecked = connected;
        _settingsMenuItem.IsEnabled = !connected;
    }

    public void ShowNotification(string message, NotificationIcon icon = NotificationIcon.Info)
    {
        _trayIcon.ShowNotification("PC Meter",
                    message,
                    icon);
    }

    public bool IsShowingConnected => _connectMenuItem.IsChecked;

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
