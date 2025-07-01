using System;
using System.Threading.Tasks;
using Avalonia;
using Serilog;

namespace MapTileDownloader.UI.Desktop;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
         .MinimumLevel.Debug()
         .WriteTo.File("logs/logs.txt", rollingInterval: RollingInterval.Day)
         .CreateLogger();
        Log.Information("程序启动");
#if !DEBUG
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        try
        {
#endif
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
#if !DEBUG
         }
        catch (Exception ex)
        {
            Log.Fatal(ex, "未捕获的主线程错误");
        }
        finally
        {
            Log.CloseAndFlush();
        }
#endif
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Log.Fatal("未捕获的AppDomain异常", e.ExceptionObject as Exception);
    }

    private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        Log.Fatal("未捕获的TaskScheduler异常", e.Exception);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
