using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using System.Windows;

namespace RamCleaner
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly MemoryService _memoryService;
        private readonly DispatcherTimer _monitorTimer;
        
        private DateTime _lastThresholdCleanTime = DateTime.MinValue;
        private DateTime _lastTimeCleanTime = DateTime.Now; 
        private const int ThresholdCooldownSeconds = 60;

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

        // Commands
        public RelayCommand CleanCommand { get; }
        public RelayCommand ClearLogCommand { get; }

        public event Action<string, string>? RequestNotification;

        public MainViewModel()
        {
            _memoryService = new MemoryService();
            CleanCommand = new RelayCommand(o => PerformClean(true));
            ClearLogCommand = new RelayCommand(o => Logs.Clear());
            
            // Check initial startup state
            _isStartupEnabled = StartupService.IsStartupEnabled();
            // Notify property changed manually for this without triggering the setter logic
            // (Actually, for simple MVVM, just setting the field is fine, UI will bind to it).

            _monitorTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.0) };
            _monitorTimer.Tick += MonitorTimer_Tick;
            _monitorTimer.Start();

            UpdateStats();
            AddLog("App started. Ready.");
        }

        private void MonitorTimer_Tick(object? sender, EventArgs e)
        {
            UpdateStats();
            CheckAutoClean();
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
            LastCleanStatus = "Cleaning...";
            var before = _memoryService.GetMemoryStatus();
            var result = _memoryService.CleanMemory();
            var after = _memoryService.GetMemoryStatus();

            double freedMb = (after.FreePhysGb - before.FreePhysGb) * 1024.0;
            if (freedMb < 0) freedMb = 0; 

            string msg = $"Freed: {freedMb:F0} MB (Processed {result.count} apps)";
            LastCleanStatus = msg;
            AddLog(msg);

            if (freedMb > 0 || isManual)
            {
                RequestNotification?.Invoke("Memory Cleaned", $"Successfully freed {freedMb:F0} MB of RAM.");
            }

            UpdateStats();
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
