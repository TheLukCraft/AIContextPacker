using System.Linq;
using AIContextPacker.Models;
using AIContextPacker.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AIContextPacker.Tests.Services;

public class PinServiceTests
{
    private readonly Mock<ILogger<PinService>> _loggerMock;
    private readonly PinService _service;

    public PinServiceTests()
    {
        _loggerMock = new Mock<ILogger<PinService>>();
        _service = new PinService(_loggerMock.Object);
    }

    #region TogglePin Tests

    [Fact]
    public void TogglePin_WithUnpinnedFile_PinsTheFile()
    {
        // Arrange
        var file = new FileTreeNode
        {
            Name = "test.cs",
            FullPath = "C:\\Project\\test.cs",
            IsDirectory = false,
            IsPinned = false
        };

        // Act
        var result = _service.TogglePin(file);

        // Assert
        result.Should().BeTrue();
        file.IsPinned.Should().BeTrue();
        _service.PinnedFiles.Should().ContainSingle();
        _service.PinnedFiles.First().Should().Be(file);
    }

    [Fact]
    public void TogglePin_WithPinnedFile_UnpinsTheFile()
    {
        // Arrange
        var file = new FileTreeNode
        {
            Name = "test.cs",
            FullPath = "C:\\Project\\test.cs",
            IsDirectory = false,
            IsPinned = false
        };

        _service.Pin(file);

        // Act
        var result = _service.TogglePin(file);

        // Assert
        result.Should().BeTrue();
        file.IsPinned.Should().BeFalse();
        _service.PinnedFiles.Should().BeEmpty();
    }

    [Fact]
    public void TogglePin_WithDirectory_ReturnsFalseAndDoesNotPin()
    {
        // Arrange
        var directory = new FileTreeNode
        {
            Name = "Folder",
            FullPath = "C:\\Project\\Folder",
            IsDirectory = true,
            IsPinned = false
        };

        // Act
        var result = _service.TogglePin(directory);

        // Assert
        result.Should().BeFalse();
        directory.IsPinned.Should().BeFalse();
        _service.PinnedFiles.Should().BeEmpty();
    }

    [Fact]
    public void TogglePin_WithNull_ReturnsFalse()
    {
        // Act
        var result = _service.TogglePin(null!);

        // Assert
        result.Should().BeFalse();
        _service.PinnedFiles.Should().BeEmpty();
    }

    #endregion

    #region Pin Tests

    [Fact]
    public void Pin_WithValidFile_PinsFileAndDeselectsIt()
    {
        // Arrange
        var file = new FileTreeNode
        {
            Name = "test.cs",
            FullPath = "C:\\Project\\test.cs",
            IsDirectory = false,
            IsPinned = false,
            IsSelected = true
        };

        // Act
        var result = _service.Pin(file);

        // Assert
        result.Should().BeTrue();
        file.IsPinned.Should().BeTrue();
        file.IsSelected.Should().BeFalse("pinned files should be automatically deselected");
        _service.PinnedFiles.Should().ContainSingle();
        _service.PinnedFiles.First().Should().Be(file);
    }

    [Fact]
    public void Pin_WithAlreadyPinnedFile_ReturnsFalse()
    {
        // Arrange
        var file = new FileTreeNode
        {
            Name = "test.cs",
            FullPath = "C:\\Project\\test.cs",
            IsDirectory = false,
            IsPinned = false
        };

        _service.Pin(file);

        // Act
        var result = _service.Pin(file);

        // Assert
        result.Should().BeFalse("file is already pinned");
        _service.PinnedFiles.Should().ContainSingle();
    }

    [Fact]
    public void Pin_WithDirectory_ReturnsFalse()
    {
        // Arrange
        var directory = new FileTreeNode
        {
            Name = "Folder",
            FullPath = "C:\\Project\\Folder",
            IsDirectory = true
        };

        // Act
        var result = _service.Pin(directory);

        // Assert
        result.Should().BeFalse();
        directory.IsPinned.Should().BeFalse();
        _service.PinnedFiles.Should().BeEmpty();
    }

    [Fact]
    public void Pin_WithNull_ReturnsFalse()
    {
        // Act
        var result = _service.Pin(null!);

        // Assert
        result.Should().BeFalse();
        _service.PinnedFiles.Should().BeEmpty();
    }

    #endregion

    #region Unpin Tests

    [Fact]
    public void Unpin_WithPinnedFile_UnpinsFile()
    {
        // Arrange
        var file = new FileTreeNode
        {
            Name = "test.cs",
            FullPath = "C:\\Project\\test.cs",
            IsDirectory = false,
            IsPinned = false
        };

        _service.Pin(file);

        // Act
        var result = _service.Unpin(file);

        // Assert
        result.Should().BeTrue();
        file.IsPinned.Should().BeFalse();
        _service.PinnedFiles.Should().BeEmpty();
    }

