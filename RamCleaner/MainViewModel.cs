using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows;
using RamCleaner.Resources;

namespace RamCleaner
{
    public class LocalizedStrings : INotifyPropertyChanged
    {
        private static readonly LocalizedStrings _instance = new LocalizedStrings();
        public static LocalizedStrings Instance => _instance;

        public string AppTitle => Strings.AppTitle;
        public string SystemMemoryStatus => Strings.SystemMemoryStatus;
        public string TotalPhysicalRAM => Strings.TotalPhysicalRAM;
        public string UsedRAM => Strings.UsedRAM;
        public string AvailableRAM => Strings.AvailableRAM;
        public string CachedStandby => Strings.CachedStandby;
        public string CommittedLimit => Strings.CommittedLimit;
        public string UsageHistory => Strings.UsageHistory;
        public string CleanMemoryNow => Strings.CleanMemoryNow;
        public string AutoClean => Strings.AutoClean;
        public string Strategies => Strings.Strategies;
        public string RunOnStartup => Strings.RunOnStartup;
        public string UsageGreater => Strings.UsageGreater;
        public string Every => Strings.Every;
        public string Whitelist => Strings.Whitelist;
        public string IgnoredProcesses => Strings.IgnoredProcesses;
        public string RunningProcesses => Strings.RunningProcesses;
        public string Add => Strings.Add;
        public string Options => Strings.Options;
        public string Notifications => Strings.Notifications;
        public string ShowAfterManualClean => Strings.ShowAfterManualClean;
        public string ShowAfterAutoClean => Strings.ShowAfterAutoClean;
        public string EventLog => Strings.EventLog;
        public string Clear => Strings.Clear;
        public string Time => Strings.Time;
        public string Message => Strings.Message;
        public string Name => Strings.Name;
        public string Action => Strings.Action;
        public string ReadyToClean => Strings.ReadyToClean;
        public string AppStarted => Strings.AppStarted;
        public string Cleaning => Strings.Cleaning;
        public string FreedMessage => Strings.FreedMessage;
        public string MemoryCleaned => Strings.MemoryCleaned;
        public string SuccessfullyFreed => Strings.SuccessfullyFreed;

        // Disk Cleaner (Hardcoded for now to avoid RESX regeneration issues)
        public string DiskCleaner => "Disk Cleaner";
        public string Scan => "Scan";
        public string CleanDisk => "Clean Junk";

        public void SetCulture(CultureInfo culture)
        {
            Strings.Culture = culture;
            OnPropertyChanged(string.Empty); // Update all properties
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}

namespace RamCleaner
{
    public class LanguageOption
    {
        public LanguageOption(string displayName, string code)
        {
            DisplayName = displayName;
            Code = code;
        }

        public string DisplayName { get; set; }
        public string Code { get; set; }
    }

    public class DiskItem : INotifyPropertyChanged
    {
        private string _sizeDisplay = "Pending scan...";
        private bool _isSelected;

        public string Name { get; set; } = "";
        public DiskCleanerService.CleanerType Type { get; set; }
        
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public string SizeDisplay
        {
            get => _sizeDisplay;
            set { _sizeDisplay = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly MemoryService _memoryService = null!;
        private readonly DiskCleanerService _diskCleaner = null!;
        private readonly DispatcherTimer _monitorTimer;
        
        private DateTime _lastThresholdCleanTime = DateTime.MinValue;
        private DateTime _lastTimeCleanTime = DateTime.Now; 
        private const int ThresholdCooldownSeconds = 60;

        // --- Language ---
        public ObservableCollection<LanguageOption> AvailableLanguages { get; } = new ObservableCollection<LanguageOption>
        {
            new LanguageOption("English", "en"),
            new LanguageOption("Português (Brasil)", "pt-BR"),
            new LanguageOption("বাংলা (Bangla)", "bn-BD")
        };

        private LanguageOption _selectedLanguage = null!;
        public LanguageOption SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (_selectedLanguage != value && value != null)
                {
                    _selectedLanguage = value;
                    OnPropertyChanged();
                    LocalizedStrings.Instance.SetCulture(new CultureInfo(value.Code));
                    SaveLanguage(value.Code);
                }
            }
        }

