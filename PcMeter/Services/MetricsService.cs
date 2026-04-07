using System.Diagnostics;

namespace PcMeter.Services;

public class MetricsService : IDisposable
{
    private readonly PerformanceCounter _cpuCounter;

    public MetricsService()
    {
        // Throws if performance counters are unavailable — caller handles
        _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
    }

    public int GetCpuPercent()
    {
        return (int)Math.Round(_cpuCounter.NextValue(), MidpointRounding.AwayFromZero);
    }

    public int GetMemPercent()
    {
        var (available, total) = PsApiWrapper.QueryMemory();
        if (total == 0) return 0;
        return (int)Math.Round(100 - ((decimal)available / (decimal)total * 100), MidpointRounding.AwayFromZero);
    }

    public void Dispose()
    {
        _cpuCounter.Dispose();
        PerformanceCounter.CloseSharedResources();
    }
}
