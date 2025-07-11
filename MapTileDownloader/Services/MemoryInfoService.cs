using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MapTileDownloader.Services;

public sealed class MemoryInfoService : IDisposable
{
    private const int _SC_PAGESIZE = 30;

    private const int _SC_PHYS_PAGES = 84;

    private static readonly Lazy<MemoryInfoService> instance = new Lazy<MemoryInfoService>(() => new MemoryInfoService());

    private MemoryInfoService()
    {
        Refresh();
    }

    public static MemoryInfoService Instance { get { return instance.Value; } }

    public ulong TotalPhysicalMemory { get; private set; }

    public void Dispose()
    {
    }

    public void Refresh()
    {
        TotalPhysicalMemory = GetTotalPhysicalMemory();
    }
    private static ulong GetTotalPhysicalMemory()
    {
        if (OperatingSystem.IsWindows())
        {
            return GetWindowsTotalMemory();
        }
        else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            return GetUnixTotalMemory();
        }
        throw new PlatformNotSupportedException("不支持的操作系统");
    }

    private static ulong GetUnixTotalMemory()
    {
        try
        {
            long pages = sysconf(_SC_PHYS_PAGES);
            long pageSize = sysconf(_SC_PAGESIZE);
            if (pages > 0 && pageSize > 0)
            {
                return (ulong)(pages * pageSize);
            }

            if (OperatingSystem.IsLinux())
            {
                string memInfo = System.IO.File.ReadAllText("/proc/meminfo");
                string totalMemLine = memInfo.Split('\n').FirstOrDefault(line => line.StartsWith("MemTotal:"));

                if (totalMemLine != null)
                {
                    string kbValue = totalMemLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[1];
                    return ulong.Parse(kbValue) * 1024;
                }
            }
        }
        catch
        {
        }

        return 0;
    }

    private static ulong GetWindowsTotalMemory()
    {
        var memStatus = new MEMORYSTATUSEX();
        memStatus.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));

        if (GlobalMemoryStatusEx(ref memStatus))
        {
            return memStatus.ullTotalPhys;
        }

        return 0;
    }
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [DllImport("libc")]
    private static extern long sysconf(int name);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }
}