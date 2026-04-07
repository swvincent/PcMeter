using System.IO;
using System.IO.Ports;
using System.Windows.Threading;

namespace PcMeter.Services;

public class SerialService
{
    private SerialPort? _port;
    private readonly Dispatcher _dispatcher;

    // (message)
    public event Action<string>? ErrorOccurred;

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
                $"Access to {portName} is denied. It may already be in use by another application or process.");
            _port?.Dispose();
            _port = null;
            return false;
        }
        catch (IOException)
        {
            ErrorOccurred?.Invoke(
                $"{portName} could not be opened. Check to be sure that it is a valid COM port.");
            _port?.Dispose();
            _port = null;
            return false;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(ex.Message);
            _port?.Dispose();
            _port = null;
            return false;
        }
    }

    public void Disconnect()
    {
        if (_port == null) return;
        try
        {
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
            // Detect transient sleep/resume disconnects via Win32 error code rather than the
            // exception message, which is localized and varies across Windows language editions.
            // ERROR_GEN_FAILURE (31 / 0x1F) is raised when a USB virtual COM port loses its
            // connection during sleep/resume. The HResult upper bits encode HRESULT facility
            // info, so mask them off to get the underlying Win32 code.
            bool isSleepResume = (ex.HResult & 0xFFFF) == 31;

            if (!isSleepResume)
                _dispatcher.Invoke(() => ErrorOccurred?.Invoke(ex.Message));
        }
        catch (Exception ex)
        {
            _dispatcher.Invoke(() => ErrorOccurred?.Invoke(ex.Message));
        }
    }
}
