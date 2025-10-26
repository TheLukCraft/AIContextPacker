using AIContextPacker.Models;
using AIContextPacker.Services;
using AIContextPacker.Services.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace AIContextPacker.Tests.Services;

public class SessionStateServiceTests
{
    private readonly Mock<ILogger<SessionStateService>> _mockLogger;
    private readonly Mock<ISettingsService> _mockSettingsService;
    private readonly Mock<IPinService> _mockPinService;
    private readonly Mock<IFileSelectionService> _mockFileSelectionService;
    private readonly SessionStateService _service;

    public SessionStateServiceTests()
    {
        _mockLogger = new Mock<ILogger<SessionStateService>>();
        _mockSettingsService = new Mock<ISettingsService>();
        _mockPinService = new Mock<IPinService>();
        _mockFileSelectionService = new Mock<IFileSelectionService>();

        _service = new SessionStateService(
            _mockLogger.Object,
            _mockSettingsService.Object,
            _mockPinService.Object,
            _mockFileSelectionService.Object);
    }

    [Fact]
    public async Task SaveSessionStateAsync_WithValidData_SavesSessionState()
    {
        // Arrange
        var projectPath = "C:\\TestProject";
        var rootNode = new FileTreeNode { FullPath = projectPath, Name = "TestProject", IsDirectory = true };
        var selectedGlobalPromptId = "prompt-1";
        var useDetectedGitignore = true;

        _mockPinService.Setup(x => x.GetPinnedFilePaths())
            .Returns(new List<string> { "file1.cs", "file2.cs" });
        
        _mockFileSelectionService.Setup(x => x.GetSelectedFilePaths(rootNode))
            .Returns(new List<string> { "selected1.cs", "selected2.cs" });

        _mockSettingsService.Setup(x => x.SaveSessionStateAsync(It.IsAny<SessionState>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.SaveSessionStateAsync(projectPath, rootNode, selectedGlobalPromptId, useDetectedGitignore);

        // Assert
        _mockSettingsService.Verify(x => x.SaveSessionStateAsync(It.Is<SessionState>(s =>
            s.LastProjectPath == projectPath &&
            s.PinnedFiles.Count == 2 &&
            s.SelectedFiles.Count == 2 &&
            s.UseDetectedGitignore == true &&
            s.SelectedGlobalPrompt == selectedGlobalPromptId
        )), Times.Once);
    }

    [Fact]
    public async Task SaveSessionStateAsync_WithNullRootNode_SavesEmptySelectedFiles()
    {
        // Arrange
        var projectPath = "C:\\TestProject";
        _mockPinService.Setup(x => x.GetPinnedFilePaths()).Returns(new List<string>());
        _mockSettingsService.Setup(x => x.SaveSessionStateAsync(It.IsAny<SessionState>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.SaveSessionStateAsync(projectPath, null, null, false);

        // Assert
        _mockSettingsService.Verify(x => x.SaveSessionStateAsync(It.Is<SessionState>(s =>
            s.SelectedFiles.Count == 0
        )), Times.Once);
    }

    [Fact]
    public async Task SaveSessionStateAsync_WithNullProjectPath_SavesEmptyString()
    {
        // Arrange
        _mockPinService.Setup(x => x.GetPinnedFilePaths()).Returns(new List<string>());
        _mockSettingsService.Setup(x => x.SaveSessionStateAsync(It.IsAny<SessionState>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.SaveSessionStateAsync(null!, null, null, false);

        // Assert
        _mockSettingsService.Verify(x => x.SaveSessionStateAsync(It.Is<SessionState>(s =>
            s.LastProjectPath == string.Empty
        )), Times.Once);
    }

    [Fact]
    public async Task SaveSessionStateAsync_WithNullGlobalPromptId_SavesEmptyString()
    {
        // Arrange
        _mockPinService.Setup(x => x.GetPinnedFilePaths()).Returns(new List<string>());
        _mockSettingsService.Setup(x => x.SaveSessionStateAsync(It.IsAny<SessionState>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.SaveSessionStateAsync("C:\\Test", null, null, false);

        // Assert
        _mockSettingsService.Verify(x => x.SaveSessionStateAsync(It.Is<SessionState>(s =>
            s.SelectedGlobalPrompt == string.Empty
        )), Times.Once);
    }

    [Fact]
    public async Task RestoreSessionStateAsync_WithValidSession_RestoresProjectAndPins()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var sessionState = new SessionState
            {
                LastProjectPath = tempDir,
                PinnedFiles = new List<string> { "file1.cs", "file2.cs" },
                SelectedFiles = new List<string> { "selected1.cs" },
                UseDetectedGitignore = true,
                SelectedGlobalPrompt = "prompt-1"
            };

            _mockSettingsService.Setup(x => x.LoadSessionStateAsync())
                .ReturnsAsync(sessionState);

            var projectLoadCalled = false;
            Func<string, Task> onProjectLoad = async (path) =>
            {
                projectLoadCalled = true;
                path.Should().Be(tempDir);
                await Task.CompletedTask;
            };

            var pinnedFiles = new List<string>();
            Action<FileTreeNode> onPinFile = (node) => pinnedFiles.Add(node.FullPath);

            var node1 = new FileTreeNode { FullPath = "file1.cs", Name = "file1.cs", IsDirectory = false };
            var node2 = new FileTreeNode { FullPath = "file2.cs", Name = "file2.cs", IsDirectory = false };

            Func<FileTreeNode?, string, FileTreeNode?> findNodeByPath = (root, path) =>
            {
                return path switch
                {
                    "file1.cs" => node1,
                    "file2.cs" => node2,
                    _ => null
                };
            };

            // Act
            var result = await _service.RestoreSessionStateAsync(onProjectLoad, onPinFile, findNodeByPath);

            // Assert
            projectLoadCalled.Should().BeTrue();
            pinnedFiles.Should().HaveCount(2);
            pinnedFiles.Should().Contain("file1.cs");
            pinnedFiles.Should().Contain("file2.cs");
            result.Should().Be(sessionState);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir);
            }
        }
    }

    [Fact]
    public async Task RestoreSessionStateAsync_WithNonExistentProject_DoesNotRestoreProject()
    {
        // Arrange
        var sessionState = new SessionState
        {
            LastProjectPath = "C:\\NonExistentProject",
            PinnedFiles = new List<string>()
        };

        _mockSettingsService.Setup(x => x.LoadSessionStateAsync())
            .ReturnsAsync(sessionState);

        var projectLoadCalled = false;
        Func<string, Task> onProjectLoad = async (path) =>
        {
            projectLoadCalled = true;
            await Task.CompletedTask;
        };

        Action<FileTreeNode> onPinFile = (node) => { };
        Func<FileTreeNode?, string, FileTreeNode?> findNodeByPath = (root, path) => null;

        // Act
        var result = await _service.RestoreSessionStateAsync(onProjectLoad, onPinFile, findNodeByPath);

        // Assert
        projectLoadCalled.Should().BeFalse();
        result.Should().Be(sessionState);
    }

    [Fact]
    public async Task RestoreSessionStateAsync_WithEmptyProjectPath_DoesNotRestoreProject()
    {
        // Arrange
        var sessionState = new SessionState
        {
            LastProjectPath = string.Empty,
            PinnedFiles = new List<string>()
        };

        _mockSettingsService.Setup(x => x.LoadSessionStateAsync())
            .ReturnsAsync(sessionState);

        var projectLoadCalled = false;
        Func<string, Task> onProjectLoad = async (path) =>
        {
            projectLoadCalled = true;
            await Task.CompletedTask;
        };

        Action<FileTreeNode> onPinFile = (node) => { };
        Func<FileTreeNode?, string, FileTreeNode?> findNodeByPath = (root, path) => null;

        // Act
        await _service.RestoreSessionStateAsync(onProjectLoad, onPinFile, findNodeByPath);

        // Assert
        projectLoadCalled.Should().BeFalse();
    }

    [Fact]
    public async Task RestoreSessionStateAsync_WithPinnedFileNotFound_SkipsFile()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var sessionState = new SessionState
            {
                LastProjectPath = tempDir,
                PinnedFiles = new List<string> { "file1.cs", "nonexistent.cs" }
            };

            _mockSettingsService.Setup(x => x.LoadSessionStateAsync())
                .ReturnsAsync(sessionState);

            Func<string, Task> onProjectLoad = async (path) => await Task.CompletedTask;

            var pinnedFiles = new List<string>();
            Action<FileTreeNode> onPinFile = (node) => pinnedFiles.Add(node.FullPath);

            var node1 = new FileTreeNode { FullPath = "file1.cs", Name = "file1.cs", IsDirectory = false };

            Func<FileTreeNode?, string, FileTreeNode?> findNodeByPath = (root, path) =>
            {
                return path == "file1.cs" ? node1 : null;
            };

            // Act
            await _service.RestoreSessionStateAsync(onProjectLoad, onPinFile, findNodeByPath);

            // Assert
            pinnedFiles.Should().HaveCount(1);
            pinnedFiles.Should().Contain("file1.cs");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir);
            }
        }
    }

