using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AIContextPacker.Models;

public partial class FileTreeNode : ObservableObject
{
    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string fullPath = string.Empty;

    [ObservableProperty]
    private bool isDirectory;

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private bool isExpanded;

    [ObservableProperty]
    private bool isPinned;

    [ObservableProperty]
    private bool isVisible = true;

    [ObservableProperty]
    private long fileSize;

    public FileTreeNode? Parent { get; set; }
    public ObservableCollection<FileTreeNode> Children { get; set; } = new();

    partial void OnIsSelectedChanged(bool value)
    {
        if (IsDirectory && Children.Count > 0)
        {
            foreach (var child in Children)
            {
                child.IsSelected = value;
            }
        }
    }
}
