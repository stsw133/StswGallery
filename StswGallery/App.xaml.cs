global using StswExpress;
global using StswExpress.Commons;
using System.Windows.Threading;

namespace StswGallery;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : StswApp
{
    private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        StswLog.Write(StswInfoType.Error, e.Exception.ToString());
    }
}
