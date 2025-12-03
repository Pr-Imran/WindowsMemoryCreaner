using System;
using System.ComponentModel;
using System.Windows;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace RamCleaner
{
    public partial class MainWindow : Window
    {
        private readonly Forms.NotifyIcon _notifyIcon;
        private MainViewModel _vm;

        public MainWindow()
        {
            InitializeComponent();
            
            _vm = new MainViewModel();
            DataContext = _vm;
            _vm.PropertyChanged += Vm_PropertyChanged;

            // Initialize System Tray Icon
            _notifyIcon = new Forms.NotifyIcon
            {
                Icon = Drawing.SystemIcons.Information,
                Visible = true,
                Text = "Safe RAM Cleaner"
            };

            _notifyIcon.DoubleClick += (s, args) => RestoreWindow();

            // Context Menu
            var contextMenu = new Forms.ContextMenuStrip();
            contextMenu.Items.Add("Open / Restore", null, (s, args) => RestoreWindow());
            contextMenu.Items.Add("Clean Now", null, (s, args) => _vm.CleanCommand.Execute(null));
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("Exit", null, (s, args) => {
                _notifyIcon.Visible = false;
                System.Windows.Application.Current.Shutdown();
            });
            _notifyIcon.ContextMenuStrip = contextMenu;

            // Subscribe to ViewModel notification requests
            _vm.RequestNotification += (title, message) =>
            {
                _notifyIcon.ShowBalloonTip(3000, title, message, Forms.ToolTipIcon.Info);
            };
            
            // Handle Start Minimized
            if (App.StartMinimized)
            {
                WindowState = WindowState.Minimized;
                Hide();
            }
        }

        private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.RamLoadPercent))
            {
                _notifyIcon.Text = $"RAM Usage: {_vm.RamLoadPercent}%";
            }
        }

        private void RestoreWindow()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
            }
            base.OnStateChanged(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            _notifyIcon.Dispose();
            _vm.PropertyChanged -= Vm_PropertyChanged;
            base.OnClosed(e);
        }
    }
}
