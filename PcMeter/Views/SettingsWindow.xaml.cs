using System.IO.Ports;
using System.Windows;
using PcMeter.Services;

namespace PcMeter.Views;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;

    public SettingsWindow(AppSettings settings)
    {
        _settings = settings;
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var ports = SerialPort.GetPortNames().OrderBy(p => p).ToList();

        if (ports.Count == 0)
        {
            MessageBox.Show(
                "No COM ports were found. Make sure the PC Meter device is connected and drivers are installed.",
                "No COM Ports", MessageBoxButton.OK, MessageBoxImage.Warning);
            ComPortComboBox.IsEnabled = false;
            OkButton.IsEnabled = false;
            return;
        }

        ComPortComboBox.ItemsSource = ports;

        // Pre-select the saved port if it is in the list, otherwise warn and select the first
        if (ports.Contains(_settings.ComPort))
        {
            ComPortComboBox.SelectedItem = _settings.ComPort;
        }
        else
        {
            ComPortComboBox.SelectedItem = ports[0];
            MessageBox.Show(this,
                $"Saved port {_settings.ComPort} was not found. Please select the correct port.",
                "Port Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        _settings.ComPort = (string)ComPortComboBox.SelectedItem;
        _settings.Save();
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
