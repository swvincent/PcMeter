using System.Diagnostics;
using System.Runtime.InteropServices;

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

    private static class PsApiWrapper
    {
        [DllImport("psapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetPerformanceInfo(out PsApiPerformanceInformation info, int size);

        [StructLayout(LayoutKind.Sequential)]
        private struct PsApiPerformanceInformation
        {
            public int Size;
            public IntPtr CommitTotal;
            public IntPtr CommitLimit;
            public IntPtr CommitPeak;
            public IntPtr PhysicalTotal;
            public IntPtr PhysicalAvailable;
            public IntPtr SystemCache;
            public IntPtr KernelTotal;
            public IntPtr KernelPaged;
            public IntPtr KernelNonPaged;
            public IntPtr PageSize;
            public int HandlesCount;
            public int ProcessCount;
            public int ThreadCount;
        }

        public static (long available, long total) QueryMemory()
        {
            if (!GetPerformanceInfo(out var info, Marshal.SizeOf<PsApiPerformanceInformation>()))
                return (0, 0);

            long pageSize = info.PageSize.ToInt64();
            long available = info.PhysicalAvailable.ToInt64() * pageSize;
            long total = info.PhysicalTotal.ToInt64() * pageSize;
            return (available, total);
        }
    }
}
