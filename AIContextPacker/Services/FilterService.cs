using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AIContextPacker.Models;
using AIContextPacker.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace AIContextPacker.Services;

/// <summary>
/// Provides file and directory filtering based on patterns and rules.
/// </summary>
public class FilterService : IFilterService
{
    private readonly List<string> _allowedExtensions;
    private readonly List<IgnoreFilter> _activeFilters;
    private readonly List<string> _gitignorePatterns;
    private readonly string _basePath;
    private readonly ILogger<FilterService>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FilterService"/> class.
    /// </summary>
    public FilterService(
        List<string> allowedExtensions,
        List<IgnoreFilter> activeFilters,
        List<string> gitignorePatterns,
        string basePath,
        ILogger<FilterService>? logger = null)
    {
        _allowedExtensions = allowedExtensions;
        _activeFilters = activeFilters;
        _gitignorePatterns = gitignorePatterns;
        _basePath = basePath;
        _logger = logger;
    }

    /// <inheritdoc/>
    public bool ShouldIncludeFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        
        // Stage 1: Whitelist check
        if (!_allowedExtensions.Contains(extension))
            return false;

        // Stage 2: Blacklist filters
        if (IsIgnoredByFilters(filePath))
            return false;

        // Stage 3: .gitignore patterns
        if (IsIgnoredByGitignore(filePath))
            return false;

