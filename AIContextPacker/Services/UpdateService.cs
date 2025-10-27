using System;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using AIContextPacker.Models;
using AIContextPacker.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

namespace AIContextPacker.Services;

public class UpdateService : IUpdateService
{
    private readonly ILogger<UpdateService> _logger;
    private const string GitHubOwner = "TheLukCraft";
    private const string GitHubRepo = "AIContextPacker";
    private const string GitHubApiUrl = $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/releases/latest";

    public UpdateService(ILogger<UpdateService> logger)
    {
        _logger = logger;
    }

    public string GetCurrentVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;

        // Return version from assembly, with fallback to read from embedded resource or default
        if (version != null && version.Major > 0)
        {
            return $"{version.Major}.{version.Minor}.{version.Build}";
        }

        // Fallback: should never happen as version is set in .csproj
        return "0.0.0";
    }

    public async Task<UpdateInfo> CheckForUpdatesAsync()
    {
        var currentVersion = GetCurrentVersion();
        _logger.LogDebug("Current version identified as: {CurrentVersion}", currentVersion);
        
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "AIContextPacker");
            client.Timeout = TimeSpan.FromSeconds(10);

            var response = await client.GetAsync(GitHubApiUrl);
            
            if (!response.IsSuccessStatusCode)
            {
                return new UpdateInfo
                {
                    CurrentVersion = currentVersion,
                    IsUpdateAvailable = false
                };
            }

            var json = await response.Content.ReadAsStringAsync();
            var release = JsonSerializer.Deserialize<GitHubRelease>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (release == null)
            {
                _logger.LogWarning("Failed to parse GitHub release or TagName is missing.");
                return new UpdateInfo
                {
                    CurrentVersion = currentVersion,
                    IsUpdateAvailable = false
                };
            }

            var latestVersion = release.TagName?.TrimStart('v') ?? "0.0.0";
            _logger.LogDebug("Latest version tag from GitHub: '{TagName}', Parsed latest version: '{LatestVersion}'", release.TagName, latestVersion);

            _logger.LogInformation("Comparing latest version '{LatestVersion}' with current version '{CurrentVersion}'", latestVersion, currentVersion);
            var isNewer = CompareVersions(latestVersion, currentVersion) > 0;
            _logger.LogDebug("CompareVersions result: {ComparisonResult}", isNewer ? 1 : 0); // Log 4 (1 means latest>current, -1 means latest<current, 0 means equal)

            return new UpdateInfo
            {
                CurrentVersion = currentVersion,
                LatestVersion = latestVersion,
                IsUpdateAvailable = isNewer,
                ReleaseUrl = release.HtmlUrl ?? $"https://github.com/{GitHubOwner}/{GitHubRepo}/releases",
                ReleaseName = release.Name ?? $"Version {latestVersion}",
                ReleaseNotes = release.Body ?? "No release notes available.",
                PublishedAt = release.PublishedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check for updates: {Message}", ex.Message);
            System.Diagnostics.Debug.WriteLine($"Update check failed: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            return new UpdateInfo
            {
                CurrentVersion = currentVersion,
                IsUpdateAvailable = false
            };
        }
    }

    private int CompareVersions(string version1, string version2)
    {
        var v1Parts = version1.Split('.');
        var v2Parts = version2.Split('.');

        for (int i = 0; i < Math.Max(v1Parts.Length, v2Parts.Length); i++)
        {
            int v1Part = i < v1Parts.Length && int.TryParse(v1Parts[i], out var p1) ? p1 : 0;
            int v2Part = i < v2Parts.Length && int.TryParse(v2Parts[i], out var p2) ? p2 : 0;

            if (v1Part > v2Part) return 1;
            if (v1Part < v2Part) return -1;
        }

        return 0;
    }

    private class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("body")]
        public string? Body { get; set; }

        [JsonPropertyName("html_url")]
        public string? HtmlUrl { get; set; }

        [JsonPropertyName("published_at")]
        public DateTime PublishedAt { get; set; }
    }
}
