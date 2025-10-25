using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AIContextPacker.Models;
using AIContextPacker.Services.Interfaces;

namespace AIContextPacker.Services;

public class PartGeneratorService
{
    private readonly IFileSystemService _fileSystemService;
    private readonly INotificationService _notificationService;

    public PartGeneratorService(
        IFileSystemService fileSystemService,
        INotificationService notificationService)
    {
        _fileSystemService = fileSystemService;
        _notificationService = notificationService;
    }

    public async Task<List<GeneratedPart>> GeneratePartsAsync(
        List<string> pinnedFiles,
        List<string> selectedFiles,
        string basePath,
        int maxCharsLimit,
        bool includeHeaders,
        string? globalPrompt = null)
    {
        var parts = new List<GeneratedPart>();
        var allFiles = new List<(string path, bool isPinned)>();

        // Add pinned files first
        foreach (var file in pinnedFiles)
        {
            allFiles.Add((file, true));
        }

        // Add selected files
        foreach (var file in selectedFiles.Where(f => !pinnedFiles.Contains(f)))
        {
            allFiles.Add((file, false));
        }

        // Validate: check if any single file exceeds the limit
        foreach (var (filePath, _) in allFiles)
        {
            var content = await _fileSystemService.ReadFileContentAsync(filePath);
            var header = includeHeaders ? GenerateFileHeader(filePath, basePath) : string.Empty;
            var totalChars = header.Length + content.Length;

            if (totalChars > maxCharsLimit)
            {
                var relativePath = _fileSystemService.GetRelativePath(basePath, filePath);
                _notificationService.ShowError(
                    $"File '{relativePath}' exceeds the maximum character limit.\n" +
                    $"File size: {totalChars:N0} chars\n" +
                    $"Limit: {maxCharsLimit:N0} chars\n\n" +
                    $"Please increase the limit or exclude this file.");
                return parts;
            }
        }

        // Start generating parts
        var currentPart = new StringBuilder();
        var currentCharCount = 0;
        var partNumber = 1;
        var isFirstPart = true;

        // Add global prompt to first part if provided
        if (!string.IsNullOrWhiteSpace(globalPrompt) && isFirstPart)
        {
            currentPart.AppendLine(globalPrompt);
            currentPart.AppendLine();
            currentCharCount = currentPart.Length;
        }

        var processingPinned = true;

        foreach (var (filePath, isPinned) in allFiles)
        {
            // If we're transitioning from pinned to regular files, finalize pinned part
            if (processingPinned && !isPinned && currentCharCount > 0)
            {
                parts.Add(new GeneratedPart
                {
                    PartNumber = partNumber++,
                    Content = currentPart.ToString(),
                    CharacterCount = currentCharCount,
                    MaxChars = maxCharsLimit
                });

                currentPart.Clear();
                currentCharCount = 0;
                isFirstPart = false;
                processingPinned = false;
            }

            var content = await _fileSystemService.ReadFileContentAsync(filePath);
            var header = includeHeaders ? GenerateFileHeader(filePath, basePath) : string.Empty;
            var fileContent = header + content + Environment.NewLine + Environment.NewLine;
            var fileCharCount = fileContent.Length;

            // Check if adding this file would exceed the limit
            if (currentCharCount + fileCharCount > maxCharsLimit && currentCharCount > 0)
            {
                // Save current part
                parts.Add(new GeneratedPart
                {
                    PartNumber = partNumber++,
                    Content = currentPart.ToString(),
                    CharacterCount = currentCharCount,
                    MaxChars = maxCharsLimit
                });

                currentPart.Clear();
                currentCharCount = 0;
                isFirstPart = false;
            }

            currentPart.Append(fileContent);
            currentCharCount += fileCharCount;
        }

        // Add the last part if there's content
        if (currentCharCount > 0)
        {
            parts.Add(new GeneratedPart
            {
                PartNumber = partNumber,
                Content = currentPart.ToString(),
                CharacterCount = currentCharCount,
                MaxChars = maxCharsLimit
            });
        }

        return parts;
    }

    public string GenerateStructure(FileTreeNode rootNode, List<string> selectedFiles, List<string> pinnedFiles)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Project Structure:");
        sb.AppendLine("==================");
        sb.AppendLine();

        BuildStructureRecursive(rootNode, sb, "", selectedFiles, pinnedFiles, true);

        return sb.ToString();
    }

    private void BuildStructureRecursive(
        FileTreeNode node,
        StringBuilder sb,
        string indent,
        List<string> selectedFiles,
        List<string> pinnedFiles,
        bool isLast)
    {
        if (!node.IsVisible)
            return;

        var prefix = isLast ? "└── " : "├── ";
        var marker = "";

        if (!node.IsDirectory)
        {
            if (pinnedFiles.Contains(node.FullPath))
                marker = " 📌";
            else if (selectedFiles.Contains(node.FullPath))
                marker = " ✓";
        }

        sb.AppendLine($"{indent}{prefix}{node.Name}{marker}");

        if (node.IsDirectory && node.Children.Any())
        {
            var newIndent = indent + (isLast ? "    " : "│   ");
            var visibleChildren = node.Children.Where(c => c.IsVisible).ToList();

            for (int i = 0; i < visibleChildren.Count; i++)
            {
                BuildStructureRecursive(
                    visibleChildren[i],
                    sb,
                    newIndent,
                    selectedFiles,
                    pinnedFiles,
                    i == visibleChildren.Count - 1);
            }
        }
    }

    private string GenerateFileHeader(string filePath, string basePath)
    {
        var relativePath = Path.GetRelativePath(basePath, filePath);
        return $"// File: {relativePath}{Environment.NewLine}{Environment.NewLine}";
    }
}
