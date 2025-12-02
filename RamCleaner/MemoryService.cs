using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace RamCleaner
{
    public struct MemoryStats
    {
        public double TotalPhysGb;
        public double UsedPhysGb;
        public double FreePhysGb;
        public int LoadPercent;
        
        public double CachedGb;
        public double CommittedGb;
        public double CommitLimitGb;
    }

    public class MemoryService
    {
        // Safety: List of critical system processes to NEVER touch.
        private static readonly HashSet<string> CriticalProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "idle", "system", "registry", "smss", "csrss", "wininit", "services", "lsass", 
            "svchost", "fontdrvhost", "memory compression", "spoolsv", "winlogon", "dwm", 
            "audiodg", "explorer", "taskmgr", "searchui", "shellexperiencehost", "lockapp",
            "msmpeng", "nissrv" // Defender
        };

        public bool IsAdmin { get; private set; }

        public MemoryService()
        {
            IsAdmin = IsRunningAsAdmin();
        }

        private bool IsRunningAsAdmin()
        {
            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch { return false; }
        }

        public MemoryStats GetMemoryStatus()
        {
            var stats = new MemoryStats();

            // 1. Basic Global Stats
            var memStatus = new NativeMethods.MEMORYSTATUSEX();
            if (NativeMethods.GlobalMemoryStatusEx(memStatus))
            {
                stats.TotalPhysGb = memStatus.ullTotalPhys / (1024.0 * 1024.0 * 1024.0);
                stats.FreePhysGb = memStatus.ullAvailPhys / (1024.0 * 1024.0 * 1024.0);
                stats.UsedPhysGb = stats.TotalPhysGb - stats.FreePhysGb;
                stats.LoadPercent = (int)memStatus.dwMemoryLoad;
            }

            // 2. Advanced Performance Stats
            NativeMethods.PERFORMANCE_INFORMATION perfInfo;
            if (NativeMethods.GetPerformanceInfo(out perfInfo, Marshal.SizeOf(typeof(NativeMethods.PERFORMANCE_INFORMATION))))
            {
                long pageSize = (long)perfInfo.PageSize;
                double cachedBytes = (long)perfInfo.SystemCache * pageSize;
                stats.CachedGb = cachedBytes / (1024.0 * 1024.0 * 1024.0);

                double committedBytes = (long)perfInfo.CommitTotal * pageSize;
                stats.CommittedGb = committedBytes / (1024.0 * 1024.0 * 1024.0);
                
                double commitLimitBytes = (long)perfInfo.CommitLimit * pageSize;
                stats.CommitLimitGb = commitLimitBytes / (1024.0 * 1024.0 * 1024.0);
            }

            return stats;
        }

        public (long freedBytes, int count) CleanMemory()
        {
            int processCount = 0;

            // 1. Clean current process first
            Process current = Process.GetCurrentProcess();
            try { NativeMethods.EmptyWorkingSet(current.Handle); } catch { }

            // 2. Clean System File Cache (Requires Admin)
            // This "flushes" the standby list effectively by momentarily capping the cache size.
            if (IsAdmin)
            {
                try
                {
                    // Set to minimal size (flush)
                    // We use a magic number that is valid but small.
                    // Note: SetSystemFileCacheSize(IntPtr.Zero, ...) doesn't always work as expected on modern Windows.
                    // We try to flush by setting limits then unsetting them.
                    // Flags: 0 to flush/reset if supported, or specific flags.
                    
                    // Minimal flush attempt: Set to 1MB min/max, then reset.
                    IntPtr size = new IntPtr(1024 * 1024); 
                    NativeMethods.SetSystemFileCacheSize(size, size, 0);
                    
                    // Reset to system managed immediately
                    // Passing -1 or MAX_SIZE_T typically resets it, 
                    // but documented way is often "Active flushing" via the above, then relaxing.
                    NativeMethods.SetSystemFileCacheSize(new IntPtr(-1), new IntPtr(-1), 0);
                }
                catch 
                { 
                    // Ignore cache errors 
                }
            }

            // 3. Clean other safe processes (Working Set Trim)
            Process[] processes = Process.GetProcesses();
            foreach (var p in processes)
            {
                try
                {
                    if (p.Id == 0 || p.Id == 4) continue; 
                    if (p.HasExited) continue;
                    if (CriticalProcesses.Contains(p.ProcessName)) continue;

                    bool success = NativeMethods.EmptyWorkingSet(p.Handle) != 0;
                    if (success) processCount++;
                }
                catch { /* Safe to ignore */ }
                finally
                {
                    p.Dispose();
                }
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();

            return (0, processCount);
        }
    }
}
