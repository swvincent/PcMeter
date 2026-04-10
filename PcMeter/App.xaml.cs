using System.IO.Ports;
using System.Windows;
using System.Windows.Threading;
using PcMeter.Navigation;
using PcMeter.Services;

namespace PcMeter;

public partial class App : Application
{
    private const string MutexName = "Local\\PcMeter-8F3A2B1C-4D5E-6F7A-8B9C-0D1E2F3A4B5C";

    private Mutex? _singleInstanceMutex;
    private readonly AppSettings _settings = AppSettings.Load();
    private MetricsService? _metrics;
    private SerialService? _serial;
    private DispatcherTimer? _timer;
    private TrayMenu? _menu;

    // Child windows (single-instance pattern)
    private Views.SettingsWindow? _settingsWindow;
    private Views.AboutWindow? _aboutWindow;

    private bool _userDisconnected;

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

        _menu = new TrayMenu();
        _menu.ConnectMenuItem.Click += (_, _) => OnConnectMenuClick();
        _menu.SettingsMenuItem.Click += (_, _) => OnSettingsMenuClick();
        _menu.AboutMenuItem.Click += (_, _) => OnAboutMenuClick();
        _menu.ExitMenuItem.Click += (_, _) => OnExitMenuClick();

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

        _menu!.UpdateCpuMem(cpu, mem);

        if (_serial!.IsConnected)
        {
            _serial.TrySend(cpu, mem);
            // Detect silent unplug: USB serial drivers buffer writes, so no exception is thrown.
            // If the port has disappeared from the system, treat it as a lost connection.
            // Guard against TrySend having already disconnected (e.g. via IOException), which
            // would make the GetPortNames call redundant and RefreshMenuState fire twice.
            if (_serial.IsConnected && !SerialPort.GetPortNames().Contains(_settings.ComPort))
            {
                _serial.Disconnect();
                RefreshMenuState();
            }
        }
        else
        {
            // Update menu if out of sync. Happens if silent unplug wasn't caught while still showing connected.
            if (_menu.ConnectMenuItem.IsChecked)
                RefreshMenuState();

            if (_userDisconnected)
                return;

            // Auto-reconnect silently after unplug or sleep/resume
            bool reconnected = _serial.Connect(_settings.ComPort, reportError: false);
            
            if (reconnected)
            {
                _menu.ShowNotification($"Reconnected to {_settings.ComPort}");
                RefreshMenuState();
            }
        }
    }

    private void TryConnect()
    {
        bool connected = _serial!.Connect(_settings.ComPort);
        if (connected)
            _menu!.ShowNotification($"PC Meter connected to {_settings.ComPort}");

        RefreshMenuState();
    }

    private void RefreshMenuState()
    {
        bool connected = _serial?.IsConnected == true;
        _menu!.RefreshMenuState(connected);
    }

    private void OnSerialConnectionLost()
    {
        // Connection dropped (unplug, sleep/resume) — update UI; timer auto-reconnects each tick.
        _userDisconnected = false;
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
            _userDisconnected = true;
            _serial.Disconnect();
            RefreshMenuState();
        }
        else
        {
            _userDisconnected = false;
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
            _settingsWindow.Activate();
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
            _aboutWindow.Activate();
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
        _menu?.Dispose();
        _singleInstanceMutex?.ReleaseMutex();
        base.OnExit(e);
    }
}
