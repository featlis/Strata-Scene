using System.Windows;
using StrataScene.Core.Logging;

namespace StrataScene.App;

public partial class App : Application
{
    private const string MutexName = "StrataScene.SingleInstance";
    private const string WakeUpEventName = "StrataScene.WakeUp";

    private Mutex? _singleInstanceMutex;
    private EventWaitHandle? _wakeUpEvent;
    private RegisteredWaitHandle? _registeredWaitHandle;
    private FileLog? _log;
    private AppController? _controller;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _log = new FileLog();
        _log.Info("Strata Scene 0.1.0 starting up...");

        SetupExceptionHandling();

        _singleInstanceMutex = new Mutex(true, MutexName, out var isOnlyInstance);
        if (!isOnlyInstance)
        {
            _log.Warn("Another instance is already running. Signaling wake up and exiting.");
            try
            {
                if (EventWaitHandle.TryOpenExisting(WakeUpEventName, out var existingWakeUpEvent))
                {
                    existingWakeUpEvent.Set();
                }
            }
            catch (Exception ex)
            {
                _log.Error("Failed to signal existing instance", ex);
            }

            Shutdown();
            return;
        }

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        SetupSingleInstanceWakeUp();

        _controller = new AppController(_log, ExitApplication);
        _log.Info("Strata Scene initialized successfully.");

        // Post-startup memory trim to reduce idle working set
        Task.Delay(3000).ContinueWith(_ => TrimMemory());
    }

    private static void TrimMemory()
    {
        try
        {
            GC.Collect(2, GCCollectionMode.Aggressive, true, true);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Aggressive, true, true);

            var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
            currentProcess.MinWorkingSet = currentProcess.MinWorkingSet;
        }
        catch
        {
            // Ignore
        }
    }

    private void SetupSingleInstanceWakeUp()
    {
        try
        {
            _wakeUpEvent = new EventWaitHandle(false, EventResetMode.AutoReset, WakeUpEventName);
            _registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(
                _wakeUpEvent,
                (_, _) =>
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        _log?.Info("Wake up signal received from second instance.");
                        _controller?.OpenLauncher();
                    });
                },
                null,
                Timeout.Infinite,
                false);
        }
        catch (Exception ex)
        {
            _log?.Warn("Failed to setup single instance wake up event", ex);
        }
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

        _registeredWaitHandle?.Unregister(null);
        _wakeUpEvent?.Dispose();

        _controller?.Dispose();

        if (_singleInstanceMutex != null)
        {
            try
            {
                _singleInstanceMutex.ReleaseMutex();
            }
            catch
            {
                // Ignored if not owned
            }
            _singleInstanceMutex.Dispose();
        }

        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        ExitApplication();
        base.OnExit(e);
    }
}
