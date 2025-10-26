using System.Collections.Generic;

namespace AIContextPacker.Models;

public class AppSettings
{
    public int MaxCharsLimit { get; set; } = 10000;
    public ThemeMode Theme { get; set; } = ThemeMode.System;
    public bool IncludeFileHeaders { get; set; } = true;
    public List<string> AllowedExtensions { get; set; } = new()
    {
        ".aab", ".ac", ".am", ".apk", ".asset", ".bat", ".bash", ".bib", ".blade.php", 
        ".cjs", ".class", ".cmake", ".conf", ".cpp", ".cs", ".csproj", ".css", ".csv", 
        ".cu", ".dart", ".dll", ".dmg", ".el", ".env", ".erb", ".exe", ".f90", ".f95", 
        ".fs", ".gd", ".gemfile", ".gemspec", ".go", ".gradle", ".groovy", ".h", ".h5", 
        ".hx", ".html", ".iml", ".in", ".ini", ".ipynb", ".jar", ".java", ".jenkinsfile", 
        ".jl", ".js", ".json", ".jsx", ".kts", ".kt", ".lnk", ".lock", ".lua", ".m", 
        ".mat", ".md", ".mdx", ".meta", ".mjs", ".mo", ".mod", ".mproj", ".nix", ".pde", 
        ".php", ".pickle", ".pkg", ".plist", ".po", ".pom", ".pkr.hcl", ".pkl", ".pklx", 
        ".pp", ".pyo", ".pyd", ".py", ".pyc", ".r", ".rake", ".rb", ".res", ".rs", 
        ".sass", ".scala", ".sbv", ".sbt", ".scss", ".service", ".settings", ".sh", 
        ".sln", ".spec.ts", ".storyboard", ".sum", ".sublime-project", ".sublime-workspace", 
        ".swift", ".tif", ".toml", ".ts", ".tscn", ".tsx", ".tsv", ".twig", ".unity", 
        ".uproject", ".vb", ".vimrc", ".war", ".workspace", ".xib", ".xml", ".xproj", 
        ".xcworkspace", ".xcproject", ".yaml", ".yml", ".zsh"
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
