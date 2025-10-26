using System;
using System.Linq;
using System.Runtime.Versioning;
using System.Windows;
using AIContextPacker.Models;
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


