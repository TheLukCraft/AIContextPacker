using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AIContextPacker.Models;
using AIContextPacker.Services.Interfaces;

namespace AIContextPacker.Services;

public class FileSystemService : IFileSystemService
{
    public async Task<ProjectStructure> LoadProjectAsync(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            throw new DirectoryNotFoundException($"Directory not found: {folderPath}");

        var structure = new ProjectStructure
        {
            RootPath = folderPath,
            RootNode = await BuildTreeAsync(folderPath, null)
        };

        var gitignorePath = Path.Combine(folderPath, ".gitignore");
        structure.HasLocalGitignore = File.Exists(gitignorePath);
        structure.LocalGitignorePath = structure.HasLocalGitignore ? gitignorePath : string.Empty;

        return structure;
    }

    private async Task<FileTreeNode> BuildTreeAsync(string path, FileTreeNode? parent)
    {
        // Yield to keep UI responsive during large tree builds
        await Task.Yield();

        // Verify the path exists before creating a node
        if (!Directory.Exists(path) && !File.Exists(path))
        {
            throw new FileNotFoundException($"Path not found: {path}");
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
                        var childNode = await BuildTreeAsync(dir, node);
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
            catch (UnauthorizedAccessException)
            {
                // Skip directories without access
            }
            catch (DirectoryNotFoundException)
            {
                // Skip directories that disappeared during enumeration
            }
            catch (FileNotFoundException)
            {
                // Skip files that disappeared during enumeration
            }
        }
        else
        {
            node.FileSize = new FileInfo(path).Length;
        }

        return node;
    }

    public async Task<List<string>> ReadGitignoreAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return new List<string>();

        var lines = await File.ReadAllLinesAsync(filePath);
        return lines
            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith("#"))
            .Select(line => line.Trim())
            .ToList();
    }

    public async Task<string> ReadFileContentAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        return await File.ReadAllTextAsync(filePath);
    }

    public bool FileExists(string path) => File.Exists(path);

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public IEnumerable<string> GetFiles(string path, string searchPattern, bool recursive)
    {
        var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        return Directory.GetFiles(path, searchPattern, option);
    }

    public IEnumerable<string> GetDirectories(string path)
    {
        return Directory.GetDirectories(path);
    }

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

    public long GetFileSize(string filePath)
    {
        return new FileInfo(filePath).Length;
    }
}
