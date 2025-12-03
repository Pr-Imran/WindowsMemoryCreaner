using System.Configuration;
using System.Data;
using System.Windows;
using System.Linq;

namespace RamCleaner;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    public static bool StartMinimized { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (e.Args.Contains("-minimized"))
        {
            StartMinimized = true;
        }
    }
}