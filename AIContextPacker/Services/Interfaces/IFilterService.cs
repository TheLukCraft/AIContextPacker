using System.Threading.Tasks;
using AIContextPacker.Models;
using AIContextPacker.Services.Interfaces;

namespace AIContextPacker.Services.Interfaces;

/// <summary>
/// Service responsible for filtering files and directories based on rules.
/// </summary>
public interface IFilterService
{
    /// <summary>
    /// Determines if a file should be included based on filters.
    /// </summary>
    /// <param name="filePath">The file path to check.</param>
    /// <returns>True if the file should be included; otherwise, false.</returns>
    bool ShouldIncludeFile(string filePath);

    /// <summary>
    /// Determines if a directory should be shown based on filters.
    /// </summary>
    /// <param name="directoryPath">The directory path to check.</param>
    /// <returns>True if the directory should be shown; otherwise, false.</returns>
    bool ShouldShowDirectory(string directoryPath);

    /// <summary>
    /// Determines if a path should be shown in the structure view.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>True if the path should be shown; otherwise, false.</returns>
    bool ShouldShowInStructure(string path);

    /// <summary>
    /// Applies filters to a file tree asynchronously with progress reporting.
    /// </summary>
    /// <param name="rootNode">The root node of the tree.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ApplyFiltersAsync(FileTreeNode rootNode, IProgressReporter? progress = null);
}
