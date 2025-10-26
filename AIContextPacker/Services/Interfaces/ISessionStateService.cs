using AIContextPacker.Models;

namespace AIContextPacker.Services.Interfaces;

/// <summary>
/// Service for managing application session state (last project, pinned files, settings).
/// </summary>
public interface ISessionStateService
{
    /// <summary>
    /// Saves the current session state including project path, pinned files, selected files, and settings.
    /// </summary>
    /// <param name="currentProjectPath">Current project path</param>
    /// <param name="rootNode">Root node of the project tree (for selected files)</param>
    /// <param name="selectedGlobalPromptId">ID of selected global prompt</param>
    /// <param name="useDetectedGitignore">Whether to use detected .gitignore</param>
    Task SaveSessionStateAsync(string currentProjectPath, FileTreeNode? rootNode, string? selectedGlobalPromptId, bool useDetectedGitignore);

    /// <summary>
    /// Restores the session state by loading the last project and restoring pinned/selected files.
    /// </summary>
    /// <param name="onProjectLoad">Callback to load project by path</param>
    /// <param name="onPinFile">Callback to pin a file by node</param>
    /// <param name="findNodeByPath">Function to find a node by path in the tree</param>
    /// <returns>Restored session state</returns>
    Task<SessionState> RestoreSessionStateAsync(
        Func<string, Task> onProjectLoad,
        Action<FileTreeNode> onPinFile,
        Func<FileTreeNode?, string, FileTreeNode?> findNodeByPath);
}