        private void SaveLanguage(string code)
        {
            try { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "language.txt"), code); } catch { }
        }
        
        private void LoadLanguage()
        {
            try 
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "language.txt");
                if (File.Exists(path))
                {
                    string code = File.ReadAllText(path).Trim();
                    var lang = System.Linq.Enumerable.FirstOrDefault(AvailableLanguages, l => l.Code == code);
                    if (lang != null)
                    {
                        SelectedLanguage = lang; // This triggers SetCulture
                        return;
                    }
                }
            }
            catch { }
            
            // Default
            SelectedLanguage = AvailableLanguages[0];
        }

        // --- Properties ---

        private string _totalRam = "...";
        public string TotalRam { get => _totalRam; set { _totalRam = value; OnPropertyChanged(); } }

        private string _usedRamDisplay = "...";
        public string UsedRamDisplay { get => _usedRamDisplay; set { _usedRamDisplay = value; OnPropertyChanged(); } }

        private string _freeRamDisplay = "...";
        public string FreeRamDisplay { get => _freeRamDisplay; set { _freeRamDisplay = value; OnPropertyChanged(); } }

        private string _cachedRamDisplay = "...";
        public string CachedRamDisplay { get => _cachedRamDisplay; set { _cachedRamDisplay = value; OnPropertyChanged(); } }

        private string _committedRamDisplay = "...";
        public string CommittedRamDisplay { get => _committedRamDisplay; set { _committedRamDisplay = value; OnPropertyChanged(); } }

        private int _ramLoadPercent;
        public int RamLoadPercent { get => _ramLoadPercent; set { _ramLoadPercent = value; OnPropertyChanged(); } }

        // --- Startup Settings ---
        private bool _isStartupEnabled;
        public bool IsStartupEnabled
        {
            get => _isStartupEnabled;
            set
            {
                if (_isStartupEnabled != value)
                {
                    _isStartupEnabled = value;
                    OnPropertyChanged();
                    StartupService.SetStartup(value);
                    AddLog(value ? "Enabled 'Run on Startup' (Task Scheduler)." : "Disabled 'Run on Startup'.");
                }
            }
        }

        // --- Auto Clean: Threshold (Percent) ---
        private bool _isThresholdCleanEnabled;
        public bool IsThresholdCleanEnabled
        {
            get => _isThresholdCleanEnabled;
            set 
            { 
                _isThresholdCleanEnabled = value; 
                OnPropertyChanged(); 
                AddLog(value ? "Auto-Clean (Percent) ENABLED." : "Auto-Clean (Percent) DISABLED.");
            }
        }

        private int _autoCleanThreshold = 80;
        public int AutoCleanThreshold
        {
            get => _autoCleanThreshold;
            set { _autoCleanThreshold = value; OnPropertyChanged(); }
        }

        // --- Auto Clean: Interval (Time) ---
        private bool _isTimeCleanEnabled;
        public bool IsTimeCleanEnabled
        {
            get => _isTimeCleanEnabled;
            set 
            { 
                _isTimeCleanEnabled = value; 
                OnPropertyChanged();
                if(value) _lastTimeCleanTime = DateTime.Now; 
                AddLog(value ? "Auto-Clean (Timer) ENABLED." : "Auto-Clean (Timer) DISABLED.");
            }
        }

        private int _autoCleanIntervalMinutes = 30;
        public int AutoCleanIntervalMinutes
        {
            get => _autoCleanIntervalMinutes;
            set { _autoCleanIntervalMinutes = value; OnPropertyChanged(); }
        }

        // --- Status & Logs ---
        private string _lastCleanStatus = "Ready to clean";
        public string LastCleanStatus { get => _lastCleanStatus; set { _lastCleanStatus = value; OnPropertyChanged(); } }

        public ObservableCollection<LogEntry> Logs { get; } = new ObservableCollection<LogEntry>();

        // --- New Features ---
        
        // 1. RAM History Graph Data
        // Points X: 0..60 (Time), Y: 0..100 (Usage %)
        private PointCollection _historyPoints = new PointCollection();
        public PointCollection HistoryPoints { get => _historyPoints; set { _historyPoints = value; OnPropertyChanged(); } }

        // 2. Whitelist
        public ObservableCollection<string> Whitelist { get; } = new ObservableCollection<string>();
        public ObservableCollection<ProcessInfo> RunningProcesses { get; } = new ObservableCollection<ProcessInfo>();
        
        // 3. Disk Cleaner
        public ObservableCollection<DiskItem> DiskItems { get; } = new ObservableCollection<DiskItem>();
        public RelayCommand ScanDiskCommand { get; } = null!;
        public RelayCommand CleanDiskCommand { get; } = null!;

        private string _newWhitelistItem = "";
        public string NewWhitelistItem 
        { 
            get => _newWhitelistItem; 
            set { _newWhitelistItem = value; OnPropertyChanged(); } 
        }
        
        private ProcessInfo? _selectedRunningProcess;
        public ProcessInfo? SelectedRunningProcess
        {
            get => _selectedRunningProcess;
            set { _selectedRunningProcess = value; OnPropertyChanged(); }
        }

        // 3. Notification Settings
        private bool _showAutoCleanNotification = false;
        public bool ShowAutoCleanNotification
        {
            get => _showAutoCleanNotification;
            set { _showAutoCleanNotification = value; OnPropertyChanged(); }
        }

        private bool _showManualCleanNotification = true;
        public bool ShowManualCleanNotification
        {
            get => _showManualCleanNotification;
            set { _showManualCleanNotification = value; OnPropertyChanged(); }
        }

        // Commands
        public RelayCommand CleanCommand { get; } = null!;
        public RelayCommand ClearLogCommand { get; } = null!;
        public RelayCommand AddWhitelistCommand { get; } = null!;
        public RelayCommand RemoveWhitelistCommand { get; } = null!;
        public RelayCommand RefreshProcessesCommand { get; } = null!;
        public RelayCommand AddSelectedToWhitelistCommand { get; } = null!;

        public event Action<string, string>? RequestNotification;

        private readonly System.Collections.Generic.List<int> _rawHistory = new System.Collections.Generic.List<int>();

        public MainViewModel()
        {
            try 
            {
                _memoryService = new MemoryService();
                _diskCleaner = new DiskCleanerService();

                // Initialize Disk Items
                DiskItems.Add(new DiskItem { Name = "System Temp Files", Type = DiskCleanerService.CleanerType.SystemTemp, IsSelected = true });
                DiskItems.Add(new DiskItem { Name = "Chrome Cache", Type = DiskCleanerService.CleanerType.ChromeCache, IsSelected = true });
                DiskItems.Add(new DiskItem { Name = "Chrome Cookies", Type = DiskCleanerService.CleanerType.ChromeCookies, IsSelected = false });
                DiskItems.Add(new DiskItem { Name = "Edge Cache", Type = DiskCleanerService.CleanerType.EdgeCache, IsSelected = true });
                DiskItems.Add(new DiskItem { Name = "Edge Cookies", Type = DiskCleanerService.CleanerType.EdgeCookies, IsSelected = false });

                ScanDiskCommand = new RelayCommand(o => PerformDiskScan());
                CleanDiskCommand = new RelayCommand(o => PerformDiskClean());

                CleanCommand = new RelayCommand(o => PerformClean(true));
                ClearLogCommand = new RelayCommand(o => Logs.Clear());

                RefreshProcessesCommand = new RelayCommand(o => RefreshRunningProcesses());

                AddSelectedToWhitelistCommand = new RelayCommand(o => 
                {
                    if (o is string name)
                    {
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            AddLog("Attempted to add empty process name to whitelist.");
                            return;
                        }
                        _memoryService.AddToWhitelist(name);
                        RefreshWhitelist();
                        RefreshRunningProcesses();
                        AddLog($"Added '{name}' to whitelist via selection.");
                    }
                    else if (SelectedRunningProcess != null)
                    {
                         // This path is less likely to be hit with CommandParameter="{Binding Name}"
                         _memoryService.AddToWhitelist(SelectedRunningProcess.Name);
                         RefreshWhitelist();
                         RefreshRunningProcesses();
                         AddLog($"Added '{SelectedRunningProcess.Name}' to whitelist via selection (SelectedRunningProcess).");
                    }
                    else
                    {
                        AddLog("Attempted to add to whitelist, but no valid process was selected or passed.");
                    }
                });

                AddWhitelistCommand = new RelayCommand(o => 
                {
                    if (!string.IsNullOrWhiteSpace(NewWhitelistItem))
                    {
                        _memoryService.AddToWhitelist(NewWhitelistItem);
                        RefreshWhitelist();
                        RefreshRunningProcesses();
                        NewWhitelistItem = "";
                    }
                });

                RemoveWhitelistCommand = new RelayCommand(o => 
                {
                    if (o is string item)
                    {
                        _memoryService.RemoveFromWhitelist(item);
                        RefreshWhitelist();
                        RefreshRunningProcesses();
                    }
                });
                
                RefreshWhitelist();
                // Initial load of processes
                RefreshRunningProcesses();
                
                // Fill history with zeros
                for (int i = 0; i < 60; i++) _rawHistory.Add(0);

                // Check initial startup state
                _isStartupEnabled = StartupService.IsStartupEnabled();

                LoadLanguage();
                
                LastCleanStatus = LocalizedStrings.Instance.ReadyToClean;

                UpdateStats();
                AddLog(LocalizedStrings.Instance.AppStarted);
            }
            catch (Exception ex)
            {
                // Fallback if initialization fails
                AddLog($"Startup Error: {ex.Message}");
            }

            // Timer must start regardless to keep UI alive if possible
            _monitorTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.0) };
            _monitorTimer.Tick += MonitorTimer_Tick;
            _monitorTimer.Start();
        }

        private void MonitorTimer_Tick(object? sender, EventArgs e)
        {
            UpdateStats();
            CheckAutoClean();

            // Update History
            _rawHistory.Add(RamLoadPercent);
            if (_rawHistory.Count > 60) _rawHistory.RemoveAt(0);

            // Generate Points for Graph (60 points, scaled to 0-100 Y)
            // Y is inverted (0 is top) so 100% load = 0 Y, 0% load = 100 Y
            var points = new PointCollection();
            for (int i = 0; i < _rawHistory.Count; i++)
            {
                points.Add(new System.Windows.Point(i, 100 - _rawHistory[i]));
            }
            HistoryPoints = points;
        }

        private void RefreshRunningProcesses()
        {
            var list = _memoryService.GetActiveProcessNames();
            RunningProcesses.Clear();
            foreach (var p in list)
            {
                RunningProcesses.Add(p);
            }
        }

        private void RefreshWhitelist()
        {
            Whitelist.Clear();
            foreach (var item in _memoryService.GetWhitelist())
            {
                Whitelist.Add(item);
            }
        }

        private void UpdateStats()
        {
            var stats = _memoryService.GetMemoryStatus();
            
            TotalRam = $"{stats.TotalPhysGb:F1} GB";
            UsedRamDisplay = $"{stats.UsedPhysGb:F2} GB ({stats.LoadPercent}%)";
            FreeRamDisplay = $"{stats.FreePhysGb:F2} GB";
            CachedRamDisplay = $"{stats.CachedGb:F2} GB";
            CommittedRamDisplay = $"{stats.CommittedGb:F2} / {stats.CommitLimitGb:F1} GB";
            RamLoadPercent = stats.LoadPercent;
        }

        private void CheckAutoClean()
        {
            bool cleaned = false;
            if (IsThresholdCleanEnabled)
            {
                if (RamLoadPercent >= AutoCleanThreshold)
                {
                    if ((DateTime.Now - _lastThresholdCleanTime).TotalSeconds > ThresholdCooldownSeconds)
                    {
                        AddLog($"Trigger: Usage {RamLoadPercent}% >= {AutoCleanThreshold}%");
                        PerformClean(false);
                        _lastThresholdCleanTime = DateTime.Now;
                        cleaned = true;
                    }
                }
            }

            if (IsTimeCleanEnabled && !cleaned)
            {
                if ((DateTime.Now - _lastTimeCleanTime).TotalMinutes >= AutoCleanIntervalMinutes)
                {
                    AddLog($"Trigger: Timer ({AutoCleanIntervalMinutes} min elapsed)");
                    PerformClean(false);
                    _lastTimeCleanTime = DateTime.Now;
                }
            }
        }

        private void PerformClean(bool isManual)
        {
            LastCleanStatus = LocalizedStrings.Instance.Cleaning;
            var before = _memoryService.GetMemoryStatus();
            var result = _memoryService.CleanMemory();
            var after = _memoryService.GetMemoryStatus();

            double freedMb = (after.FreePhysGb - before.FreePhysGb) * 1024.0;
            if (freedMb < 0) freedMb = 0; 

            string msg = string.Format(LocalizedStrings.Instance.FreedMessage, freedMb, result.count);
            LastCleanStatus = msg;
            AddLog(msg);

            bool shouldNotify = isManual ? ShowManualCleanNotification : ShowAutoCleanNotification;

            if ((freedMb > 0 || isManual) && shouldNotify)
            {
                RequestNotification?.Invoke(LocalizedStrings.Instance.MemoryCleaned, string.Format(LocalizedStrings.Instance.SuccessfullyFreed, freedMb));
            }

            UpdateStats();
        }

        private async void PerformDiskScan()
        {
            AddLog("Scanning disk junk...");
            await System.Threading.Tasks.Task.Run(() => 
            {
                foreach (var item in DiskItems)
                {
                     long size = _diskCleaner.GetSize(item.Type);
                     string sizeStr = FormatBytes(size);
                     System.Windows.Application.Current.Dispatcher.Invoke(() => item.SizeDisplay = sizeStr);
                }
            });
            AddLog("Disk scan complete.");
        }

        private async void PerformDiskClean()
        {
             AddLog("Cleaning selected disk junk...");
             long totalFreed = 0;
             await System.Threading.Tasks.Task.Run(() => 
             {
                 foreach (var item in DiskItems)
                 {
                     if (item.IsSelected)
                     {
                         var result = _diskCleaner.Clean(item.Type);
                         totalFreed += result.BytesCleaned;
                         System.Windows.Application.Current.Dispatcher.Invoke(() => item.SizeDisplay = "Cleaned");
                     }
                 }
                 
                 // Re-scan
                 foreach (var item in DiskItems)
                {
                     long size = _diskCleaner.GetSize(item.Type);
                     string sizeStr = FormatBytes(size);
                     System.Windows.Application.Current.Dispatcher.Invoke(() => item.SizeDisplay = sizeStr);
                }
             });
             AddLog($"Disk cleaning complete. Freed {FormatBytes(totalFreed)}.");
        }

        private string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F2} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F2} MB";
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
        }

        private void AddLog(string message)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() => 
            {
                Logs.Insert(0, new LogEntry(message));
                if (Logs.Count > 100) Logs.RemoveAt(Logs.Count - 1);
            });
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
