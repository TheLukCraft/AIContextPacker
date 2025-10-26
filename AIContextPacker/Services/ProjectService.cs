using System;
using System.Diagnostics;
using System.Threading.Tasks;
using AIContextPacker.Exceptions;
using AIContextPacker.Models;
using AIContextPacker.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace AIContextPacker.Services;

/// <summary>
/// Manages project loading and state.
/// </summary>
public class ProjectService : IProjectService
{
    private readonly IFileSystemService _fileSystemService;
    private readonly ILogger<ProjectService> _logger;
    private ProjectStructure? _currentProject;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectService"/> class.
    /// </summary>
    /// <param name="fileSystemService">The file system service.</param>
    /// <param name="logger">The logger instance.</param>
    public ProjectService(
        IFileSystemService fileSystemService,
        ILogger<ProjectService> logger)
    {
        _fileSystemService = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public ProjectStructure? CurrentProject => _currentProject;

    /// <inheritdoc/>
    public string? CurrentProjectPath => _currentProject?.RootPath;

    /// <inheritdoc/>
    public bool IsProjectLoaded => _currentProject != null;

    /// <inheritdoc/>
    public async Task<ProjectStructure> LoadProjectAsync(string folderPath, IProgressReporter? progress = null)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            _logger.LogWarning("Attempted to load project with empty path");
            throw new ArgumentException("Folder path cannot be empty", nameof(folderPath));
        }

        _logger.LogInformation("Loading project from: {FolderPath}", folderPath);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            progress?.Report("Validating folder...", 10);

            if (!_fileSystemService.DirectoryExists(folderPath))
            {
                _logger.LogError("Directory not found: {FolderPath}", folderPath);
                throw new ProjectLoadException($"Directory does not exist: {folderPath}", folderPath);
            }

            progress?.Report("Loading project structure...", 30);

            var structure = await _fileSystemService.LoadProjectAsync(folderPath);
            
            progress?.Report("Reading .gitignore...", 60);

            if (structure.HasLocalGitignore)
            {
                _logger.LogInformation("Found .gitignore at: {GitignorePath}", structure.LocalGitignorePath);
            }

            progress?.Report("Finalizing...", 90);

            _currentProject = structure;

            stopwatch.Stop();
            _logger.LogInformation(
                "Project loaded successfully from {FolderPath} in {ElapsedMs}ms. Files/Folders: {NodeCount}",
                folderPath, stopwatch.ElapsedMilliseconds, CountNodes(structure.RootNode));

            progress?.Report("Project loaded successfully!", 100);
            progress?.Clear();

            return structure;
        }
        catch (Exception ex) when (ex is not ProjectLoadException and not ArgumentException)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to load project from {FolderPath} after {ElapsedMs}ms",
                folderPath, stopwatch.ElapsedMilliseconds);
            
            progress?.Clear();
            throw new ProjectLoadException($"Failed to load project: {ex.Message}", folderPath, ex);
        }
    }

    /// <inheritdoc/>
    public void UnloadProject()
    {
        if (_currentProject != null)
        {
            _logger.LogInformation("Unloading project: {ProjectPath}", _currentProject.RootPath);
            _currentProject = null;
        }
    }

    /// <inheritdoc/>
    public FileTreeNode? GetRootNode()
    {
        return _currentProject?.RootNode;
    }

    /// <summary>
    /// Counts the total number of nodes in the tree.
    /// </summary>
    private int CountNodes(FileTreeNode node)
    {
        var count = 1;
        foreach (var child in node.Children)
        {
            count += CountNodes(child);
        }
        return count;
    }
}
