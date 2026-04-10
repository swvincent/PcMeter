using H.NotifyIcon;
using H.NotifyIcon.Core;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace PcMeter.Navigation;

internal class TrayMenu : IDisposable
{
    public TrayMenu()
    {
        TrayIcon = CreateTrayIcon();
    }

    public TaskbarIcon TrayIcon { get; set; }
    private MenuItem? CpuMenuItem { get; set; }
    private MenuItem? MemMenuItem { get; set; }
    public MenuItem? ConnectMenuItem { get; set; }
    public MenuItem? SettingsMenuItem { get; set; }
    public MenuItem? AboutMenuItem { get; set; }
    public MenuItem? ExitMenuItem { get; set; }

    private TaskbarIcon CreateTrayIcon()
    {
        CpuMenuItem = new MenuItem { Header = "CPU: ?", IsEnabled = false };
        MemMenuItem = new MenuItem { Header = "Memory: ?", IsEnabled = false };

        ConnectMenuItem = new MenuItem { Header = "_Connect", IsCheckable = true };
        SettingsMenuItem = new MenuItem { Header = "_Settings" };
        AboutMenuItem = new MenuItem { Header = "_About" };
        ExitMenuItem = new MenuItem { Header = "E_xit" };

        //ConnectMenuItem.Click += (_, _) => OnConnectMenuClick();
        //SettingsMenuItem.Click += (_, _) => OnSettingsMenuClick();
        //aboutItem.Click += (_, _) => OnAboutMenuClick();
        //exitItem.Click += (_, _) => OnExitMenuClick();

        var contextMenu = new ContextMenu();
        contextMenu.Items.Add(CpuMenuItem);
        contextMenu.Items.Add(MemMenuItem);
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
        CpuMenuItem!.Header = $"CPU: {cpu}%";
        MemMenuItem!.Header = $"Memory: {mem}%";
    }

    public void RefreshMenuState(bool connected)
    {
        ConnectMenuItem!.Header = connected ? "_Connected" : "_Connect";
        ConnectMenuItem.IsChecked = connected;
        SettingsMenuItem!.IsEnabled = !connected;
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
                TrayIcon.Dispose();
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
