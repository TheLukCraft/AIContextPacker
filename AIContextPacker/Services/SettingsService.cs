using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AIContextPacker.Helpers;
using AIContextPacker.Models;
using AIContextPacker.Services.Interfaces;
using Newtonsoft.Json;

namespace AIContextPacker.Services;

public class SettingsService : ISettingsService
{
    private readonly string _settingsFolder;
    private readonly string _settingsFile;
    private readonly string _sessionFile;
    private readonly string _recentProjectsFile;

    public SettingsService()
    {
        _settingsFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AIContextPacker"
        );

        _settingsFile = Path.Combine(_settingsFolder, "settings.json");
        _sessionFile = Path.Combine(_settingsFolder, "session.json");
        _recentProjectsFile = Path.Combine(_settingsFolder, "recent.json");

        EnsureSettingsFolderExists();
    }

    private void EnsureSettingsFolderExists()
    {
        if (!Directory.Exists(_settingsFolder))
        {
            Directory.CreateDirectory(_settingsFolder);
        }
    }

    public async Task<AppSettings> LoadSettingsAsync()
    {
        if (!File.Exists(_settingsFile))
        {
            var defaultSettings = new AppSettings
            {
                CustomIgnoreFilters = new List<IgnoreFilter>(), // Empty by default
                GlobalPrompts = DefaultPrompts.GetDefaultPrompts(),
                ActiveFilters = new Dictionary<string, bool>()
            };
            await SaveSettingsAsync(defaultSettings);
            return defaultSettings;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_settingsFile);
            var settings = JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
            
            // Migrate old IgnoreFilters to CustomIgnoreFilters if needed
            var settingsObj = JsonConvert.DeserializeObject<dynamic>(json);
            if (settingsObj?.IgnoreFilters != null && settings.CustomIgnoreFilters.Count == 0)
            {
                // Migration path - but don't include default filters in custom
                settings.CustomIgnoreFilters = new List<IgnoreFilter>();
            }
            
            // Ensure we have default prompts if none exist
            if (settings.GlobalPrompts.Count == 0)
            {
                settings.GlobalPrompts = DefaultPrompts.GetDefaultPrompts();
                await SaveSettingsAsync(settings);
            }
            
            return settings;
        }
        catch
        {
            return new AppSettings
            {
                CustomIgnoreFilters = new List<IgnoreFilter>()
            };
        }
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
        await File.WriteAllTextAsync(_settingsFile, json);
    }

    public async Task<List<string>> GetRecentProjectsAsync()
    {
        if (!File.Exists(_recentProjectsFile))
            return new List<string>();

        try
        {
            var json = await File.ReadAllTextAsync(_recentProjectsFile);
            return JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    public async Task AddRecentProjectAsync(string projectPath)
    {
        var recent = await GetRecentProjectsAsync();
        
        recent.Remove(projectPath);
        recent.Insert(0, projectPath);
        
        if (recent.Count > 10)
            recent = recent.Take(10).ToList();

        var json = JsonConvert.SerializeObject(recent, Formatting.Indented);
        await File.WriteAllTextAsync(_recentProjectsFile, json);
    }

    public async Task<SessionState> LoadSessionStateAsync()
    {
        if (!File.Exists(_sessionFile))
            return new SessionState();

        try
        {
            var json = await File.ReadAllTextAsync(_sessionFile);
            return JsonConvert.DeserializeObject<SessionState>(json) ?? new SessionState();
        }
        catch
        {
            return new SessionState();
        }
    }

    public async Task SaveSessionStateAsync(SessionState state)
    {
        var json = JsonConvert.SerializeObject(state, Formatting.Indented);
        await File.WriteAllTextAsync(_sessionFile, json);
    }
}
