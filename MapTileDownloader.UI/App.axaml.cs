using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.Messaging;
using FzLib.Avalonia.Dialogs;
using FzLib.Avalonia.Services;
using MapTileDownloader.Services;
using MapTileDownloader.UI.Mapping;
using MapTileDownloader.UI.ViewModels;
using MapTileDownloader.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace MapTileDownloader.UI;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; }

    private void InitializeHost()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Services.AddDialogService();

        builder.Services.AddStorageProviderService();
        builder.Services.AddClipboardService();

        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<DownloadViewModel>();
        builder.Services.AddSingleton<LocalToolsViewModel>();
        builder.Services.AddSingleton<MapAreaSelectorViewModel>();
        builder.Services.AddSingleton<MbtilesPickerViewModel>();

        builder.Services.AddTransient<MainWindow>();
        builder.Services.AddSingleton<MainView>();
        builder.Services.AddTransient<LocalToolsPanel>();
        builder.Services.AddTransient<DownloadPanel>();
        builder.Services.AddTransient<MapAreaSelector>();
        builder.Services.AddTransient<MbtilesPicker>();
        builder.Services.AddSingleton<MapView>();

        builder.Services.AddSingleton<IMapService, MapView>(s => s.GetRequiredService<MapView>());
        builder.Services.AddSingleton<IMainViewControl, MainViewControl>();


        var host = builder.Build();
        Services = host.Services;
        host.Start();
    }

    public override void Initialize()
    {
        InitializeHost();
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = Services.GetRequiredService<MainWindow>();
            desktop.Exit += (s, e) => { Configs.Instance.Save(); };
        }
        else
        {
            throw new PlatformNotSupportedException();
        }

        base.OnFrameworkInitializationCompleted();
    }
}