using System.Collections.Generic;

namespace AIContextPacker.Models;

public class ProjectStructure
{
    public string RootPath { get; set; } = string.Empty;
    public FileTreeNode RootNode { get; set; } = new();
    public bool HasLocalGitignore { get; set; }
    public string LocalGitignorePath { get; set; } = string.Empty;
}
