using System;
using System.IO;
using System.Linq;

namespace RamCleaner
{
    public class DiskCleanerService
    {
        public enum CleanerType
        {
            SystemTemp,
            ChromeCache,
            ChromeCookies,
            EdgeCache,
            EdgeCookies,
            FirefoxCache,
            FirefoxCookies
        }

        public class CleanResult
        {
            public long BytesCleaned { get; set; }
            public int FilesCleaned { get; set; }
            public string Message { get; set; } = "";
        }

        private string GetChromePath() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Google\Chrome\User Data\Default");
        private string GetEdgePath() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Edge\User Data\Default");
        private string GetFirefoxPath() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Mozilla\Firefox\Profiles");

        public long GetSize(CleanerType type)
        {
            try
            {
                string path = GetPathForType(type);
                if (string.IsNullOrEmpty(path)) return 0;

                // specific handling for Cookies (single file usually)
                if (type == CleanerType.ChromeCookies || type == CleanerType.EdgeCookies)
                {
                    string cookiePath = Path.Combine(path, "Network", "Cookies");
                    if (!File.Exists(cookiePath)) 
                         cookiePath = Path.Combine(path, "Cookies"); // Older versions

                    return File.Exists(cookiePath) ? new FileInfo(cookiePath).Length : 0;
                }

                // Handling for directories (Cache, Temp)
                if (Directory.Exists(path))
                {
                    return Directory.GetFiles(path, "*", SearchOption.AllDirectories).Sum(t => new FileInfo(t).Length);
                }
            }
            catch { }
            return 0;
        }

        public CleanResult Clean(CleanerType type)
        {
            var result = new CleanResult();
            try
            {
                string path = GetPathForType(type);
                if (string.IsNullOrEmpty(path)) return result;

                // Cookies (File)
                if (type == CleanerType.ChromeCookies || type == CleanerType.EdgeCookies)
                {
                    string cookiePath = Path.Combine(path, "Network", "Cookies");
                    if (!File.Exists(cookiePath)) 
                         cookiePath = Path.Combine(path, "Cookies");

                    if (File.Exists(cookiePath))
                    {
                        result.BytesCleaned = new FileInfo(cookiePath).Length;
                        File.Delete(cookiePath);
                        result.FilesCleaned = 1;
                    }
                }
                // Directories (Temp, Cache)
                else if (Directory.Exists(path))
                {
                    var info = new DirectoryInfo(path);
                    foreach (var file in info.GetFiles("*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            long size = file.Length;
                            file.Delete();
                            result.BytesCleaned += size;
                            result.FilesCleaned++;
                        }
                        catch { } // Skip locked files
                    }
                }
            }
            catch (Exception ex) 
            {
                result.Message = ex.Message;
            }
            return result;
        }

        private string GetPathForType(CleanerType type)
        {
            switch (type)
            {
                case CleanerType.SystemTemp: return Path.GetTempPath();
                case CleanerType.ChromeCache: return Path.Combine(GetChromePath(), "Cache");
                case CleanerType.ChromeCookies: return GetChromePath(); // Logic handles subdir
                case CleanerType.EdgeCache: return Path.Combine(GetEdgePath(), "Cache");
                case CleanerType.EdgeCookies: return GetEdgePath(); // Logic handles subdir
                default: return "";
            }
        }
    }
}
