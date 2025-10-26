using System;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using AIContextPacker.Models;
using AIContextPacker.Services.Interfaces;

namespace AIContextPacker.Services;

public class UpdateService : IUpdateService
{
    private const string GitHubOwner = "TheLukCraft";
    private const string GitHubRepo = "AIContextPacker";
    private const string GitHubApiUrl = $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/releases/latest";

    public string GetCurrentVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
    }

    public async Task<UpdateInfo> CheckForUpdatesAsync()
    {
        var currentVersion = GetCurrentVersion();
        
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
                return new UpdateInfo
                {
                    CurrentVersion = currentVersion,
                    IsUpdateAvailable = false
                };
            }

            var latestVersion = release.TagName?.TrimStart('v') ?? "0.0.0";
            var isNewer = CompareVersions(latestVersion, currentVersion) > 0;

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
            System.Diagnostics.Debug.WriteLine($"Update check failed: {ex.Message}");
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
        public string? TagName { get; set; }
        public string? Name { get; set; }
        public string? Body { get; set; }
        public string? HtmlUrl { get; set; }
        public DateTime PublishedAt { get; set; }
    }
}
