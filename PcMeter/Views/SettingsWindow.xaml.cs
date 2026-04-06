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

        // Pre-select the saved port if it is in the list, otherwise select the first
        ComPortComboBox.SelectedItem = ports.Contains(_settings.ComPort)
            ? _settings.ComPort
            : ports[0];
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (ComPortComboBox.SelectedItem is not string selectedPort)
        {
            ValidationText.Visibility = Visibility.Visible;
            return;
        }

        _settings.ComPort = selectedPort;
        _settings.Save();
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
