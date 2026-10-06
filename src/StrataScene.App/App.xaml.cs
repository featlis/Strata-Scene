using System.Windows;
using System.Windows.Threading;
using StrataScene.App.Tray;
using StrataScene.Core.Logging;

namespace StrataScene.App;

public partial class App : Application
{
    private const string MutexName = "StrataScene.SingleInstance";
    private Mutex? _singleInstanceMutex;
    private FileLog? _log;
    private TrayController? _trayController;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _log = new FileLog();
        _log.Info("Strata Scene 0.1.0 starting up...");

        SetupExceptionHandling();

        _singleInstanceMutex = new Mutex(true, MutexName, out var isOnlyInstance);
        if (!isOnlyInstance)
        {
            _log.Warn("Another instance is already running. Exiting.");
            Shutdown();
            return;
        }

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _trayController = new TrayController(_log, ExitApplication);
        _log.Info("Tray icon initialized successfully.");
    }

    private void SetupExceptionHandling()
    {
        DispatcherUnhandledException += (s, e) =>
        {
            _log?.Error("Unhandled dispatcher exception", e.Exception);
            e.Handled = true; // Keep running per specification
        };

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            _log?.Error("AppDomain unhandled exception", e.ExceptionObject as Exception);
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            _log?.Error("Unobserved task exception", e.Exception);
            e.SetObserved();
        };
    }

    private void ExitApplication()
    {
        _log?.Info("Exiting Strata Scene.");
        _trayController?.Dispose();
        _singleInstanceMutex?.ReleaseMutex();
        _singleInstanceMutex?.Dispose();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayController?.Dispose();
        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }
}
