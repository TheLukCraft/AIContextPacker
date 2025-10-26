using System.Collections.Generic;

namespace AIContextPacker.Models;

public class AppSettings
{
    public int MaxCharsLimit { get; set; } = 10000;
    public ThemeMode Theme { get; set; } = ThemeMode.System;
    public bool IncludeFileHeaders { get; set; } = true;
    public List<string> AllowedExtensions { get; set; } = new()
    {
        ".cs", ".html", ".css", ".js", ".ts", ".json", ".md", 
        ".java", ".py", ".xml", ".txt", ".xaml", ".tsx", ".jsx"
    };
    public List<IgnoreFilter> CustomIgnoreFilters { get; set; } = new();
    public List<GlobalPrompt> GlobalPrompts { get; set; } = new();
    public Dictionary<string, bool> ActiveFilters { get; set; } = new();
}

public enum ThemeMode
{
    Light,
    Dark,
    System
}
