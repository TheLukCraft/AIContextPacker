using System.Collections.Generic;
using AIContextPacker.Models;

namespace AIContextPacker.Services.Interfaces;

/// <summary>
/// Provides services for managing pinned files in the file tree.
/// </summary>
public interface IPinService
{
    /// <summary>
    /// Gets the collection of currently pinned files.
    /// </summary>
    IReadOnlyList<FileTreeNode> PinnedFiles { get; }

    /// <summary>
    /// Toggles the pin status of a file node.
    /// </summary>
    /// <param name="node">The file node to toggle.</param>
    /// <returns>True if the file is now pinned, false if unpinned.</returns>
    bool TogglePin(FileTreeNode node);

    /// <summary>
    /// Pins a file node.
    /// </summary>
    /// <param name="node">The file node to pin.</param>
    /// <returns>True if the file was pinned, false if it was already pinned or is a directory.</returns>
    bool Pin(FileTreeNode node);

    /// <summary>
    /// Unpins a file node.
    /// </summary>
    /// <param name="node">The file node to unpin.</param>
    /// <returns>True if the file was unpinned, false if it was not pinned.</returns>
    bool Unpin(FileTreeNode node);

    /// <summary>
    /// Gets the file paths of all pinned files.
    /// </summary>
    /// <returns>A list of file paths.</returns>
    IReadOnlyList<string> GetPinnedFilePaths();

    /// <summary>
    /// Clears all pinned files.
    /// </summary>
    void ClearAll();

    /// <summary>
    /// Checks if a file node is pinned.
    /// </summary>
    /// <param name="node">The file node to check.</param>
    /// <returns>True if pinned, false otherwise.</returns>
    bool IsPinned(FileTreeNode node);
}