    [Fact]
    public void Unpin_WithUnpinnedFile_ReturnsFalse()
    {
        // Arrange
        var file = new FileTreeNode
        {
            Name = "test.cs",
            FullPath = "C:\\Project\\test.cs",
            IsDirectory = false,
            IsPinned = false
        };

        // Act
        var result = _service.Unpin(file);

        // Assert
        result.Should().BeFalse("file is not pinned");
        _service.PinnedFiles.Should().BeEmpty();
    }

    [Fact]
    public void Unpin_WithNull_ReturnsFalse()
    {
        // Act
        var result = _service.Unpin(null!);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetPinnedFilePaths Tests

    [Fact]
    public void GetPinnedFilePaths_WithMultiplePinnedFiles_ReturnsAllPaths()
    {
        // Arrange
        var file1 = new FileTreeNode
        {
            Name = "file1.cs",
            FullPath = "C:\\Project\\file1.cs",
            IsDirectory = false
        };

        var file2 = new FileTreeNode
        {
            Name = "file2.cs",
            FullPath = "C:\\Project\\file2.cs",
            IsDirectory = false
        };

        _service.Pin(file1);
        _service.Pin(file2);

        // Act
        var result = _service.GetPinnedFilePaths();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain("C:\\Project\\file1.cs");
        result.Should().Contain("C:\\Project\\file2.cs");
    }

    [Fact]
    public void GetPinnedFilePaths_WithNoPinnedFiles_ReturnsEmptyList()
    {
        // Act
        var result = _service.GetPinnedFilePaths();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region ClearAll Tests

    [Fact]
    public void ClearAll_WithPinnedFiles_UnpinsAllFilesAndClearsList()
    {
        // Arrange
        var file1 = new FileTreeNode
        {
            Name = "file1.cs",
            FullPath = "C:\\Project\\file1.cs",
            IsDirectory = false
        };

        var file2 = new FileTreeNode
        {
            Name = "file2.cs",
            FullPath = "C:\\Project\\file2.cs",
            IsDirectory = false
        };

        _service.Pin(file1);
        _service.Pin(file2);

        // Act
        _service.ClearAll();

        // Assert
        _service.PinnedFiles.Should().BeEmpty();
        file1.IsPinned.Should().BeFalse();
        file2.IsPinned.Should().BeFalse();
    }

    [Fact]
    public void ClearAll_WithNoPinnedFiles_DoesNotThrow()
    {
        // Act
        var act = () => _service.ClearAll();

        // Assert
        act.Should().NotThrow();
        _service.PinnedFiles.Should().BeEmpty();
    }

    #endregion

    #region IsPinned Tests

    [Fact]
    public void IsPinned_WithPinnedFile_ReturnsTrue()
    {
        // Arrange
        var file = new FileTreeNode
        {
            Name = "test.cs",
            FullPath = "C:\\Project\\test.cs",
            IsDirectory = false
        };

        _service.Pin(file);

        // Act
        var result = _service.IsPinned(file);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsPinned_WithUnpinnedFile_ReturnsFalse()
    {
        // Arrange
        var file = new FileTreeNode
        {
            Name = "test.cs",
            FullPath = "C:\\Project\\test.cs",
            IsDirectory = false,
            IsPinned = false
        };

        // Act
        var result = _service.IsPinned(file);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsPinned_WithNull_ReturnsFalse()
    {
        // Act
        var result = _service.IsPinned(null!);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Multiple Operations Tests

    [Fact]
    public void PinMultipleFiles_ThenUnpinOne_MaintainsCorrectState()
    {
        // Arrange
        var file1 = new FileTreeNode
        {
            Name = "file1.cs",
            FullPath = "C:\\Project\\file1.cs",
            IsDirectory = false
        };

        var file2 = new FileTreeNode
        {
            Name = "file2.cs",
            FullPath = "C:\\Project\\file2.cs",
            IsDirectory = false
        };

        var file3 = new FileTreeNode
        {
            Name = "file3.cs",
            FullPath = "C:\\Project\\file3.cs",
            IsDirectory = false
        };

        // Act
        _service.Pin(file1);
        _service.Pin(file2);
        _service.Pin(file3);
        _service.Unpin(file2);

        // Assert
        _service.PinnedFiles.Should().HaveCount(2);
        _service.PinnedFiles.Should().Contain(file1);
        _service.PinnedFiles.Should().NotContain(file2);
        _service.PinnedFiles.Should().Contain(file3);

        file1.IsPinned.Should().BeTrue();
        file2.IsPinned.Should().BeFalse();
        file3.IsPinned.Should().BeTrue();
    }

    #endregion
}
