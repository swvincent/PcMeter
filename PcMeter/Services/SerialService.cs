using System.IO;
using System.IO.Ports;
using System.Windows.Threading;

namespace PcMeter.Services;

public class SerialService
{
    private SerialPort? _port;
    private readonly Dispatcher _dispatcher;

    // (message, isSleepResumeError)
    public event Action<string, bool>? ErrorOccurred;

    public bool IsConnected => _port?.IsOpen == true;

    public SerialService()
    {
        _dispatcher = Dispatcher.CurrentDispatcher;
    }

    public bool Connect(string portName)
    {
        try
        {
            _port = new SerialPort(portName, 9600)
            {
                ReadTimeout = 500,
                WriteTimeout = 500
            };
            _port.Open();
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            ErrorOccurred?.Invoke(
                $"Access to {portName} is denied. It may already be in use by another application or process.", false);
            _port = null;
            return false;
        }
        catch (IOException)
        {
            ErrorOccurred?.Invoke(
                $"{portName} could not be opened. Check to be sure that it is a valid COM port.", false);
            _port = null;
            return false;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(ex.Message, false);
            _port = null;
            return false;
        }
    }

    public void Disconnect()
    {
        if (_port == null) return;
        try
        {
            if (_port.IsOpen)
                _port.Dispose();
        }
        catch (IOException)
        {
            // Silently ignore — can occur when USB virtual COM port was unplugged before Dispose
        }
        finally
        {
            _port = null;
        }
    }

    public void TrySend(int cpu, int mem)
    {
        if (!IsConnected) return;
        try
        {
            _port!.Write($"C{cpu}\rM{mem}\r");
        }
        catch (IOException ex)
        {
            bool isSleepResume = ex.Message.Contains("A device attached to the system is not functioning");
            string message = ex.Message;
            _dispatcher.Invoke(() => ErrorOccurred?.Invoke(message, isSleepResume));
        }
        catch (Exception ex)
        {
            string message = ex.Message;
            _dispatcher.Invoke(() => ErrorOccurred?.Invoke(message, false));
        }
    }
}
