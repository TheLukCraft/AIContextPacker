using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
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

public class SearchServiceTests
{
    private readonly Mock<IFileSystemService> _mockFileSystem;
    private readonly Mock<ILogger<SearchService>> _mockLogger;
    private readonly SearchService _searchService;

    public SearchServiceTests()
    {
        _mockFileSystem = new Mock<IFileSystemService>();
        _mockLogger = new Mock<ILogger<SearchService>>();
        _searchService = new SearchService(_mockFileSystem.Object, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenFileSystemServiceIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new SearchService(null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new SearchService(_mockFileSystem.Object, null!));
    }

    [Fact]
    public async Task SearchInFileContentAsync_ShouldThrowArgumentNullException_WhenRootNodeIsNull()
    {
        // Arrange
        var options = new SearchOptions { SearchTerm = "test" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _searchService.SearchInFileContentAsync(null!, options, CancellationToken.None));
    }

    [Fact]
    public async Task SearchInFileContentAsync_ShouldThrowArgumentNullException_WhenOptionsIsNull()
    {
        // Arrange
        var rootNode = new FileTreeNode { Name = "root", IsDirectory = true };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _searchService.SearchInFileContentAsync(rootNode, null!, CancellationToken.None));
    }

    [Fact]
    public async Task SearchInFileContentAsync_ShouldFindMatchingFiles_WithPlainTextSearch()
    {
        // Arrange
        var rootNode = CreateTestTree();
        var options = new SearchOptions 
        { 
            SearchTerm = "function",
            IsCaseSensitive = false,
            UseRegex = false,
            MatchWholeWord = false
        };

        SetupFileContent("file1.cs", "public void function() {}");
        SetupFileContent("file2.cs", "console.log('hello');");
        SetupFileContent("file3.cs", "var myFunction = () => {};");

        // Act
        var result = await _searchService.SearchInFileContentAsync(rootNode, options, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.FilesSearched.Should().Be(3);
        result.FilesMatched.Should().Be(2);
        result.MatchedNodes.Should().HaveCount(2);
        result.MatchedNodes.Should().Contain(n => n.Name == "file1.cs");
        result.MatchedNodes.Should().Contain(n => n.Name == "file3.cs");
    }

    [Fact]
    public async Task SearchInFileContentAsync_ShouldRespectCaseSensitivity()
    {
        // Arrange
        var rootNode = CreateTestTree();
        var options = new SearchOptions 
        { 
            SearchTerm = "Function",
            IsCaseSensitive = true,
            UseRegex = false,
            MatchWholeWord = false
        };

        SetupFileContent("file1.cs", "public void function() {}");
        SetupFileContent("file2.cs", "public void Function() {}");
        SetupFileContent("file3.cs", "var myfunction = () => {};");

        // Act
        var result = await _searchService.SearchInFileContentAsync(rootNode, options, CancellationToken.None);

        // Assert
        result.FilesMatched.Should().Be(1);
        result.MatchedNodes.Should().Contain(n => n.Name == "file2.cs");
    }

    [Fact]
    public async Task SearchInFileContentAsync_ShouldMatchWholeWord()
    {
        // Arrange
        var rootNode = CreateTestTree();
        var options = new SearchOptions 
        { 
            SearchTerm = "test",
            IsCaseSensitive = false,
            UseRegex = false,
            MatchWholeWord = true
        };

        SetupFileContent("file1.cs", "test case");
        SetupFileContent("file2.cs", "testing");
        SetupFileContent("file3.cs", "my test");

        // Act
        var result = await _searchService.SearchInFileContentAsync(rootNode, options, CancellationToken.None);

        // Assert
        result.FilesMatched.Should().Be(2);
        result.MatchedNodes.Should().Contain(n => n.Name == "file1.cs");
        result.MatchedNodes.Should().Contain(n => n.Name == "file3.cs");
        result.MatchedNodes.Should().NotContain(n => n.Name == "file2.cs");
    }

    [Fact]
    public async Task SearchInFileContentAsync_ShouldSupportRegex()
    {
        // Arrange
        var rootNode = CreateTestTree();
        var options = new SearchOptions 
        { 
            SearchTerm = @"function\d+",
            IsCaseSensitive = false,
            UseRegex = true,
            MatchWholeWord = false
        };

        SetupFileContent("file1.cs", "function1()");
        SetupFileContent("file2.cs", "function()");
        SetupFileContent("file3.cs", "function42()");

        // Act
        var result = await _searchService.SearchInFileContentAsync(rootNode, options, CancellationToken.None);

        // Assert
        result.FilesMatched.Should().Be(2);
        result.MatchedNodes.Should().Contain(n => n.Name == "file1.cs");
        result.MatchedNodes.Should().Contain(n => n.Name == "file3.cs");
    }

    [Fact]
    public async Task SearchInFileContentAsync_ShouldOnlySearchVisibleNodes()
    {
        // Arrange
        var rootNode = new FileTreeNode 
        { 
            Name = "root",
            IsDirectory = true,
            IsVisible = true,
            Children = new System.Collections.ObjectModel.ObservableCollection<FileTreeNode>
            {
                new FileTreeNode 
                { 
                    Name = "visible.cs",
                    FullPath = "visible.cs",
                    IsDirectory = false,
                    IsVisible = true
                },
                new FileTreeNode 
                { 
                    Name = "hidden.cs",
                    FullPath = "hidden.cs",
                    IsDirectory = false,
                    IsVisible = false
                }
            }
        };

        var options = new SearchOptions { SearchTerm = "test" };

        SetupFileContent("visible.cs", "test content");
        SetupFileContent("hidden.cs", "test content");

        // Act
        var result = await _searchService.SearchInFileContentAsync(rootNode, options, CancellationToken.None);

        // Assert
        result.FilesSearched.Should().Be(1);
        result.FilesMatched.Should().Be(1);
        result.MatchedNodes.Should().Contain(n => n.Name == "visible.cs");
        result.MatchedNodes.Should().NotContain(n => n.Name == "hidden.cs");
    }

    [Fact]
    public async Task SearchInFileContentAsync_ShouldHandleIOException_AndContinueSearching()
    {
        // Arrange
        var rootNode = CreateTestTree();
        var options = new SearchOptions { SearchTerm = "test" };

        SetupFileContent("file1.cs", "test");
        _mockFileSystem.Setup(fs => fs.ReadFileContentAsync("file2.cs"))
            .ThrowsAsync(new IOException("File locked"));
        SetupFileContent("file3.cs", "test");

        // Act
        var result = await _searchService.SearchInFileContentAsync(rootNode, options, CancellationToken.None);

        // Assert
        result.FilesSearched.Should().Be(3);
        result.FilesMatched.Should().Be(2);
        result.MatchedNodes.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchInFileContentAsync_ShouldHandleUnauthorizedAccessException_AndContinueSearching()
    {
        // Arrange
        var rootNode = CreateTestTree();
        var options = new SearchOptions { SearchTerm = "test" };

        SetupFileContent("file1.cs", "test");
        _mockFileSystem.Setup(fs => fs.ReadFileContentAsync("file2.cs"))
            .ThrowsAsync(new UnauthorizedAccessException("Access denied"));
        SetupFileContent("file3.cs", "test");

        // Act
        var result = await _searchService.SearchInFileContentAsync(rootNode, options, CancellationToken.None);

        // Assert
        result.FilesSearched.Should().Be(3);
        result.FilesMatched.Should().Be(2);
    }

    [Fact]
    public async Task SearchInFileContentAsync_ShouldSupportCancellation()
    {
        // Arrange
        var rootNode = CreateTestTree();
        var options = new SearchOptions { SearchTerm = "test" };
        var cts = new CancellationTokenSource();

        // Setup file system to cancel after first read
        var callCount = 0;
        _mockFileSystem.Setup(fs => fs.ReadFileContentAsync(It.IsAny<string>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount > 1)
                {
                    cts.Cancel();
                }
                return "test content";
            });

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _searchService.SearchInFileContentAsync(rootNode, options, cts.Token));
    }

