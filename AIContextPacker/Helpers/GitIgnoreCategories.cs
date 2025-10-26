using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AIContextPacker.Models;

namespace AIContextPacker.Helpers;

public static class GitIgnoreCategories
{
    public class CategoryFilter
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
    }

    private static readonly List<CategoryFilter> _categoryMappings = new()
    {
        // Backend
        new() { Name = "Node.js", Category = "Backend", FileName = "Node.gitignore" },
        new() { Name = "Python", Category = "Backend", FileName = "Python.gitignore" },
        new() { Name = "Java", Category = "Backend", FileName = "Java.gitignore" },
        new() { Name = "Go", Category = "Backend", FileName = "Go.gitignore" },
        new() { Name = ".NET", Category = "Backend", FileName = "Dotnet.gitignore" },
        new() { Name = "Laravel", Category = "Backend", FileName = "Laravel.gitignore" },
        new() { Name = "Rails", Category = "Backend", FileName = "Rails.gitignore" },
        new() { Name = "Ruby", Category = "Backend", FileName = "Ruby.gitignore" },
        new() { Name = "Rust", Category = "Backend", FileName = "Rust.gitignore" },
        new() { Name = "Scala", Category = "Backend", FileName = "Scala.gitignore" },

        // Frontend
        new() { Name = "Angular", Category = "Frontend", FileName = "Angular.gitignore" },
        new() { Name = "Next.js", Category = "Frontend", FileName = "Nextjs.gitignore" },
        new() { Name = "Sass", Category = "Frontend", FileName = "Sass.gitignore" },
        new() { Name = "Jekyll", Category = "Frontend", FileName = "Jekyll.gitignore" },
        new() { Name = "GitHub Pages", Category = "Frontend", FileName = "GitHubPages.gitignore" },
        new() { Name = "GitBook", Category = "Frontend", FileName = "GitBook.gitignore" },
        new() { Name = "ExtJS", Category = "Frontend", FileName = "ExtJs.gitignore" },
        new() { Name = "WordPress", Category = "Frontend", FileName = "WordPress.gitignore" },
        new() { Name = "Drupal", Category = "Frontend", FileName = "Drupal.gitignore" },
        new() { Name = "Joomla", Category = "Frontend", FileName = "Joomla.gitignore" },

        // Mobile
        new() { Name = "Android", Category = "Mobile", FileName = "Android.gitignore" },
        new() { Name = "Kotlin", Category = "Mobile", FileName = "Kotlin.gitignore" },
        new() { Name = "Swift", Category = "Mobile", FileName = "Swift.gitignore" },
        new() { Name = "Objective-C", Category = "Mobile", FileName = "Objective-C.gitignore" },
        new() { Name = "Flutter", Category = "Mobile", FileName = "Flutter.gitignore" },
        new() { Name = "Dart", Category = "Mobile", FileName = "Dart.gitignore" },
        new() { Name = "Xcode", Category = "Mobile", FileName = "Xcode.gitignore" },
        new() { Name = "Appcelerator Titanium", Category = "Mobile", FileName = "AppceleratorTitanium.gitignore" },
        new() { Name = "Gradle", Category = "Mobile", FileName = "Gradle.gitignore" },

        // DevOps / CI
        new() { Name = "Chef Cookbook", Category = "DevOps / CI", FileName = "ChefCookbook.gitignore" },
        new() { Name = "Jenkins", Category = "DevOps / CI", FileName = "JENKINS_HOME.gitignore" },
        new() { Name = "Terraform", Category = "DevOps / CI", FileName = "Terraform.gitignore" },
        new() { Name = "Packer", Category = "DevOps / CI", FileName = "Packer.gitignore" },
        new() { Name = "Maven", Category = "DevOps / CI", FileName = "Maven.gitignore" },
        new() { Name = "CMake", Category = "DevOps / CI", FileName = "CMake.gitignore" },
        new() { Name = "Autotools", Category = "DevOps / CI", FileName = "Autotools.gitignore" },
        new() { Name = "SCons", Category = "DevOps / CI", FileName = "SCons.gitignore" },
        new() { Name = "Composer", Category = "DevOps / CI", FileName = "Composer.gitignore" },

        // Operating Systems
        new() { Name = "Windows", Category = "Operating Systems", FileName = "Windows.gitignore" },
        new() { Name = "Linux", Category = "Operating Systems", FileName = "Linux.gitignore" },
        new() { Name = "Arch Linux", Category = "Operating Systems", FileName = "ArchLinuxPackages.gitignore" },
        new() { Name = "Nix", Category = "Operating Systems", FileName = "Nix.gitignore" },

        // IDEs / Editors
        new() { Name = "JetBrains", Category = "IDEs / Editors", FileName = "JetBrains.gitignore" },
        new() { Name = "Visual Studio", Category = "IDEs / Editors", FileName = "VisualStudio.gitignore" },
        new() { Name = "VS Code", Category = "IDEs / Editors", FileName = "VisualStudioCode.gitignore" },
        new() { Name = "Eclipse", Category = "IDEs / Editors", FileName = "Eclipse.gitignore" },
        new() { Name = "Vim", Category = "IDEs / Editors", FileName = "Vim.gitignore" },
        new() { Name = "Emacs", Category = "IDEs / Editors", FileName = "Emacs.gitignore" },
        new() { Name = "Sublime Text", Category = "IDEs / Editors", FileName = "SublimeText.gitignore" },
        new() { Name = "Notepad++", Category = "IDEs / Editors", FileName = "NotepadPP.gitignore" },
        new() { Name = "Kate", Category = "IDEs / Editors", FileName = "Kate.gitignore" },

        // Game Development
        new() { Name = "Unity", Category = "Game Development", FileName = "Unity.gitignore" },
        new() { Name = "Unreal Engine", Category = "Game Development", FileName = "UnrealEngine.gitignore" },
        new() { Name = "Godot", Category = "Game Development", FileName = "Godot.gitignore" },
        new() { Name = "Flax Engine", Category = "Game Development", FileName = "FlaxEngine.gitignore" },
        new() { Name = "Adventure Game Studio", Category = "Game Development", FileName = "AdventureGameStudio.gitignore" },
        new() { Name = "Haxe", Category = "Game Development", FileName = "Haxe.gitignore" },
        new() { Name = "C++", Category = "Game Development", FileName = "C++.gitignore" },
        new() { Name = "Lua", Category = "Game Development", FileName = "Lua.gitignore" },
        new() { Name = "Processing", Category = "Game Development", FileName = "Processing.gitignore" },

        // Data Science / Machine Learning
        new() { Name = "R", Category = "Data Science / ML", FileName = "R.gitignore" },
        new() { Name = "Julia", Category = "Data Science / ML", FileName = "Julia.gitignore" },
        new() { Name = "MATLAB", Category = "Data Science / ML", FileName = "MATLAB.gitignore" },
        new() { Name = "Fortran", Category = "Data Science / ML", FileName = "Fortran.gitignore" },
        new() { Name = "CUDA", Category = "Data Science / ML", FileName = "CUDA.gitignore" },
        new() { Name = "TeX", Category = "Data Science / ML", FileName = "TeX.gitignore" }
    };

    public static List<string> GetAllCategories()
    {
        return _categoryMappings
            .Select(m => m.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToList();
    }

    public static List<IgnoreFilter> GetFiltersForCategory(string category)
    {
        var filtersInCategory = _categoryMappings
            .Where(m => m.Category == category)
            .ToList();

        var result = new List<IgnoreFilter>();

        foreach (var filter in filtersInCategory)
        {
            var patterns = LoadPatternsFromFile(filter.FileName);
            if (patterns.Count > 0)
            {
                result.Add(new IgnoreFilter
                {
                    Name = filter.Name,
                    Patterns = patterns
                });
            }
        }

        return result;
    }

    private static List<string> LoadPatternsFromFile(string fileName)
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"AIContextPacker.Resources.GitIgnores.{fileName}";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                // If not embedded, try loading from file system
                var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "GitIgnores", fileName);
                if (File.Exists(filePath))
                {
                    return File.ReadAllLines(filePath)
                        .Where(line => !string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith("#"))
                        .Select(line => line.Trim())
                        .ToList();
                }
                return new List<string>();
            }

            using var reader = new StreamReader(stream);
            var patterns = new List<string>();
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                // Skip comments and empty lines
                if (!string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith("#"))
                {
                    patterns.Add(line.Trim());
                }
            }
            return patterns;
        }
        catch
        {
            return new List<string>();
        }
    }
}
