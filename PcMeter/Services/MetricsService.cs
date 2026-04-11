using System.Diagnostics;

namespace PcMeter.Services;

public class MetricsService : IDisposable
{
    private readonly PerformanceCounter _cpuCounter;

    public MetricsService()
    {
        // Throws if performance counters are unavailable — caller handles
        _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

        // First call always returns 0; prime it so the first real read is accurate
        _cpuCounter.NextValue();
    }

    public int GetCpuPercent()
    {
        return (int)Math.Round(_cpuCounter.NextValue(),
            MidpointRounding.AwayFromZero);
    }

    public int GetMemPercent()
    {
        var (available, total) = PsApiWrapper.QueryMemory();

        if (total == 0)
            return 0;
        
        return (int)Math.Round(100 - ((decimal)available / (decimal)total * 100),
            MidpointRounding.AwayFromZero);
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
                _cpuCounter.Dispose();
                PerformanceCounter.CloseSharedResources();
            }

            // free unmanaged resources (unmanaged objects) and override finalizer
            // set large fields to null
            disposedValue = true;
        }
    }

    // // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~MetricsService()
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
