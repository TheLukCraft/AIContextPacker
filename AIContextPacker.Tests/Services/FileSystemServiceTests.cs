using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AIContextPacker.Exceptions;
using AIContextPacker.Services;
using FluentAssertions;
using Xunit;

namespace AIContextPacker.Tests.Services;

/// <summary>
/// Unit tests for <see cref="FileSystemService"/>.
/// </summary>
public class FileSystemServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly FileSystemService _sut; // System Under Test

    public FileSystemServiceTests()
    {
        // Create a temporary test directory
        _testDirectory = Path.Combine(Path.GetTempPath(), $"AIContextPacker_Tests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        
        _sut = new FileSystemService();
    }

    public void Dispose()
    {
        // Cleanup test directory
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public async Task LoadProjectAsync_WithValidDirectory_ReturnsProjectStructure()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "test.cs");
        await File.WriteAllTextAsync(testFile, "// Test content");

        // Act
        var result = await _sut.LoadProjectAsync(_testDirectory);

        // Assert
        result.Should().NotBeNull();
        result.RootPath.Should().Be(_testDirectory);
        result.RootNode.Should().NotBeNull();
        result.RootNode.IsDirectory.Should().BeTrue();
        result.RootNode.Children.Should().ContainSingle();
    }

    [Fact]
    public async Task LoadProjectAsync_WithNonExistentDirectory_ThrowsProjectLoadException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "does-not-exist");

        // Act
        Func<Task> act = async () => await _sut.LoadProjectAsync(nonExistentPath);

        // Assert
        await act.Should().ThrowAsync<ProjectLoadException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task ReadGitignoreAsync_WithValidFile_ReturnsPatterns()
    {
        // Arrange
        var gitignorePath = Path.Combine(_testDirectory, ".gitignore");
        var content = @"# Comment
*.log
bin/
# Another comment
obj/";
        await File.WriteAllTextAsync(gitignorePath, content);

        // Act
        var result = await _sut.ReadGitignoreAsync(gitignorePath);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain("*.log");
        result.Should().Contain("bin/");
        result.Should().Contain("obj/");
    }

    [Fact]
    public async Task ReadGitignoreAsync_WithNonExistentFile_ReturnsEmptyList()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, ".gitignore");

        // Act
        var result = await _sut.ReadGitignoreAsync(nonExistentPath);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ReadFileContentAsync_WithValidFile_ReturnsContent()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "test.txt");
        var expectedContent = "Test content";
        await File.WriteAllTextAsync(testFile, expectedContent);

        // Act
        var result = await _sut.ReadFileContentAsync(testFile);

        // Assert
        result.Should().Be(expectedContent);
    }

    [Fact]
    public async Task ReadFileContentAsync_WithNonExistentFile_ThrowsFileSystemException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "does-not-exist.txt");

        // Act
        Func<Task> act = async () => await _sut.ReadFileContentAsync(nonExistentPath);

        // Assert
        await act.Should().ThrowAsync<FileSystemException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public void FileExists_WithExistingFile_ReturnsTrue()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "test.txt");
        File.WriteAllText(testFile, "test");

        // Act
        var result = _sut.FileExists(testFile);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void FileExists_WithNonExistentFile_ReturnsFalse()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "does-not-exist.txt");

        // Act
        var result = _sut.FileExists(nonExistentPath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetRelativePath_WithValidPaths_ReturnsRelativePath()
    {
        // Arrange
        var basePath = @"C:\Projects\MyApp";
        var fullPath = @"C:\Projects\MyApp\src\Services\MyService.cs";

        // Act
        var result = _sut.GetRelativePath(basePath, fullPath);

        // Assert
        result.Should().Be(@"src\Services\MyService.cs");
    }

    [Fact]
    public void GetFileSize_WithValidFile_ReturnsCorrectSize()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "test.txt");
        var content = "12345"; // 5 bytes
        File.WriteAllText(testFile, content);

        // Act
        var result = _sut.GetFileSize(testFile);

        // Assert
        result.Should().Be(5);
    }
}
