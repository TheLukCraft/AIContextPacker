using System.Collections.Generic;
using System.Linq;
using AIContextPacker.Models;
using AIContextPacker.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace AIContextPacker.Services;

/// <summary>
/// Provides services for managing file selection in the file tree.
/// </summary>
public class FileSelectionService : IFileSelectionService
{
    private readonly ILogger<FileSelectionService>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSelectionService"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    public FileSelectionService(ILogger<FileSelectionService>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public void SelectAll(FileTreeNode rootNode)
    {
        if (rootNode == null)
        {
            _logger?.LogWarning("SelectAll called with null root node");
            return;
        }

        _logger?.LogDebug("Selecting all visible, non-pinned files");
        SelectAllRecursive(rootNode, true);
    }

    /// <inheritdoc/>
    public void DeselectAll(FileTreeNode rootNode)
    {
        if (rootNode == null)
        {
            _logger?.LogWarning("DeselectAll called with null root node");
            return;
        }

        _logger?.LogDebug("Deselecting all visible, non-pinned files");
        SelectAllRecursive(rootNode, false);
    }

    /// <inheritdoc/>
    public IEnumerable<string> GetSelectedFilePaths(FileTreeNode rootNode)
    {
        if (rootNode == null)
        {
            _logger?.LogWarning("GetSelectedFilePaths called with null root node");
            yield break;
        }

        foreach (var path in GetSelectedFilePathsRecursive(rootNode))
        {
            yield return path;
        }
    }

    /// <inheritdoc/>
    public int GetSelectedFileCount(FileTreeNode rootNode)
    {
        if (rootNode == null)
        {
            return 0;
        }

        var count = 0;
        foreach (var _ in GetSelectedFilePaths(rootNode))
        {
            count++;
        }

        return count;
    }

    private void SelectAllRecursive(FileTreeNode node, bool select)
    {
        // Skip invisible or pinned nodes
        if (!node.IsVisible || node.IsPinned)
        {
            return;
        }

        // For directories, recursively process children first, then update directory state
        if (node.IsDirectory)
        {
            foreach (var child in node.Children)
            {
                SelectAllRecursive(child, select);
            }
            
            // Update directory checkbox based on children state
            // This ensures the checkbox reflects the actual state of children
            UpdateDirectorySelectionState(node);
        }
        else
        {
            // For files, directly set the selection state
            node.IsSelected = select;
        }
    }

    private void UpdateDirectorySelectionState(FileTreeNode directory)
    {
        if (!directory.IsDirectory || directory.Children.Count == 0)
        {
            return;
        }

        var visibleChildren = directory.Children.Where(c => c.IsVisible && !c.IsPinned).ToList();
        if (visibleChildren.Count == 0)
        {
            directory.IsSelected = false;
            return;
        }

        var selectedCount = visibleChildren.Count(c => c.IsSelected);
        directory.IsSelected = selectedCount == visibleChildren.Count;
    }

    private IEnumerable<string> GetSelectedFilePathsRecursive(FileTreeNode node)
    {
        if (!node.IsDirectory && node.IsSelected && node.IsVisible)
        {
            yield return node.FullPath;
        }

        foreach (var child in node.Children)
        {
            foreach (var path in GetSelectedFilePathsRecursive(child))
            {
                yield return path;
            }
        }
    }
}
