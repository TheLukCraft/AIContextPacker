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

        // Convert gitignore pattern to regex
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*\\*/", "(.*/)?")
            .Replace("\\*", "[^/]*")
            .Replace("\\?", ".")
            .Replace("/", Regex.Escape(Path.DirectorySeparatorChar.ToString()));

        // If pattern ends with /, it only matches directories
        if (pattern.EndsWith("/"))
        {
            regexPattern = regexPattern.TrimEnd(Regex.Escape(Path.DirectorySeparatorChar.ToString()).ToCharArray());
            return Directory.Exists(Path.Combine(_basePath, path)) && 
                   Regex.IsMatch(path, regexPattern, RegexOptions.IgnoreCase);
        }

        // If pattern starts with /, it's relative to root
        if (!pattern.StartsWith("/") && !pattern.StartsWith("*"))
        {
            regexPattern = "(.*/)?(" + regexPattern.Substring(1) + ")";
        }

        regexPattern += "$";

        return Regex.IsMatch(path, regexPattern, RegexOptions.IgnoreCase);
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
