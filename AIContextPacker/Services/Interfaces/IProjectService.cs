using System.Threading.Tasks;
using AIContextPacker.Models;

namespace AIContextPacker.Services.Interfaces;

/// <summary>
/// Service responsible for project management operations.
/// </summary>
public interface IProjectService
{
    /// <summary>
    /// Gets the currently loaded project structure, or null if no project is loaded.
    /// </summary>
    ProjectStructure? CurrentProject { get; }

    /// <summary>
    /// Gets the current project path.
    /// </summary>
    string? CurrentProjectPath { get; }

    /// <summary>
    /// Gets whether a project is currently loaded.
    /// </summary>
    bool IsProjectLoaded { get; }

    /// <summary>
    /// Loads a project from the specified folder path.
    /// </summary>
    /// <param name="folderPath">The folder path to load.</param>
    /// <param name="progress">Optional progress reporter for status updates.</param>
    /// <returns>The loaded project structure.</returns>
    Task<ProjectStructure> LoadProjectAsync(string folderPath, IProgressReporter? progress = null);

    /// <summary>
    /// Unloads the current project.
    /// </summary>
    void UnloadProject();

    /// <summary>
    /// Gets the root node of the current project's file tree.
    /// </summary>
    /// <returns>The root file tree node, or null if no project is loaded.</returns>
    FileTreeNode? GetRootNode();
}
