using System;
using System.Windows;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace RamCleaner
{
    public partial class MainWindow : Window
    {
        private readonly Forms.NotifyIcon _notifyIcon;

        public MainWindow()
        {
            InitializeComponent();
            
            var vm = new MainViewModel();
            DataContext = vm;

            // Initialize System Tray Icon (Required for Balloon Notifications)
            _notifyIcon = new Forms.NotifyIcon
            {
                // Use a standard system icon so we don't need an external asset
                Icon = Drawing.SystemIcons.Information,
                Visible = true,
                Text = "Safe RAM Cleaner"
            };

            // Subscribe to ViewModel notification requests
            vm.RequestNotification += (title, message) =>
            {
                _notifyIcon.ShowBalloonTip(3000, title, message, Forms.ToolTipIcon.Info);
            };
        }

        protected override void OnClosed(EventArgs e)
        {
            _notifyIcon.Dispose();
            base.OnClosed(e);
        }
    }
}
