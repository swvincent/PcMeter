using System.IO.Ports;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using H.NotifyIcon;
using H.NotifyIcon.Core;
using PcMeter.Services;

namespace PcMeter;

public partial class App : Application
{
    private const string MutexName = "Local\\PcMeter-8F3A2B1C-4D5E-6F7A-8B9C-0D1E2F3A4B5C";

    private Mutex? _singleInstanceMutex;
    private readonly AppSettings _settings = AppSettings.Load();
    private MetricsService? _metrics;
    private SerialService? _serial;
    private TaskbarIcon? _trayIcon;
    private DispatcherTimer? _timer;

    // Named menu item refs — set in CreateTrayIcon()
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

        // Initialize metrics
        try
        {
            _metrics = new MetricsService();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"The performance counter(s) could not be initialized. The program cannot continue.\n\nDetails: {ex.Message}",
                "PC Meter Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }

        // Initialize serial service
        _serial = new SerialService();
        _serial.ErrorOccurred += OnSerialError;
        _serial.ConnectionLost += OnSerialConnectionLost;

        // Build tray icon and context menu entirely in code
        _trayIcon = CreateTrayIcon();

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

    private TaskbarIcon CreateTrayIcon()
    {
        _cpuMenuItem = new MenuItem { Header = "CPU: ?", IsEnabled = false };
        _memMenuItem = new MenuItem { Header = "Memory: ?", IsEnabled = false };

        _connectMenuItem = new MenuItem { Header = "_Connect", IsCheckable = true };
        _connectMenuItem.Click += (_, _) => OnConnectMenuClick();

        _settingsMenuItem = new MenuItem { Header = "_Settings" };
        _settingsMenuItem.Click += (_, _) => OnSettingsMenuClick();

        var aboutItem = new MenuItem { Header = "_About" };
        aboutItem.Click += (_, _) => OnAboutMenuClick();

        var exitItem = new MenuItem { Header = "E_xit" };
        exitItem.Click += (_, _) => OnExitMenuClick();

        var contextMenu = new ContextMenu();
        contextMenu.Items.Add(_cpuMenuItem);
        contextMenu.Items.Add(_memMenuItem);
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(_connectMenuItem);
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(_settingsMenuItem);
        contextMenu.Items.Add(aboutItem);
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(exitItem);

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

    private void OnTimerTick(object? sender, EventArgs e)
    {
        int cpu = _metrics!.GetCpuPercent();
        int mem = _metrics.GetMemPercent();

        _cpuMenuItem!.Header = $"CPU: {cpu}%";
        _memMenuItem!.Header = $"Memory: {mem}%";

        if (_serial!.IsConnected)
        {
            _serial.TrySend(cpu, mem);
        }
        else
        {
            // Silect disconnects happen, which go undetected w/o exception. Update menu if out of sync.
            if (_connectMenuItem is not null && _connectMenuItem.IsChecked)
                RefreshMenuState();

            // Auto-reconnect silently after unplug or sleep/resume
            bool reconnected = _serial.Connect(_settings.ComPort, reportError: false);
            
            if (reconnected)
            {
                _trayIcon!.ShowNotification("PC Meter",
                    $"Reconnected to {_settings.ComPort}",
                    NotificationIcon.Info);
                RefreshMenuState();
            }
        }
    }

    private void TryConnect()
    {
        bool connected = _serial!.Connect(_settings.ComPort);
        if (connected)
        {
            _trayIcon!.ShowNotification("PC Meter",
                $"PC Meter connected to {_settings.ComPort}",
                NotificationIcon.Info);
        }
        RefreshMenuState();
    }

    private void RefreshMenuState()
    {
        bool connected = _serial?.IsConnected == true;
        _connectMenuItem!.Header = connected ? "_Connected" : "_Connect";
        _connectMenuItem.IsChecked = connected;
        _settingsMenuItem!.IsEnabled = !connected;
    }

    private void OnSerialConnectionLost()
    {
        // Connection dropped (unplug, sleep/resume) — update UI; timer auto-reconnects each tick.
        RefreshMenuState();
    }

    private bool _showingError;

    private void OnSerialError(string message)
    {
        if (_showingError) return;
        _showingError = true;

        _timer?.Stop();
        _serial?.Disconnect();

        // A topmost helper window as owner ensures the dialog appears above other windows.
        // Without an owner, ownerless dialogs in tray apps can appear behind the focused window.
        var helper = new Window
        {
            WindowStyle = WindowStyle.None,
            ShowInTaskbar = false,
            Topmost = true,
            Width = 0,
            Height = 0
        };

        helper.Show();

        MessageBox.Show(helper,
            $"A serial port communication error occurred:\n\n{message}",
            "PC Meter Error", MessageBoxButton.OK, MessageBoxImage.Error);
        helper.Close();

        _showingError = false;
        RefreshMenuState();
        _timer?.Start();
    }

    private void OnConnectMenuClick()
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

    private void OnSettingsMenuClick()
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

    private void OnAboutMenuClick()
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

    private void OnExitMenuClick()
    {
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _timer?.Stop();
        _serial?.Disconnect();
        _metrics?.Dispose();
        _trayIcon?.Dispose();
        _singleInstanceMutex?.ReleaseMutex();
        base.OnExit(e);
    }
}
