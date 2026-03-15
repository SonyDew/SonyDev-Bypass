using System.Windows;
using Velopack;

namespace SonyDevBypass.App;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        VelopackApp.Build()
            .SetArgs(e.Args)
            .SetAutoApplyOnStartup(false)
            .Run();

        base.OnStartup(e);
    }
}
