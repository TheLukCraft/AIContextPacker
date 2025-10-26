using System.Linq;
using AIContextPacker.Models;
using AIContextPacker.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AIContextPacker.Tests.Services;

public class FileSelectionServiceTests
{
    private readonly Mock<ILogger<FileSelectionService>> _loggerMock;
    private readonly FileSelectionService _service;

    public FileSelectionServiceTests()
    {
        _loggerMock = new Mock<ILogger<FileSelectionService>>();
        _service = new FileSelectionService(_loggerMock.Object);
    }

    #region SelectAll Tests

    [Fact]
    public void SelectAll_WithValidTree_SelectsAllVisibleNonPinnedFiles()
    {
        // Arrange
        var folder = new FileTreeNode
        {
            Name = "Folder",
            FullPath = "C:\\Project\\Folder",
            IsDirectory = true,
            IsVisible = true,
            Children =
            {
                new FileTreeNode
                {
                    Name = "file1.cs",
                    FullPath = "C:\\Project\\Folder\\file1.cs",
                    IsDirectory = false,
                    IsVisible = true,
                    IsPinned = false,
                    IsSelected = false
                },
                new FileTreeNode
                {
                    Name = "file2.cs",
                    FullPath = "C:\\Project\\Folder\\file2.cs",
                    IsDirectory = false,
                    IsVisible = true,
                    IsPinned = false,
                    IsSelected = false
                },
                new FileTreeNode
                {
                    Name = "file3.cs",
                    FullPath = "C:\\Project\\Folder\\file3.cs",
                    IsDirectory = false,
                    IsVisible = true,
                    IsPinned = true, // Pinned - should NOT be selected
                    IsSelected = false
                },
                new FileTreeNode
                {
                    Name = "file4.cs",
                    FullPath = "C:\\Project\\Folder\\file4.cs",
                    IsDirectory = false,
                    IsVisible = false, // Not visible - should NOT be selected
                    IsPinned = false,
                    IsSelected = false
                }
            }
        };

        var rootNode = new FileTreeNode
        {
            Name = "Root",
            FullPath = "C:\\Project",
            IsDirectory = true,
            IsVisible = true,
            Children = { folder }
        };

        // Act
        _service.SelectAll(rootNode);

        // Assert
        folder.Children[0].IsSelected.Should().BeTrue("file1 is visible and not pinned");
        folder.Children[1].IsSelected.Should().BeTrue("file2 is visible and not pinned");
        folder.Children[2].IsSelected.Should().BeFalse("file3 is pinned");
        folder.Children[3].IsSelected.Should().BeFalse("file4 is not visible");
    }

    [Fact]
    public void SelectAll_WithNestedStructure_SelectsAllVisibleNonPinnedFilesRecursively()
    {
        // Arrange
        var rootNode = new FileTreeNode
        {
            Name = "Root",
            FullPath = "C:\\Project",
            IsDirectory = true,
            IsVisible = true,
            Children =
            {
                new FileTreeNode
                {
                    Name = "Folder1",
                    FullPath = "C:\\Project\\Folder1",
                    IsDirectory = true,
                    IsVisible = true,
                    Children =
                    {
                        new FileTreeNode
                        {
                            Name = "nested.cs",
                            FullPath = "C:\\Project\\Folder1\\nested.cs",
                            IsDirectory = false,
                            IsVisible = true,
                            IsPinned = false,
                            IsSelected = false
                        }
                    }
                }
            }
        };

        // Act
        _service.SelectAll(rootNode);

        // Assert
        rootNode.Children[0].Children[0].IsSelected.Should().BeTrue("nested file should be selected");
    }

