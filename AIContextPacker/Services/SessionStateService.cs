using AIContextPacker.Models;
using AIContextPacker.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.IO;

namespace AIContextPacker.Services;

/// <summary>
/// Service for managing application session state.
/// </summary>
public class SessionStateService : ISessionStateService
{
    private readonly ILogger<SessionStateService> _logger;
    private readonly ISettingsService _settingsService;
    private readonly IPinService _pinService;
    private readonly IFileSelectionService _fileSelectionService;

    public SessionStateService(
        ILogger<SessionStateService> logger,
        ISettingsService settingsService,
        IPinService pinService,
        IFileSelectionService fileSelectionService)
    {
        _logger = logger;
        _settingsService = settingsService;
        _pinService = pinService;
        _fileSelectionService = fileSelectionService;
    }

    public async Task SaveSessionStateAsync(
        string currentProjectPath,
        FileTreeNode? rootNode,
        string? selectedGlobalPromptId,
        bool useDetectedGitignore)
    {
        _logger.LogInformation("Saving session state");

        var sessionState = new SessionState
        {
            LastProjectPath = currentProjectPath ?? string.Empty,
            PinnedFiles = _pinService.GetPinnedFilePaths().ToList(),
            SelectedFiles = rootNode != null 
                ? _fileSelectionService.GetSelectedFilePaths(rootNode).ToList() 
                : new List<string>(),
            UseDetectedGitignore = useDetectedGitignore,
            SelectedGlobalPrompt = selectedGlobalPromptId ?? string.Empty
        };

        await _settingsService.SaveSessionStateAsync(sessionState);

        _logger.LogInformation(
            "Session state saved: Project={ProjectPath}, PinnedFiles={PinnedCount}, SelectedFiles={SelectedCount}",
            sessionState.LastProjectPath,
            sessionState.PinnedFiles.Count,
            sessionState.SelectedFiles.Count);
    }

    public async Task<SessionState> RestoreSessionStateAsync(
        Func<string, Task> onProjectLoad,
        Action<FileTreeNode> onPinFile,
        Func<FileTreeNode?, string, FileTreeNode?> findNodeByPath)
    {
        ArgumentNullException.ThrowIfNull(onProjectLoad);
        ArgumentNullException.ThrowIfNull(onPinFile);
        ArgumentNullException.ThrowIfNull(findNodeByPath);

        _logger.LogInformation("Restoring session state");

        var sessionState = await _settingsService.LoadSessionStateAsync();

        _logger.LogInformation(
            "Session state loaded: Project={ProjectPath}, PinnedFiles={PinnedCount}, SelectedFiles={SelectedCount}",
            sessionState.LastProjectPath,
            sessionState.PinnedFiles.Count,
            sessionState.SelectedFiles.Count);

        // Restore last project if it exists
        if (!string.IsNullOrEmpty(sessionState.LastProjectPath) &&
            Directory.Exists(sessionState.LastProjectPath))
        {
            _logger.LogInformation("Restoring last project: {ProjectPath}", sessionState.LastProjectPath);
            
            await onProjectLoad(sessionState.LastProjectPath);

            // Restore pinned files after project is loaded
            // Note: RootNode must be set by the caller before pinning
            foreach (var pinnedPath in sessionState.PinnedFiles)
            {
                var node = findNodeByPath(null, pinnedPath); // null will be replaced by RootNode in caller
                if (node != null)
                {
                    onPinFile(node);
                    _logger.LogDebug("Restored pinned file: {FilePath}", pinnedPath);
                }
                else
                {
                    _logger.LogWarning("Could not restore pinned file (not found): {FilePath}", pinnedPath);
                }
            }

            _logger.LogInformation("Restored {PinnedCount} pinned files", sessionState.PinnedFiles.Count);
        }
        else
        {
            _logger.LogInformation("No previous project to restore");
        }

        return sessionState;
    }
}
