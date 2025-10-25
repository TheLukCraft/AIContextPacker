using System.Collections.Generic;
using System.Threading.Tasks;
using AIContextPacker.Models;

namespace AIContextPacker.Services.Interfaces;

public interface IFileSystemService
{
    Task<ProjectStructure> LoadProjectAsync(string folderPath);
    Task<List<string>> ReadGitignoreAsync(string filePath);
    Task<string> ReadFileContentAsync(string filePath);
    bool FileExists(string path);
    bool DirectoryExists(string path);
    IEnumerable<string> GetFiles(string path, string searchPattern, bool recursive);
    IEnumerable<string> GetDirectories(string path);
    string GetRelativePath(string basePath, string fullPath);
    long GetFileSize(string filePath);
}
