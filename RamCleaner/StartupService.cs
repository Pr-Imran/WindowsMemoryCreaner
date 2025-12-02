using System;
using System.Diagnostics;
using System.IO;

namespace RamCleaner
{
    public static class StartupService
    {
        private const string TaskName = "SafeRamCleanerAutoRun";

        public static bool IsStartupEnabled()
        {
            try
            {
                var psi = new ProcessStartInfo("schtasks", $"/Query /TN \"{TaskName}\"")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                
                using var proc = Process.Start(psi);
                proc?.WaitForExit();
                return proc?.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        public static void SetStartup(bool enable)
        {
            string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
            if (string.IsNullOrEmpty(exePath) || !exePath.EndsWith(".exe")) return;

            if (enable)
            {
                // Create a scheduled task that runs with highest privileges (Admin) at logon
                // We need to wrap the path in quotes because it might contain spaces.
                // The arguments for /TR need outer quotes if the inner command has quotes.
                // schtasks /TR "'C:\Path With Spaces\App.exe'"
                string command = $"/Create /TN \"{TaskName}\" /TR \"'{exePath}'\" /SC ONLOGON /RL HIGHEST /F";
                RunSchTasks(command);
            }
            else
            {
                // Delete the task
                RunSchTasks($"/Delete /TN \"{TaskName}\" /F");
            }
        }

        private static void RunSchTasks(string arguments)
        {
            try
            {
                var psi = new ProcessStartInfo("schtasks", arguments)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(psi)?.WaitForExit();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to change startup settings: {ex.Message}");
            }
        }
    }
}
