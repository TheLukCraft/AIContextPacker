using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using AIContextPacker.Models;
using AIContextPacker.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace AIContextPacker.Services;

/// <summary>
/// Provides services for managing pinned files in the file tree.
/// </summary>
public class PinService : IPinService
{
    private readonly ObservableCollection<FileTreeNode> _pinnedFiles;
    private readonly ILogger<PinService>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PinService"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    public PinService(ILogger<PinService>? logger = null)
    {
        _pinnedFiles = new ObservableCollection<FileTreeNode>();
        _logger = logger;
    }

    /// <inheritdoc/>
    public IReadOnlyList<FileTreeNode> PinnedFiles => _pinnedFiles.ToList().AsReadOnly();

    /// <inheritdoc/>
    public bool TogglePin(FileTreeNode node)
    {
        if (node == null)
        {
            _logger?.LogWarning("TogglePin called with null node");
            return false;
        }

        if (node.IsDirectory)
        {
            _logger?.LogDebug("Cannot pin directory: {Path}", node.FullPath);
            return false;
        }

        if (node.IsPinned)
        {
            return Unpin(node);
        }
        else
        {
            return Pin(node);
        }
    }

    /// <inheritdoc/>
    public bool Pin(FileTreeNode node)
    {
        if (node == null)
        {
            _logger?.LogWarning("Pin called with null node");
            return false;
        }

        if (node.IsDirectory)
        {
            _logger?.LogDebug("Cannot pin directory: {Path}", node.FullPath);
            return false;
        }

        if (node.IsPinned)
        {
            _logger?.LogDebug("File already pinned: {Path}", node.FullPath);
            return false;
        }

        node.IsPinned = true;
        node.IsSelected = false; // Pinned files are automatically deselected
        _pinnedFiles.Add(node);

        _logger?.LogDebug("Pinned file: {Path}", node.FullPath);
        return true;
    }

    /// <inheritdoc/>
    public bool Unpin(FileTreeNode node)
    {
        if (node == null)
        {
            _logger?.LogWarning("Unpin called with null node");
            return false;
        }

        if (!node.IsPinned)
        {
            _logger?.LogDebug("File is not pinned: {Path}", node.FullPath);
            return false;
        }

        node.IsPinned = false;
        _pinnedFiles.Remove(node);

        _logger?.LogDebug("Unpinned file: {Path}", node.FullPath);
        return true;
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetPinnedFilePaths()
    {
        return _pinnedFiles.Select(f => f.FullPath).ToList().AsReadOnly();
    }

    /// <inheritdoc/>
    public void ClearAll()
    {
        _logger?.LogDebug("Clearing all {Count} pinned files", _pinnedFiles.Count);

        var pinnedNodes = _pinnedFiles.ToList();
        foreach (var node in pinnedNodes)
        {
            node.IsPinned = false;
        }

        _pinnedFiles.Clear();
    }

    /// <inheritdoc/>
    public bool IsPinned(FileTreeNode node)
    {
        if (node == null)
        {
            return false;
        }

        return node.IsPinned;
    }
}