    [Fact]
    public void SelectAll_WithNullRootNode_DoesNotThrow()
    {
        // Act
        var act = () => _service.SelectAll(null!);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region DeselectAll Tests

    [Fact]
    public void DeselectAll_WithSelectedFiles_DeselectsAllVisibleNonPinnedFiles()
    {
        // Arrange
        var rootNode = new FileTreeNode
        {
            Name = "Root",
            FullPath = "C:\\Project",
            IsDirectory = true,
            IsVisible = true,
            Children =
            {
                new FileTreeNode
                {
                    Name = "file1.cs",
                    FullPath = "C:\\Project\\file1.cs",
                    IsDirectory = false,
                    IsVisible = true,
                    IsPinned = false,
                    IsSelected = true
                },
                new FileTreeNode
                {
                    Name = "file2.cs",
                    FullPath = "C:\\Project\\file2.cs",
                    IsDirectory = false,
                    IsVisible = true,
                    IsPinned = true, // Pinned - should remain selected
                    IsSelected = true
                },
                new FileTreeNode
                {
                    Name = "file3.cs",
                    FullPath = "C:\\Project\\file3.cs",
                    IsDirectory = false,
                    IsVisible = false, // Not visible - should remain selected
                    IsPinned = false,
                    IsSelected = true
                }
            }
        };

        // Act
        _service.DeselectAll(rootNode);

        // Assert
        rootNode.Children[0].IsSelected.Should().BeFalse("file1 should be deselected");
        rootNode.Children[1].IsSelected.Should().BeTrue("file2 is pinned, should remain selected");
        rootNode.Children[2].IsSelected.Should().BeTrue("file3 is not visible, should remain selected");
    }

    [Fact]
    public void DeselectAll_WithNullRootNode_DoesNotThrow()
    {
        // Act
        var act = () => _service.DeselectAll(null!);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region GetSelectedFilePaths Tests

    [Fact]
    public void GetSelectedFilePaths_WithSelectedFiles_ReturnsOnlySelectedVisibleFiles()
    {
        // Arrange
        var rootNode = new FileTreeNode
        {
            Name = "Root",
            FullPath = "C:\\Project",
            IsDirectory = true,
            Children =
            {
                new FileTreeNode
                {
                    Name = "selected.cs",
                    FullPath = "C:\\Project\\selected.cs",
                    IsDirectory = false,
                    IsSelected = true,
                    IsVisible = true
                },
                new FileTreeNode
                {
                    Name = "notselected.cs",
                    FullPath = "C:\\Project\\notselected.cs",
                    IsDirectory = false,
                    IsSelected = false,
                    IsVisible = true
                },
                new FileTreeNode
                {
                    Name = "selectedbutinvisible.cs",
                    FullPath = "C:\\Project\\selectedbutinvisible.cs",
                    IsDirectory = false,
                    IsSelected = true,
                    IsVisible = false // Not visible - should not be returned
                }
            }
        };

        // Act
        var result = _service.GetSelectedFilePaths(rootNode).ToList();

        // Assert
        result.Should().ContainSingle();
        result[0].Should().Be("C:\\Project\\selected.cs");
    }

    [Fact]
    public void GetSelectedFilePaths_WithNestedStructure_ReturnsAllSelectedFilesRecursively()
    {
        // Arrange
        var rootNode = new FileTreeNode
        {
            Name = "Root",
            FullPath = "C:\\Project",
            IsDirectory = true,
            Children =
            {
                new FileTreeNode
                {
                    Name = "file1.cs",
                    FullPath = "C:\\Project\\file1.cs",
                    IsDirectory = false,
                    IsSelected = true,
                    IsVisible = true
                },
                new FileTreeNode
                {
                    Name = "Folder",
                    FullPath = "C:\\Project\\Folder",
                    IsDirectory = true,
                    Children =
                    {
                        new FileTreeNode
                        {
                            Name = "file2.cs",
                            FullPath = "C:\\Project\\Folder\\file2.cs",
                            IsDirectory = false,
                            IsSelected = true,
                            IsVisible = true
                        }
                    }
                }
            }
        };

        // Act
        var result = _service.GetSelectedFilePaths(rootNode).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain("C:\\Project\\file1.cs");
        result.Should().Contain("C:\\Project\\Folder\\file2.cs");
    }

    [Fact]
    public void GetSelectedFilePaths_WithNoSelectedFiles_ReturnsEmptyCollection()
    {
        // Arrange
        var rootNode = new FileTreeNode
        {
            Name = "Root",
            FullPath = "C:\\Project",
            IsDirectory = true,
            Children =
            {
                new FileTreeNode
                {
                    Name = "file1.cs",
                    FullPath = "C:\\Project\\file1.cs",
                    IsDirectory = false,
                    IsSelected = false,
                    IsVisible = true
                }
            }
        };

        // Act
        var result = _service.GetSelectedFilePaths(rootNode).ToList();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetSelectedFilePaths_WithNullRootNode_ReturnsEmptyCollection()
    {
        // Act
        var result = _service.GetSelectedFilePaths(null!).ToList();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetSelectedFilePaths_DoesNotIncludeDirectories_OnlyFiles()
    {
        // Arrange
        var rootNode = new FileTreeNode
        {
            Name = "Root",
            FullPath = "C:\\Project",
            IsDirectory = true,
            IsSelected = true, // Directory is selected
            IsVisible = true,
            Children =
            {
                new FileTreeNode
                {
                    Name = "file.cs",
                    FullPath = "C:\\Project\\file.cs",
                    IsDirectory = false,
                    IsSelected = true,
                    IsVisible = true
                }
            }
        };

        // Act
        var result = _service.GetSelectedFilePaths(rootNode).ToList();

        // Assert
        result.Should().ContainSingle();
        result[0].Should().Be("C:\\Project\\file.cs");
        result.Should().NotContain("C:\\Project"); // Directory should not be included
    }

    #endregion

    #region GetSelectedFileCount Tests

    [Fact]
    public void GetSelectedFileCount_WithSelectedFiles_ReturnsCorrectCount()
    {
        // Arrange
        var rootNode = new FileTreeNode
        {
            Name = "Root",
            FullPath = "C:\\Project",
            IsDirectory = true,
            Children =
            {
                new FileTreeNode
                {
                    Name = "file1.cs",
                    FullPath = "C:\\Project\\file1.cs",
                    IsDirectory = false,
                    IsSelected = true,
                    IsVisible = true
                },
                new FileTreeNode
                {
                    Name = "file2.cs",
                    FullPath = "C:\\Project\\file2.cs",
                    IsDirectory = false,
                    IsSelected = true,
                    IsVisible = true
                },
                new FileTreeNode
                {
                    Name = "file3.cs",
                    FullPath = "C:\\Project\\file3.cs",
                    IsDirectory = false,
                    IsSelected = false,
                    IsVisible = true
                }
            }
        };

        // Act
        var result = _service.GetSelectedFileCount(rootNode);

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public void GetSelectedFileCount_WithNullRootNode_ReturnsZero()
    {
        // Act
        var result = _service.GetSelectedFileCount(null!);

        // Assert
        result.Should().Be(0);
    }

    #endregion
}
