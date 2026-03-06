global using StswExpress;
global using StswExpress.Commons;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace StswGallery;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : StswApp
{
    public static IConfiguration Configuration { get; private set; } = null!;
    public static IServiceProvider? ServiceProvider { get; private set; }

    public static CancellationTokenSource CancellationTokenSource { get; } = new();
    public static CancellationToken CancellationToken => CancellationTokenSource.Token;

    /// OnStartup
    protected override void OnStartup(StartupEventArgs e)
    {
        //try
        //{
            base.OnStartup(e);
        //}
        //catch (ConfigurationErrorsException ex)
        //{
        //    var cfgEx = ex.InnerException as ConfigurationErrorsException ?? ex;
        //    var cfgPath = cfgEx.Filename;
        //
        //    if (!string.IsNullOrWhiteSpace(cfgPath) && File.Exists(cfgPath))
        //    {
        //        File.Delete(cfgPath);
        //        base.OnStartup(e);
        //    }
        //    else
        //    {
        //        throw;
        //    }
        //}

        /// Configuration
        var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
        Configuration = configurationBuilder.Build();

        // Ensure AppSettingsService uses the latest configuration (no exception if file missing)
        AppSettingsService.Reload();

        /// DependencyInjection
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        AutoConfigureServices(serviceCollection);
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        ServiceProvider = serviceCollection.BuildServiceProvider();

        MainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        MainWindow.Show();
    }

    /// ConfigureServices
    private void ConfigureServices(ServiceCollection serviceCollection)
    {
        /// Window
        serviceCollection.AddSingleton(provider => new MainWindow()
        {
            DataContext = provider.GetRequiredService<MainContext>()
        });
    }

    /// AutoConfigureServices
    protected virtual void AutoConfigureServices(ServiceCollection services)
    {
        if (!services.Any(x => x.ServiceType == typeof(AppSettings)))
            services.AddSingleton(AppSettingsService.Current);

        if (!services.Any(x => x.ServiceType == typeof(IConfiguration)))
            services.Configure<AppSettings>(Configuration.GetSection(string.Empty));

        if (!services.Any(x => x.ServiceType == typeof(Func<Type, BaseContext>)))
            services.AddSingleton<Func<Type, BaseContext>>(provider => contextType => (BaseContext)provider.GetRequiredService(contextType));

        if (!services.Any(x => x.ServiceType == typeof(Func<Type, string?, StswObservableDialog>)))
            services.AddSingleton<Func<Type, string?, StswObservableDialog>>(provider => (contextType, identifier) =>
            {
                var dialog = (StswObservableDialog)provider.GetRequiredService(contextType);
                dialog.DialogIdentifier = identifier;
                return dialog;
            });

        var alreadyRegistered = new HashSet<Type>(services.Select(x => x.ServiceType));
        var assembly = Assembly.GetEntryAssembly();
        if (assembly is null)
            return;

        var types = assembly.GetTypes();

        foreach (var type in types)
        {
            if (type.IsAbstract || type.IsInterface || type.IsGenericType || type.IsValueType)
                continue;

            if (type.Name.EndsWith("Context", StringComparison.OrdinalIgnoreCase))
            {
                if (!alreadyRegistered.Contains(type))
                {
                    services.AddSingleton(type);
                    alreadyRegistered.Add(type);
                }
                continue;
            }

            var iface = type.GetInterface($"I{type.Name}");
            if (iface != null && !alreadyRegistered.Contains(iface) && !alreadyRegistered.Contains(type))
            {
                services.AddSingleton(iface, type);
                alreadyRegistered.Add(iface);
            }
        }
    }

    /// DispatcherUnhandledException
    private async void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        await StswDispatcher.RunWhenUiIsReadyAsync(() => StswMessageDialog.Show(e.Exception, "Unhandled exception"));
        e.Handled = true;
    }

    /// OnExit
    protected override void OnExit(ExitEventArgs e)
    {
        if (!CancellationTokenSource.IsCancellationRequested)
            CancellationTokenSource.Cancel();
        CancellationTokenSource.Dispose();

        base.OnExit(e);
    }
}
