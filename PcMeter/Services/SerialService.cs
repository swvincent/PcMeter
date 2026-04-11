using System.IO;
using System.IO.Ports;
using System.Windows.Threading;

namespace PcMeter.Services;

public class SerialService : IDisposable
{
    private SerialPort? _port;
    private readonly Dispatcher _dispatcher;

    // (message)
    public event Action<string>? ErrorOccurred;

    // Fired when the connection is lost mid-session (unplug, sleep/resume, etc.).
    // No dialog is shown; the app auto-reconnects on the next timer tick.
    public event Action? ConnectionLost;

    public bool IsConnected => _port?.IsOpen == true;

    public SerialService()
    {
        _dispatcher = Dispatcher.CurrentDispatcher;
    }

    // Pass reportError: false during silent auto-reconnect attempts to suppress error dialogs.
    public bool Connect(string portName, bool reportError = true)
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
            if (reportError)
                ErrorOccurred?.Invoke(
                    $"Access to {portName} is denied. It may already be in use by another application or process.");
            _port?.Dispose();
            _port = null;
            return false;
        }
        catch (IOException)
        {
            if (reportError)
                ErrorOccurred?.Invoke(
                    $"{portName} could not be opened. Check to be sure that it is a valid COM port.");
            _port?.Dispose();
            _port = null;
            return false;
        }
        catch (Exception ex)
        {
            if (reportError)
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
        catch (Exception ex) when (ex is IOException or TimeoutException)
        {
            // Any IO failure or write timeout means the connection is broken (unplug, sleep/resume, etc.).
            // Disconnect and signal App to auto-reconnect on the next timer tick.
            Disconnect();
            _dispatcher.Invoke(() => ConnectionLost?.Invoke());
        }
        catch (Exception ex)
        {
            Disconnect();
            _dispatcher.Invoke(() => ErrorOccurred?.Invoke(ex.Message));
        }
    }

    #region IDisposable Support

    private bool disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _port?.Dispose();
            }

            // free unmanaged resources (unmanaged objects) and override finalizer
            // set large fields to null
            disposedValue = true;
        }
    }

    // // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~SerialService()
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