    [Fact]
    public async Task RestoreSessionStateAsync_WithNullOnProjectLoad_ThrowsArgumentNullException()
    {
        // Arrange
        Action<FileTreeNode> onPinFile = (node) => { };
        Func<FileTreeNode?, string, FileTreeNode?> findNodeByPath = (root, path) => null;

        // Act
        var act = async () => await _service.RestoreSessionStateAsync(null!, onPinFile, findNodeByPath);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task RestoreSessionStateAsync_WithNullOnPinFile_ThrowsArgumentNullException()
    {
        // Arrange
        Func<string, Task> onProjectLoad = async (path) => await Task.CompletedTask;
        Func<FileTreeNode?, string, FileTreeNode?> findNodeByPath = (root, path) => null;

        // Act
        var act = async () => await _service.RestoreSessionStateAsync(onProjectLoad, null!, findNodeByPath);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task RestoreSessionStateAsync_WithNullFindNodeByPath_ThrowsArgumentNullException()
    {
        // Arrange
        Func<string, Task> onProjectLoad = async (path) => await Task.CompletedTask;
        Action<FileTreeNode> onPinFile = (node) => { };

        // Act
        var act = async () => await _service.RestoreSessionStateAsync(onProjectLoad, onPinFile, null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task RestoreSessionStateAsync_ReturnsLoadedSessionState()
    {
        // Arrange
        var sessionState = new SessionState
        {
            LastProjectPath = string.Empty,
            UseDetectedGitignore = false,
            SelectedGlobalPrompt = "test-prompt"
        };

        _mockSettingsService.Setup(x => x.LoadSessionStateAsync())
            .ReturnsAsync(sessionState);

        Func<string, Task> onProjectLoad = async (path) => await Task.CompletedTask;
        Action<FileTreeNode> onPinFile = (node) => { };
        Func<FileTreeNode?, string, FileTreeNode?> findNodeByPath = (root, path) => null;

        // Act
        var result = await _service.RestoreSessionStateAsync(onProjectLoad, onPinFile, findNodeByPath);

        // Assert
        result.Should().NotBeNull();
        result.UseDetectedGitignore.Should().BeFalse();
        result.SelectedGlobalPrompt.Should().Be("test-prompt");
    }
}