    [Fact]
    public async Task SearchInFileContentAsync_ShouldHandleInvalidRegex()
    {
        // Arrange
        var rootNode = CreateTestTree();
        var options = new SearchOptions 
        { 
            SearchTerm = "[invalid(regex",
            UseRegex = true
        };

        SetupFileContent("file1.cs", "some content");
        SetupFileContent("file2.cs", "other content");
        SetupFileContent("file3.cs", "more content");

        // Act
        var result = await _searchService.SearchInFileContentAsync(rootNode, options, CancellationToken.None);

        // Assert - Should not match anything due to invalid regex, but shouldn't throw
        result.FilesMatched.Should().Be(0);
    }

    [Fact]
    public void ClearSearchHighlight_ShouldThrowArgumentNullException_WhenRootNodeIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _searchService.ClearSearchHighlight(null!));
    }

    [Fact]
    public void ClearSearchHighlight_ShouldClearAllIsSearchMatchFlags()
    {
        // Arrange
        var rootNode = new FileTreeNode
        {
            Name = "root",
            IsDirectory = true,
            IsSearchMatch = true,
            Children = new System.Collections.ObjectModel.ObservableCollection<FileTreeNode>
            {
                new FileTreeNode 
                { 
                    Name = "file1.cs",
                    IsSearchMatch = true
                },
                new FileTreeNode 
                { 
                    Name = "folder",
                    IsDirectory = true,
                    IsSearchMatch = true,
                    Children = new System.Collections.ObjectModel.ObservableCollection<FileTreeNode>
                    {
                        new FileTreeNode { Name = "file2.cs", IsSearchMatch = true }
                    }
                }
            }
        };

        // Act
        _searchService.ClearSearchHighlight(rootNode);

        // Assert
        rootNode.IsSearchMatch.Should().BeFalse();
        rootNode.Children[0].IsSearchMatch.Should().BeFalse();
        rootNode.Children[1].IsSearchMatch.Should().BeFalse();
        rootNode.Children[1].Children[0].IsSearchMatch.Should().BeFalse();
    }

    #region Helper Methods

    private FileTreeNode CreateTestTree()
    {
        return new FileTreeNode
        {
            Name = "root",
            IsDirectory = true,
            IsVisible = true,
            Children = new System.Collections.ObjectModel.ObservableCollection<FileTreeNode>
            {
                new FileTreeNode 
                { 
                    Name = "file1.cs",
                    FullPath = "file1.cs",
                    IsDirectory = false,
                    IsVisible = true
                },
                new FileTreeNode 
                { 
                    Name = "file2.cs",
                    FullPath = "file2.cs",
                    IsDirectory = false,
                    IsVisible = true
                },
                new FileTreeNode 
                { 
                    Name = "file3.cs",
                    FullPath = "file3.cs",
                    IsDirectory = false,
                    IsVisible = true
                }
            }
        };
    }

    private void SetupFileContent(string path, string content)
    {
        _mockFileSystem.Setup(fs => fs.ReadFileContentAsync(path))
            .ReturnsAsync(content);
    }

    #endregion
}
