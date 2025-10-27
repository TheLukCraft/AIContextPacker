using System;
using System.Collections.Generic;
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
    private readonly Mock<ILogger<SearchService>> _mockLogger;
    private readonly SearchService _searchService;

    public SearchServiceTests()
    {
        _mockLogger = new Mock<ILogger<SearchService>>();
        _searchService = new SearchService(_mockLogger.Object);
    }

    private FileTreeNode CreateTestTree()
    {
        var root = new FileTreeNode { Name = "Root", FullPath = "C:\\Root", IsDirectory = true };
        var folder1 = new FileTreeNode { Name = "Documents", FullPath = "C:\\Root\\Documents", IsDirectory = true, Parent = root };
        var file1 = new FileTreeNode { Name = "Report.txt", FullPath = "C:\\Root\\Documents\\Report.txt", IsDirectory = false, Parent = folder1 };
        var file2 = new FileTreeNode { Name = "Summary.doc", FullPath = "C:\\Root\\Documents\\Summary.doc", IsDirectory = false, Parent = folder1 };
        
        var folder2 = new FileTreeNode { Name = "Code", FullPath = "C:\\Root\\Code", IsDirectory = true, Parent = root };
        var file3 = new FileTreeNode { Name = "Program.cs", FullPath = "C:\\Root\\Code\\Program.cs", IsDirectory = false, Parent = folder2 };
        var file4 = new FileTreeNode { Name = "README.md", FullPath = "C:\\Root\\Code\\README.md", IsDirectory = false, Parent = folder2 };

        folder1.Children.Add(file1);
        folder1.Children.Add(file2);
        folder2.Children.Add(file3);
        folder2.Children.Add(file4);
        root.Children.Add(folder1);
        root.Children.Add(folder2);

        return root;
    }

    [Fact]
    public async Task SearchAsync_WithMatchingName_ReturnsMatchedNodes()
    {
        // Arrange
        var root = CreateTestTree();
        var options = new SearchOptions
        {
            SearchTerm = "Report",
            IsCaseSensitive = false,
            UseRegex = false,
            MatchWholeWord = false
        };

        // Act
        var result = await _searchService.SearchAsync(root, options, CancellationToken.None);

        // Assert
        result.FilesMatched.Should().Be(1);
        result.MatchedNodes.Should().HaveCount(1);
        result.MatchedNodes[0].Name.Should().Be("Report.txt");
    }

    [Fact]
    public async Task SearchAsync_CaseInsensitive_FindsMatches()
    {
        // Arrange
        var root = CreateTestTree();
        var options = new SearchOptions
        {
            SearchTerm = "program",
            IsCaseSensitive = false,
            UseRegex = false,
            MatchWholeWord = false
        };

        // Act
        var result = await _searchService.SearchAsync(root, options, CancellationToken.None);

        // Assert
        result.FilesMatched.Should().Be(1);
        result.MatchedNodes[0].Name.Should().Be("Program.cs");
    }

    [Fact]
    public async Task SearchAsync_CaseSensitive_DoesNotFindMismatchedCase()
    {
        // Arrange
        var root = CreateTestTree();
        var options = new SearchOptions
        {
            SearchTerm = "program",
            IsCaseSensitive = true,
            UseRegex = false,
            MatchWholeWord = false
        };

        // Act
        var result = await _searchService.SearchAsync(root, options, CancellationToken.None);

        // Assert
        result.FilesMatched.Should().Be(0);
    }

    [Fact]
    public async Task SearchAsync_WithRegex_FindsPattern()
    {
        // Arrange
        var root = CreateTestTree();
        var options = new SearchOptions
        {
            SearchTerm = @"\.cs$",
            IsCaseSensitive = false,
            UseRegex = true,
            MatchWholeWord = false
        };

        // Act
        var result = await _searchService.SearchAsync(root, options, CancellationToken.None);

        // Assert
        result.FilesMatched.Should().Be(1);
        result.MatchedNodes[0].Name.Should().Be("Program.cs");
    }

    [Fact]
    public async Task SearchAsync_WholeWord_MatchesExactName()
    {
        // Arrange
        var root = CreateTestTree();
        var options = new SearchOptions
        {
            SearchTerm = "Code",
            IsCaseSensitive = false,
            UseRegex = false,
            MatchWholeWord = true
        };

        // Act
        var result = await _searchService.SearchAsync(root, options, CancellationToken.None);

        // Assert
        result.FilesMatched.Should().Be(1);
        result.MatchedNodes[0].Name.Should().Be("Code");
    }

    [Fact]
    public async Task SearchAsync_WholeWord_DoesNotMatchPartial()
    {
        // Arrange
        var root = CreateTestTree();
        var options = new SearchOptions
        {
            SearchTerm = "Doc",
            IsCaseSensitive = false,
            UseRegex = false,
            MatchWholeWord = true
        };

        // Act
        var result = await _searchService.SearchAsync(root, options, CancellationToken.None);

        // Assert
        result.FilesMatched.Should().Be(0);
    }

    [Fact]
    public async Task SearchAsync_SkipsInvisibleNodes()
    {
        // Arrange
        var root = CreateTestTree();
        root.Children[0].Children[0].IsVisible = false; // Hide Report.txt
        
        var options = new SearchOptions
        {
            SearchTerm = "Report",
            IsCaseSensitive = false,
            UseRegex = false,
            MatchWholeWord = false
        };

        // Act
        var result = await _searchService.SearchAsync(root, options, CancellationToken.None);

        // Assert
        result.FilesMatched.Should().Be(0);
    }

    [Fact]
    public async Task SearchAsync_FindsMultipleMatches()
    {
        // Arrange
        var root = CreateTestTree();
        var options = new SearchOptions
        {
            SearchTerm = ".", // Match all files with extensions
            IsCaseSensitive = false,
            UseRegex = false,
            MatchWholeWord = false
        };

        // Act
        var result = await _searchService.SearchAsync(root, options, CancellationToken.None);

        // Assert
        result.FilesMatched.Should().Be(4); // All files have extensions
    }

    [Fact]
    public async Task SearchAsync_WithInvalidRegex_ReturnsNoMatches()
    {
        // Arrange
        var root = CreateTestTree();
        var options = new SearchOptions
        {
            SearchTerm = "[invalid(regex",
            IsCaseSensitive = false,
            UseRegex = true,
            MatchWholeWord = false
        };

        // Act
        var result = await _searchService.SearchAsync(root, options, CancellationToken.None);

        // Assert
        result.FilesMatched.Should().Be(0);
    }

    [Fact]
    public async Task SearchAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var root = CreateTestTree();
        var options = new SearchOptions
        {
            SearchTerm = "test",
            IsCaseSensitive = false,
            UseRegex = false,
            MatchWholeWord = false
        };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await _searchService.SearchAsync(root, options, cts.Token));
    }

    [Fact]
    public async Task SearchAsync_WithNullRootNode_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new SearchOptions
        {
            SearchTerm = "test",
            IsCaseSensitive = false,
            UseRegex = false,
            MatchWholeWord = false
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _searchService.SearchAsync(null!, options, CancellationToken.None));
    }

    [Fact]
    public async Task SearchAsync_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var root = CreateTestTree();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _searchService.SearchAsync(root, null!, CancellationToken.None));
    }
}
