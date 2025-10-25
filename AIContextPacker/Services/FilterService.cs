using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AIContextPacker.Models;

namespace AIContextPacker.Services;

public class FilterService
{
    private readonly List<string> _allowedExtensions;
    private readonly List<IgnoreFilter> _activeFilters;
    private readonly List<string> _gitignorePatterns;
    private readonly string _basePath;

    public FilterService(
        List<string> allowedExtensions,
        List<IgnoreFilter> activeFilters,
        List<string> gitignorePatterns,
        string basePath)
    {
        _allowedExtensions = allowedExtensions;
        _activeFilters = activeFilters;
        _gitignorePatterns = gitignorePatterns;
        _basePath = basePath;
    }

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

        // Handle negation
        if (pattern.StartsWith("!"))
            return false;

        // Simple pattern matching
        // Remove trailing slash for directory patterns
        var cleanPattern = pattern.TrimEnd('/');
        var cleanPath = path.TrimEnd('/');

        // Handle wildcards
        if (pattern.Contains("**"))
        {
            // **/ means match in any directory
            var patternPart = pattern.Replace("**/", "");
            if (cleanPath.Contains(patternPart) || cleanPath.EndsWith(patternPart))
                return true;
        }

        // Simple glob matching
        if (pattern.Contains("*"))
        {
            var regexPattern = "^" + Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".") + "$";
            
            if (Regex.IsMatch(cleanPath, regexPattern, RegexOptions.IgnoreCase))
                return true;
                
            // Also check if any path segment matches
            var pathParts = cleanPath.Split('/');
            foreach (var part in pathParts)
            {
                if (Regex.IsMatch(part, regexPattern, RegexOptions.IgnoreCase))
                    return true;
            }
        }
        else
        {
            // Exact match or path segment match
            if (cleanPath.Equals(cleanPattern, StringComparison.OrdinalIgnoreCase))
                return true;
                
            if (cleanPath.Contains("/" + cleanPattern, StringComparison.OrdinalIgnoreCase))
                return true;
                
            if (cleanPath.EndsWith(cleanPattern, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private string GetRelativePath(string fullPath)
    {
        if (fullPath.StartsWith(_basePath))
        {
            var relative = fullPath.Substring(_basePath.Length).TrimStart(Path.DirectorySeparatorChar);
            return relative.Replace(Path.DirectorySeparatorChar, '/');
        }
        return fullPath.Replace(Path.DirectorySeparatorChar, '/');
    }
}
