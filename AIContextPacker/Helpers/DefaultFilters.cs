using System.Collections.Generic;
using AIContextPacker.Models;

namespace AIContextPacker.Helpers;

public static class DefaultFilters
{
    public static List<IgnoreFilter> GetDefaultFilters()
    {
        return new List<IgnoreFilter>
        {
            new IgnoreFilter
            {
                Name = ".NET Build",
                Patterns = new List<string>
                {
                    "bin/",
                    "obj/",
                    "*.dll",
                    "*.exe",
                    "*.pdb",
                    "*.cache",
                    ".vs/",
                    "*.user",
                    "*.suo",
                    "packages/",
                    "*.nupkg",
                    "*_wpftmp.csproj"
                }
            },
            new IgnoreFilter
            {
                Name = "Node.js",
                Patterns = new List<string>
                {
                    "node_modules/",
                    "package-lock.json",
                    "yarn.lock",
                    "npm-debug.log",
                    "*.log",
                    "dist/",
                    "build/",
                    ".next/",
                    ".nuxt/"
                }
            },
            new IgnoreFilter
            {
                Name = "Python",
                Patterns = new List<string>
                {
                    "__pycache__/",
                    "*.py[cod]",
                    "*$py.class",
                    "*.so",
                    ".Python",
                    "venv/",
                    "env/",
                    ".venv/",
                    "pip-log.txt",
                    "*.egg-info/",
                    "dist/",
                    "build/"
                }
            },
            new IgnoreFilter
            {
                Name = "Git",
                Patterns = new List<string>
                {
                    ".git/",
                    ".gitignore",
                    ".gitattributes"
                }
            },
            new IgnoreFilter
            {
                Name = "IDE",
                Patterns = new List<string>
                {
                    ".vscode/",
                    ".idea/",
                    "*.swp",
                    "*.swo",
                    "*~",
                    ".DS_Store",
                    "Thumbs.db"
                }
            }
        };
    }
}
