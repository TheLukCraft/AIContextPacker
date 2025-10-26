using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AIContextPacker.Exceptions;
using AIContextPacker.Models;
using AIContextPacker.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace AIContextPacker.Services;

/// <summary>
/// Provides file system operations for the application.
/// </summary>
public class FileSystemService : IFileSystemService
{
    private readonly ILogger<FileSystemService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public FileSystemService(ILogger<FileSystemService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Parameterless constructor for testing purposes.
    /// </summary>
    public FileSystemService() : this(Microsoft.Extensions.Logging.Abstractions.NullLogger<FileSystemService>.Instance)
    {
    }

    /// <summary>
    /// Loads a project from the specified folder path.
    /// </summary>
    /// <param name="folderPath">The folder path to load.</param>
    /// <returns>A <see cref="ProjectStructure"/> containing the project information.</returns>
    /// <exception cref="ProjectLoadException">Thrown when the project cannot be loaded.</exception>
    public async Task<ProjectStructure> LoadProjectAsync(string folderPath)
    {
        _logger.LogInformation("Loading project from path: {FolderPath}", folderPath);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (!Directory.Exists(folderPath))
            {
                _logger.LogError("Directory not found: {FolderPath}", folderPath);
                throw new ProjectLoadException($"Directory not found: {folderPath}", folderPath);
            }

            // Run tree building on background thread to avoid UI freeze
            var rootNode = await Task.Run(() => BuildTreeSync(folderPath, null));

            var structure = new ProjectStructure
            {
                RootPath = folderPath,
                RootNode = rootNode
            };

            var gitignorePath = Path.Combine(folderPath, ".gitignore");
            structure.HasLocalGitignore = File.Exists(gitignorePath);
            structure.LocalGitignorePath = structure.HasLocalGitignore ? gitignorePath : string.Empty;

            stopwatch.Stop();
            _logger.LogInformation(
                "Project loaded successfully from {FolderPath} in {ElapsedMs}ms. HasGitignore: {HasGitignore}", 
                folderPath, stopwatch.ElapsedMilliseconds, structure.HasLocalGitignore);

            return structure;
        }
        catch (Exception ex) when (ex is not ProjectLoadException)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to load project from {FolderPath} after {ElapsedMs}ms", 
                folderPath, stopwatch.ElapsedMilliseconds);
            throw new ProjectLoadException($"Failed to load project: {ex.Message}", folderPath, ex);
        }
    }

    private FileTreeNode BuildTreeSync(string path, FileTreeNode? parent)
    {
        // Verify the path exists before creating a node
        if (!Directory.Exists(path) && !File.Exists(path))
        {
            _logger.LogWarning("Path not found during tree building: {Path}", path);
            throw new FileSystemException($"Path not found: {path}", path);
        }

        var node = new FileTreeNode
        {
            Name = Path.GetFileName(path) ?? path,
            FullPath = path,
            IsDirectory = Directory.Exists(path),
            Parent = parent
        };

        if (node.IsDirectory)
        {
            try
            {
                var directories = Directory.GetDirectories(path).OrderBy(d => d);
                foreach (var dir in directories)
                {
                    // Only add directories that actually exist
                    if (Directory.Exists(dir))
                    {
                        var childNode = BuildTreeSync(dir, node);
                        node.Children.Add(childNode);
                    }
                }

                var files = Directory.GetFiles(path).OrderBy(f => f);
                foreach (var file in files)
                {
                    // Only add files that actually exist
                    if (File.Exists(file))
                    {
                        var fileNode = new FileTreeNode
                        {
                            Name = Path.GetFileName(file),
                            FullPath = file,
                            IsDirectory = false,
                            Parent = node,
                            FileSize = new FileInfo(file).Length
                        };
                        node.Children.Add(fileNode);
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                // Skip directories without access - common for system folders
                _logger.LogWarning(ex, "Access denied to directory: {Path}", path);
            }
            catch (DirectoryNotFoundException ex)
            {
                // Skip directories that disappeared during enumeration
                _logger.LogWarning(ex, "Directory not found during enumeration: {Path}", path);
            }
            catch (FileNotFoundException ex)
            {
                // Skip files that disappeared during enumeration
                _logger.LogWarning(ex, "File not found during enumeration: {Path}", path);
            }
        }
        else
        {
            try
            {
                node.FileSize = new FileInfo(path).Length;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not get file size for: {Path}", path);
                node.FileSize = 0;
            }
        }

        return node;
    }

    /// <summary>
    /// Reads gitignore patterns from a file.
    /// </summary>
    /// <param name="filePath">The path to the .gitignore file.</param>
    /// <returns>A list of gitignore patterns.</returns>
    public async Task<List<string>> ReadGitignoreAsync(string filePath)
    {
        _logger.LogDebug("Reading .gitignore from: {FilePath}", filePath);

        if (!File.Exists(filePath))
        {
            _logger.LogDebug(".gitignore file not found: {FilePath}", filePath);
            return new List<string>();
        }

        try
        {
            var lines = await File.ReadAllLinesAsync(filePath);
            var patterns = lines
                .Where(line => !string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith("#"))
                .Select(line => line.Trim())
                .ToList();

            _logger.LogInformation("Read {Count} patterns from .gitignore: {FilePath}", patterns.Count, filePath);
            return patterns;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read .gitignore file: {FilePath}", filePath);
            return new List<string>();
        }
    }

    /// <summary>
    /// Reads the content of a file.
    /// </summary>
    /// <param name="filePath">The file path to read.</param>
    /// <returns>The file content as a string.</returns>
    /// <exception cref="FileSystemException">Thrown when the file cannot be read.</exception>
    public async Task<string> ReadFileContentAsync(string filePath)
    {
        _logger.LogDebug("Reading file content: {FilePath}", filePath);

        if (!File.Exists(filePath))
        {
            _logger.LogError("File not found: {FilePath}", filePath);
            throw new FileSystemException($"File not found: {filePath}", filePath);
        }

        try
        {
            var content = await File.ReadAllTextAsync(filePath);
            _logger.LogDebug("Successfully read {Length} characters from: {FilePath}", content.Length, filePath);
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read file: {FilePath}", filePath);
            throw new FileSystemException($"Failed to read file: {ex.Message}", filePath, ex);
        }
    }

    /// <summary>
    /// Checks if a file exists.
    /// </summary>
    /// <param name="path">The file path to check.</param>
    /// <returns>True if the file exists; otherwise, false.</returns>
    public bool FileExists(string path) => File.Exists(path);

    /// <summary>
    /// Checks if a directory exists.
    /// </summary>
    /// <param name="path">The directory path to check.</param>
    /// <returns>True if the directory exists; otherwise, false.</returns>
    public bool DirectoryExists(string path) => Directory.Exists(path);

    /// <summary>
    /// Gets all files in a directory matching a search pattern.
    /// </summary>
    /// <param name="path">The directory path.</param>
    /// <param name="searchPattern">The search pattern (e.g., "*.cs").</param>
    /// <param name="recursive">Whether to search recursively.</param>
    /// <returns>An enumerable of file paths.</returns>
    public IEnumerable<string> GetFiles(string path, string searchPattern, bool recursive)
    {
        _logger.LogDebug("Getting files from {Path} with pattern {Pattern}, recursive: {Recursive}", 
            path, searchPattern, recursive);

        var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        return Directory.GetFiles(path, searchPattern, option);
    }

    /// <summary>
    /// Gets all subdirectories in a directory.
    /// </summary>
    /// <param name="path">The directory path.</param>
    /// <returns>An enumerable of directory paths.</returns>
    public IEnumerable<string> GetDirectories(string path)
    {
        return Directory.GetDirectories(path);
    }

    /// <summary>
    /// Gets the relative path from a base path to a full path.
    /// </summary>
    /// <param name="basePath">The base path.</param>
    /// <param name="fullPath">The full path.</param>
    /// <returns>The relative path.</returns>
    public string GetRelativePath(string basePath, string fullPath)
    {
        var baseUri = new Uri(basePath.EndsWith(Path.DirectorySeparatorChar.ToString()) 
            ? basePath 
            : basePath + Path.DirectorySeparatorChar);
        var fullUri = new Uri(fullPath);
        
        return Uri.UnescapeDataString(
            baseUri.MakeRelativeUri(fullUri)
                .ToString()
                .Replace('/', Path.DirectorySeparatorChar)
        );
    }

    /// <summary>
    /// Gets the size of a file in bytes.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <returns>The file size in bytes.</returns>
    public long GetFileSize(string filePath)
    {
        try
        {
            return new FileInfo(filePath).Length;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not get file size for: {FilePath}", filePath);
            return 0;
        }
    }
}
