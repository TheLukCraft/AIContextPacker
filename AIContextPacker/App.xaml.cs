using System;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Windows;
using AIContextPacker.Models;
using AIContextPacker.Services;
using AIContextPacker.Services.Interfaces;
using AIContextPacker.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;

namespace AIContextPacker;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Configure Serilog
        ConfigureLogging();

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private void ConfigureLogging()
    {
        var logsFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AIContextPacker", "Logs");

        Directory.CreateDirectory(logsFolder);

        var logFile = Path.Combine(logsFolder, "app-.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                logFile,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Log.Information("Application starting up");
    }

    private void ConfigureServices(ServiceCollection services)
    {
        // Logging
        services.AddLogging(builder =>
        {
            builder.AddSerilog(dispose: true);
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Services
        services.AddSingleton<IFileSystemService, FileSystemService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IClipboardService, ClipboardService>();
        services.AddSingleton<IUpdateService, UpdateService>();
        services.AddSingleton<IProjectService, ProjectService>();
        services.AddSingleton<IFileSelectionService, FileSelectionService>();
        services.AddSingleton<IPinService, PinService>();
        services.AddSingleton<IFilterCategoryService, FilterCategoryService>();

        // ViewModels
        services.AddSingleton<MainViewModel>();

        // Windows
        services.AddTransient<MainWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("Application shutting down");
        Log.CloseAndFlush();
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
    
    public static void ApplyTheme(Models.ThemeMode theme)
    {
        var app = Current;
        if (app == null) return;

        string themeSource = theme switch
        {
            Models.ThemeMode.Light => "Resources/Themes/LightTheme.xaml",
            Models.ThemeMode.Dark => "Resources/Themes/DarkTheme.xaml",
            Models.ThemeMode.System => GetSystemTheme(),
            _ => "Resources/Themes/LightTheme.xaml"
        };

        var newTheme = new ResourceDictionary
        {
            Source = new System.Uri(themeSource, System.UriKind.Relative)
        };

        // Remove old theme dictionaries and add new one
        var existingTheme = app.Resources.MergedDictionaries
            .FirstOrDefault(d => d.Source?.OriginalString?.Contains("Themes/") == true);
        
        if (existingTheme != null)
        {
            app.Resources.MergedDictionaries.Remove(existingTheme);
        }
        
        app.Resources.MergedDictionaries.Insert(0, newTheme);
    }
    
    [SupportedOSPlatform("windows")]
    private static string GetSystemTheme()
    {
        // Check Windows theme
        try
        {
            var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            
            if (value is int intValue)
            {
                return intValue == 1 
                    ? "Resources/Themes/LightTheme.xaml" 
                    : "Resources/Themes/DarkTheme.xaml";
            }
        }
        catch
        {
            // Fallback to light theme if unable to determine
        }
        
        return "Resources/Themes/LightTheme.xaml";
    }
}


