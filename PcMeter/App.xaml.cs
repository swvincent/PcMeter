using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using H.NotifyIcon;
using H.NotifyIcon.Core;
using PcMeter.Services;

namespace PcMeter;

public partial class App : Application
{
    private static readonly string MutexName = "Local\\PcMeter-8F3A2B1C-4D5E-6F7A-8B9C-0D1E2F3A4B5C";

    private Mutex? _singleInstanceMutex;
    private AppSettings _settings = new();
    private MetricsService? _metrics;
    private SerialService? _serial;
    private TaskbarIcon? _trayIcon;
    private DispatcherTimer? _timer;

    // Named menu item refs
    private MenuItem? _cpuMenuItem;
    private MenuItem? _memMenuItem;
    private MenuItem? _connectMenuItem;
    private MenuItem? _settingsMenuItem;

    // Child windows (single-instance pattern)
    private Views.SettingsWindow? _settingsWindow;
    private Views.AboutWindow? _aboutWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // Single instance enforcement
        _singleInstanceMutex = new Mutex(true, MutexName, out bool isNewInstance);
        if (!isNewInstance)
        {
            MessageBox.Show("Another instance of PC Meter is already running. Program will close.",
                "PC Meter Already Running", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        // Load settings
        _settings = AppSettings.Load();

        // Initialize metrics
        try
        {
            _metrics = new MetricsService();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"The performance counter(s) could not be initialized. The program cannot continue.\n\nDetails: {ex.Message}",
                "PC Meter Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }

        // Initialize serial service
        _serial = new SerialService();
        _serial.ErrorOccurred += OnSerialError;

        // Get tray icon and named menu items from resources
        _trayIcon = (TaskbarIcon)Resources["TrayIcon"];
        var cm = _trayIcon.ContextMenu!;
        _cpuMenuItem      = (MenuItem)cm.FindName("CpuMenuItem");
        _memMenuItem      = (MenuItem)cm.FindName("MemMenuItem");
        _connectMenuItem  = (MenuItem)cm.FindName("ConnectMenuItem");
        _settingsMenuItem = (MenuItem)cm.FindName("SettingsMenuItem");

        // Set up timer
        _timer = new DispatcherTimer(DispatcherPriority.Normal, Dispatcher.CurrentDispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _timer.Tick += OnTimerTick;

        // Connect and start
        TryConnect();
        _timer.Start();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        int cpu = _metrics!.GetCpuPercent();
        int mem = _metrics.GetMemPercent();

        if (_cpuMenuItem != null) _cpuMenuItem.Header = $"CPU: {cpu}%";
        if (_memMenuItem != null) _memMenuItem.Header = $"Memory: {mem}%";

        _serial!.TrySend(cpu, mem);
    }

    private void TryConnect()
    {
        bool connected = _serial!.Connect(_settings.ComPort);
        if (connected && _trayIcon != null)
        {
            _trayIcon.ShowNotification("PC Meter",
                $"PC Meter connected to {_settings.ComPort}",
                NotificationIcon.Info);
        }
        RefreshMenuState();
    }

    private void RefreshMenuState()
    {
        bool connected = _serial?.IsConnected == true;
        if (_connectMenuItem != null)
        {
            _connectMenuItem.Header = connected ? "_Connected" : "_Connect";
            _connectMenuItem.IsChecked = connected;
        }
        if (_settingsMenuItem != null)
            _settingsMenuItem.IsEnabled = !connected;
    }

    private void OnSerialError(string message, bool isSleepResumeError)
    {
        if (isSleepResumeError)
            return;

        _timer?.Stop();
        _serial?.Disconnect();
        MessageBox.Show($"Communication with the device has been lost. Has it been unplugged?\n\nDetails: {message}",
            "PC Meter Error", MessageBoxButton.OK, MessageBoxImage.Error);
        RefreshMenuState();
        _timer?.Start();
    }

    // Called by TrayContextMenu code-behind
    public void OnConnectMenuClick()
    {
        if (_serial?.IsConnected == true)
        {
            _serial.Disconnect();
            RefreshMenuState();
        }
        else
        {
            TryConnect();
        }
    }

    public void OnSettingsMenuClick()
    {
        if (_settingsWindow == null)
        {
            _settingsWindow = new Views.SettingsWindow(_settings);
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
            _settingsWindow.Show();
        }
        else
        {
            _settingsWindow.Activate();
        }
    }

    public void OnAboutMenuClick()
    {
        if (_aboutWindow == null)
        {
            _aboutWindow = new Views.AboutWindow();
            _aboutWindow.Closed += (_, _) => _aboutWindow = null;
            _aboutWindow.Show();
        }
        else
        {
            _aboutWindow.Activate();
        }
    }

    public void OnExitMenuClick()
    {
        _timer?.Stop();
        _serial?.Disconnect();
        _metrics?.Dispose();
        _trayIcon?.Dispose();
        _singleInstanceMutex?.ReleaseMutex();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        _singleInstanceMutex?.ReleaseMutex();
        base.OnExit(e);
    }
}
