using System.Windows;
using AIContextPacker.Services;
using AIContextPacker.Services.Interfaces;
using AIContextPacker.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace AIContextPacker;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private void ConfigureServices(ServiceCollection services)
    {
        // Services
        services.AddSingleton<IFileSystemService, FileSystemService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IClipboardService, ClipboardService>();

        // ViewModels
        services.AddSingleton<MainViewModel>();

        // Windows
        services.AddTransient<MainWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        var mainViewModel = _serviceProvider?.GetService<MainViewModel>();
        mainViewModel?.SaveStateAsync().Wait();

        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}

