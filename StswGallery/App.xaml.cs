global using StswExpress;
global using StswExpress.Commons;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace StswGallery;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : StswApp
{
    public static CancellationTokenSource CancellationTokenSource { get; } = new();
    public static CancellationToken CancellationToken => CancellationTokenSource.Token;

    private async  void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        await StswDispatcher.RunWhenUiIsReadyAsync(() => StswMessageDialog.Show(e.Exception, "Unhandled exception"));
        e.Handled = true;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (!CancellationTokenSource.IsCancellationRequested)
            CancellationTokenSource.Cancel();
        CancellationTokenSource.Dispose();

        base.OnExit(e);
    }
}
