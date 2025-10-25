using System.Collections.Generic;

namespace AIContextPacker.Models;

public class SessionState
{
    public string LastProjectPath { get; set; } = string.Empty;
    public List<string> SelectedFiles { get; set; } = new();
    public List<string> PinnedFiles { get; set; } = new();
    public Dictionary<string, bool> ExpandedFolders { get; set; } = new();
    public string SelectedGlobalPrompt { get; set; } = string.Empty;
    public bool UseDetectedGitignore { get; set; } = true;
}