        return true;
    }

    /// <inheritdoc/>
    public bool ShouldShowDirectory(string directoryPath)
    {
        // Check if directory should be shown based on filters and .gitignore
        if (IsIgnoredByFilters(directoryPath))
            return false;

        if (IsIgnoredByGitignore(directoryPath))
            return false;

        return true;
    }

    /// <inheritdoc/>
    public bool ShouldShowInStructure(string path)
    {
        // For "Copy Structure", we show all files including those filtered by whitelist
        // but still respect blacklist filters
        if (IsIgnoredByFilters(path))
            return false;

        if (IsIgnoredByGitignore(path))
            return false;

        return true;
    }

    private bool IsIgnoredByFilters(string path)
    {
        var relativePath = GetRelativePath(path);

        foreach (var filter in _activeFilters)
        {
            foreach (var pattern in filter.Patterns)
            {
                if (MatchesGitignorePattern(relativePath, pattern))
                    return true;
            }
        }

        return false;
    }

    private bool IsIgnoredByGitignore(string path)
    {
        var relativePath = GetRelativePath(path);

        foreach (var pattern in _gitignorePatterns)
        {
            if (MatchesGitignorePattern(relativePath, pattern))
                return true;
        }

        return false;
    }

    private bool MatchesGitignorePattern(string path, string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return false;

        // Remove leading/trailing whitespace
        pattern = pattern.Trim();

        // Handle negation (not implemented yet, just skip)
        if (pattern.StartsWith("!"))
            return false;

        // Normalize path separators
        path = path.Replace(Path.DirectorySeparatorChar, '/');
        
        // Check if pattern is for directories (ends with /)
        bool isDirectoryPattern = pattern.EndsWith("/");
        var cleanPattern = pattern.TrimEnd('/');
        var cleanPath = path.TrimEnd('/');

        // Handle rooted patterns (starting with /)
        bool isRooted = cleanPattern.StartsWith("/");
        if (isRooted)
        {
            cleanPattern = cleanPattern.TrimStart('/');
        }

        // Handle ** wildcards
        if (cleanPattern.Contains("**/"))
        {
            // **/ at the start means match anywhere
            cleanPattern = cleanPattern.Replace("**/", "");
            if (MatchesSimplePattern(cleanPath, cleanPattern) || 
                cleanPath.Contains("/" + cleanPattern) ||
                cleanPath.EndsWith("/" + cleanPattern))
                return true;
        }
        else if (cleanPattern.Contains("**"))
        {
            // Handle other ** patterns
            var regexPattern = "^" + Regex.Escape(cleanPattern)
                .Replace("\\*\\*", ".*")
                .Replace("\\*", "[^/]*")
                .Replace("\\?", "[^/]") + "$";
            
            if (Regex.IsMatch(cleanPath, regexPattern, RegexOptions.IgnoreCase))
                return true;
        }
        else if (cleanPattern.Contains("*") || cleanPattern.Contains("?") || cleanPattern.Contains("["))
        {
            // Simple glob matching (includes *, ?, and [] patterns)
            if (MatchesSimplePattern(cleanPath, cleanPattern))
                return true;
                
            // Check if any path segment matches
            var pathParts = cleanPath.Split('/');
            if (!isRooted)
            {
                foreach (var part in pathParts)
                {
                    if (MatchesSimplePattern(part, cleanPattern))
                        return true;
                }
            }
        }
        else
        {
            // Exact match without wildcards
            if (isRooted)
            {
                // Rooted pattern must match from the start
                if (cleanPath.Equals(cleanPattern, StringComparison.OrdinalIgnoreCase))
                    return true;
                if (cleanPath.StartsWith(cleanPattern + "/", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            else
            {
                // Non-rooted pattern can match anywhere
                if (cleanPath.Equals(cleanPattern, StringComparison.OrdinalIgnoreCase))
                    return true;
                
                // Match as path segment
                var pathParts = cleanPath.Split('/');
                foreach (var part in pathParts)
                {
                    if (part.Equals(cleanPattern, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                
                // Match if it appears as a complete segment in the path
                if (cleanPath.Contains("/" + cleanPattern + "/", StringComparison.OrdinalIgnoreCase) ||
                    cleanPath.EndsWith("/" + cleanPattern, StringComparison.OrdinalIgnoreCase) ||
                    cleanPath.StartsWith(cleanPattern + "/", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    private bool MatchesSimplePattern(string text, string pattern)
    {
        // Convert glob pattern to regex
        var regexPattern = "^";
        
        for (int i = 0; i < pattern.Length; i++)
        {
            char c = pattern[i];
            
            if (c == '*')
            {
                regexPattern += "[^/]*";
            }
            else if (c == '?')
            {
                regexPattern += "[^/]";
            }
            else if (c == '[')
            {
                // Handle character class [abc] or [a-z]
                int closeBracket = pattern.IndexOf(']', i);
                if (closeBracket > i)
                {
                    // Extract the character class and add it as-is to regex
                    regexPattern += pattern.Substring(i, closeBracket - i + 1);
                    i = closeBracket; // Skip to closing bracket
                }
                else
                {
                    // No closing bracket, treat as literal
                    regexPattern += Regex.Escape(c.ToString());
                }
            }
            else
            {
                regexPattern += Regex.Escape(c.ToString());
            }
        }
        
        regexPattern += "$";
        
        return Regex.IsMatch(text, regexPattern, RegexOptions.IgnoreCase);
    }

    private string GetRelativePath(string fullPath)
    {
        if (fullPath.StartsWith(_basePath, StringComparison.OrdinalIgnoreCase))
        {
            var relative = fullPath.Substring(_basePath.Length)
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return relative.Replace(Path.DirectorySeparatorChar, '/');
        }
        return fullPath.Replace(Path.DirectorySeparatorChar, '/');
    }

    /// <inheritdoc/>
    public async Task ApplyFiltersAsync(FileTreeNode rootNode, IProgressReporter? progress = null)
    {
        _logger?.LogInformation("Applying filters to file tree");
        
        var totalNodes = CountNodes(rootNode);
        var processedNodes = 0;

        await Task.Run(() =>
        {
            ApplyFiltersRecursive(rootNode, ref processedNodes, totalNodes, progress);
        });

        progress?.Clear();
        _logger?.LogInformation("Filters applied to {TotalNodes} nodes", totalNodes);
    }

    private void ApplyFiltersRecursive(
        FileTreeNode node, 
        ref int processedNodes, 
        int totalNodes, 
        IProgressReporter? progress)
    {
        if (progress?.CancellationToken.IsCancellationRequested == true)
            return;

        processedNodes++;

        if (processedNodes % 50 == 0 && totalNodes > 0)
        {
            var percent = (double)processedNodes / totalNodes * 100;
            progress?.Report($"Filtering: {processedNodes}/{totalNodes} items...", percent);
        }

        if (!node.IsDirectory)
        {
            // For files, check if they should be included
            node.IsVisible = ShouldIncludeFile(node.FullPath);
        }
        else
        {
            // First check if the directory itself should be filtered out
            var shouldShowDir = ShouldShowDirectory(node.FullPath);
            
            if (!shouldShowDir)
            {
                // Directory is filtered out - hide it and all children
                node.IsVisible = false;
                _logger?.LogDebug("Directory filtered out: {Path}", node.FullPath);
                return;
            }
            
            // Directory is not filtered, process children
            foreach (var child in node.Children)
            {
                ApplyFiltersRecursive(child, ref processedNodes, totalNodes, progress);
            }

            // Directory is visible if it has any visible children
            node.IsVisible = node.Children.Any(c => c.IsVisible);
        }
    }

    private int CountNodes(FileTreeNode node)
    {
        var count = 1;
        foreach (var child in node.Children)
        {
            count += CountNodes(child);
        }
        return count;
    }
}

