using System.Runtime.InteropServices;

namespace PcMeter.Services;

/// <summary>
/// Windows PsApi GetPerformanceInfo C# Wrapper by Antonio Bakula
/// </summary>
/// <remarks>
/// http://www.antoniob.com/windows-psapi-getperformanceinfo-csharp-wrapper.html
/// Also see http://stackoverflow.com/questions/10027341/c-sharp-get-used-memory-in
/// </remarks>
internal static class PsApiWrapper
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
