using System;
using System.IO;
using System.Threading.Tasks;
using AIContextPacker.Exceptions;
using AIContextPacker.Models;
using AIContextPacker.Services;
using AIContextPacker.Services.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AIContextPacker.Tests.Services;

/// <summary>
/// Unit tests for <see cref="ProjectService"/>.
/// </summary>
public class ProjectServiceTests
{
    private readonly Mock<IFileSystemService> _fileSystemMock;
    private readonly Mock<ILogger<ProjectService>> _loggerMock;
    private readonly ProjectService _sut;

    public ProjectServiceTests()
    {
        _fileSystemMock = new Mock<IFileSystemService>();
        _loggerMock = new Mock<ILogger<ProjectService>>();
        _sut = new ProjectService(_fileSystemMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullFileSystemService_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new ProjectService(null!, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("fileSystemService");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new ProjectService(_fileSystemMock.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void CurrentProject_WhenNoProjectLoaded_ReturnsNull()
    {
        // Assert
        _sut.CurrentProject.Should().BeNull();
    }

    [Fact]
    public void CurrentProjectPath_WhenNoProjectLoaded_ReturnsNull()
    {
        // Assert
        _sut.CurrentProjectPath.Should().BeNull();
    }

    [Fact]
    public void IsProjectLoaded_WhenNoProjectLoaded_ReturnsFalse()
    {
        // Assert
        _sut.IsProjectLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task LoadProjectAsync_WithEmptyPath_ThrowsArgumentException()
    {
        // Act
        Func<Task> act = async () => await _sut.LoadProjectAsync("");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("folderPath");
    }

    [Fact]
    public async Task LoadProjectAsync_WithNonExistentDirectory_ThrowsProjectLoadException()
    {
        // Arrange
        var path = @"C:\NonExistent";
        _fileSystemMock.Setup(x => x.DirectoryExists(path)).Returns(false);

        // Act
        Func<Task> act = async () => await _sut.LoadProjectAsync(path);

        // Assert
        await act.Should().ThrowAsync<ProjectLoadException>()
            .WithMessage("*does not exist*");
    }

    [Fact]
    public async Task LoadProjectAsync_WithValidPath_LoadsProjectSuccessfully()
    {
        // Arrange
        var path = @"C:\TestProject";
        var expectedStructure = new ProjectStructure
        {
            RootPath = path,
            RootNode = new FileTreeNode { Name = "TestProject", IsDirectory = true },
            HasLocalGitignore = false
        };

        _fileSystemMock.Setup(x => x.DirectoryExists(path)).Returns(true);
        _fileSystemMock.Setup(x => x.LoadProjectAsync(path))
            .ReturnsAsync(expectedStructure);

        // Act
        var result = await _sut.LoadProjectAsync(path);

        // Assert
        result.Should().NotBeNull();
        result.RootPath.Should().Be(path);
        _sut.IsProjectLoaded.Should().BeTrue();
        _sut.CurrentProject.Should().Be(result);
        _sut.CurrentProjectPath.Should().Be(path);
    }

    [Fact]
    public async Task LoadProjectAsync_WithProgressReporter_ReportsProgress()
    {
        // Arrange
        var path = @"C:\TestProject";
        var structure = new ProjectStructure
        {
            RootPath = path,
            RootNode = new FileTreeNode { Name = "TestProject", IsDirectory = true },
            HasLocalGitignore = false
        };

        _fileSystemMock.Setup(x => x.DirectoryExists(path)).Returns(true);
        _fileSystemMock.Setup(x => x.LoadProjectAsync(path)).ReturnsAsync(structure);

        var progressCalls = new System.Collections.Generic.List<(string status, double? percent)>();
        var progressMock = new Mock<IProgressReporter>();
        progressMock.Setup(x => x.Report(It.IsAny<string>(), It.IsAny<double?>()))
            .Callback<string, double?>((status, percent) => progressCalls.Add((status, percent)));

        // Act
        await _sut.LoadProjectAsync(path, progressMock.Object);

        // Assert
        progressCalls.Should().NotBeEmpty();
        progressCalls.Should().Contain(x => x.status.Contains("Validating"));
        progressCalls.Should().Contain(x => x.status.Contains("Loading project structure"));
        progressMock.Verify(x => x.Clear(), Times.Once);
    }

    [Fact]
    public async Task LoadProjectAsync_WhenFileSystemServiceThrows_ThrowsProjectLoadException()
    {
        // Arrange
        var path = @"C:\TestProject";
        _fileSystemMock.Setup(x => x.DirectoryExists(path)).Returns(true);
        _fileSystemMock.Setup(x => x.LoadProjectAsync(path))
            .ThrowsAsync(new IOException("Disk error"));

        // Act
        Func<Task> act = async () => await _sut.LoadProjectAsync(path);

        // Assert
        var exception = await act.Should().ThrowAsync<ProjectLoadException>()
            .WithMessage("*Failed to load project*");
        exception.And.InnerException.Should().BeOfType<IOException>();
    }

    [Fact]
    public async Task UnloadProject_WithLoadedProject_ClearsCurrentProject()
    {
        // Arrange
        var path = @"C:\TestProject";
        var structure = new ProjectStructure
        {
            RootPath = path,
            RootNode = new FileTreeNode { Name = "TestProject", IsDirectory = true }
        };

        _fileSystemMock.Setup(x => x.DirectoryExists(path)).Returns(true);
        _fileSystemMock.Setup(x => x.LoadProjectAsync(path)).ReturnsAsync(structure);

        await _sut.LoadProjectAsync(path);

        // Act
        _sut.UnloadProject();

        // Assert
        _sut.IsProjectLoaded.Should().BeFalse();
        _sut.CurrentProject.Should().BeNull();
        _sut.CurrentProjectPath.Should().BeNull();
    }

    [Fact]
    public void UnloadProject_WithNoLoadedProject_DoesNotThrow()
    {
        // Act
        Action act = () => _sut.UnloadProject();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public async Task GetRootNode_WithLoadedProject_ReturnsRootNode()
    {
        // Arrange
        var path = @"C:\TestProject";
        var rootNode = new FileTreeNode { Name = "TestProject", IsDirectory = true };
        var structure = new ProjectStructure
        {
            RootPath = path,
            RootNode = rootNode
        };

        _fileSystemMock.Setup(x => x.DirectoryExists(path)).Returns(true);
        _fileSystemMock.Setup(x => x.LoadProjectAsync(path)).ReturnsAsync(structure);

        await _sut.LoadProjectAsync(path);

        // Act
        var result = _sut.GetRootNode();

        // Assert
        result.Should().Be(rootNode);
    }

    [Fact]
    public void GetRootNode_WithNoLoadedProject_ReturnsNull()
    {
        // Act
        var result = _sut.GetRootNode();

        // Assert
        result.Should().BeNull();
    }
}
