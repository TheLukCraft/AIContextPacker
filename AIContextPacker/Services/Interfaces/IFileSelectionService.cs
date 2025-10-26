using System.Collections.Generic;
using AIContextPacker.Models;

namespace AIContextPacker.Services.Interfaces;

/// <summary>
/// Provides services for managing file selection in the file tree.
/// </summary>
public interface IFileSelectionService
{
    /// <summary>
    /// Selects all visible, non-pinned files in the tree.
    /// </summary>
    /// <param name="rootNode">The root node of the file tree.</param>
    void SelectAll(FileTreeNode rootNode);

    /// <summary>
    /// Deselects all visible, non-pinned files in the tree.
    /// </summary>
    /// <param name="rootNode">The root node of the file tree.</param>
    void DeselectAll(FileTreeNode rootNode);

    /// <summary>
    /// Gets the full paths of all selected, visible files in the tree.
    /// </summary>
    /// <param name="rootNode">The root node of the file tree.</param>
    /// <returns>An enumerable collection of file paths.</returns>
    IEnumerable<string> GetSelectedFilePaths(FileTreeNode rootNode);

    /// <summary>
    /// Counts the number of selected, visible files in the tree.
    /// </summary>
    /// <param name="rootNode">The root node of the file tree.</param>
    /// <returns>The count of selected files.</returns>
    int GetSelectedFileCount(FileTreeNode rootNode);
}
