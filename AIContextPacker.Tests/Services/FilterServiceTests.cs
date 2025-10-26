using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AIContextPacker.Models;
using AIContextPacker.Services;
using AIContextPacker.Services.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AIContextPacker.Tests.Services;

public class FilterServiceTests
{
    private readonly Mock<ILogger<FilterService>> _loggerMock;

    public FilterServiceTests()
    {
        _loggerMock = new Mock<ILogger<FilterService>>();
    }

    #region ShouldIncludeFile Tests

    [Fact]
    public void ShouldIncludeFile_WithAllowedExtension_ReturnsTrue()
    {
        // Arrange
        var allowedExtensions = new List<string> { ".cs", ".txt", ".json" };
        var service = CreateFilterService(allowedExtensions: allowedExtensions);

        // Act
        var result = service.ShouldIncludeFile("C:\\Project\\test.cs");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldIncludeFile_WithDisallowedExtension_ReturnsFalse()
    {
        // Arrange
        var allowedExtensions = new List<string> { ".cs", ".txt" };
        var service = CreateFilterService(allowedExtensions: allowedExtensions);

        // Act
        var result = service.ShouldIncludeFile("C:\\Project\\test.exe");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldIncludeFile_IgnoredByFilter_ReturnsFalse()
    {
        // Arrange
        var allowedExtensions = new List<string> { ".cs" };
        var filters = new List<IgnoreFilter>
        {
            new IgnoreFilter
            {
                Name = "Test Filter",
                Patterns = new List<string> { "*.Designer.cs" }
            }
        };
        var service = CreateFilterService(allowedExtensions: allowedExtensions, activeFilters: filters);

        // Act
        var result = service.ShouldIncludeFile("C:\\Project\\Form1.Designer.cs");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldIncludeFile_IgnoredByGitignore_ReturnsFalse()
    {
        // Arrange
        var allowedExtensions = new List<string> { ".cs" };
        var gitignorePatterns = new List<string> { "bin/", "obj/" };
        var service = CreateFilterService(
            allowedExtensions: allowedExtensions,
            gitignorePatterns: gitignorePatterns,
            basePath: "C:\\Project");

        // Act
        var result = service.ShouldIncludeFile("C:\\Project\\bin\\Debug\\test.cs");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldIncludeFile_CaseInsensitiveExtension_ReturnsTrue()
    {
        // Arrange
        var allowedExtensions = new List<string> { ".cs" };
        var service = CreateFilterService(allowedExtensions: allowedExtensions);

        // Act
        var result = service.ShouldIncludeFile("C:\\Project\\Test.CS");

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region ShouldShowDirectory Tests

    [Fact]
    public void ShouldShowDirectory_NotFiltered_ReturnsTrue()
    {
        // Arrange
        var service = CreateFilterService();

        // Act
        var result = service.ShouldShowDirectory("C:\\Project\\Source");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldShowDirectory_IgnoredByFilter_ReturnsFalse()
    {
        // Arrange
        var filters = new List<IgnoreFilter>
        {
            new IgnoreFilter
            {
                Name = "Test Filter",
                Patterns = new List<string> { "node_modules/" }
            }
        };
        var service = CreateFilterService(
            activeFilters: filters,
            basePath: "C:\\Project");

        // Act
        var result = service.ShouldShowDirectory("C:\\Project\\node_modules");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldShowDirectory_IgnoredByGitignore_ReturnsFalse()
    {
        // Arrange
        var gitignorePatterns = new List<string> { ".vs/", "bin/", "obj/" };
        var service = CreateFilterService(
            gitignorePatterns: gitignorePatterns,
            basePath: "C:\\Project");

        // Act
        var result = service.ShouldShowDirectory("C:\\Project\\.vs");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region ApplyFiltersAsync Tests

    [Fact]
    public async Task ApplyFiltersAsync_SingleFile_FiltersCorrectly()
    {
        // Arrange
        var allowedExtensions = new List<string> { ".cs" };
        var service = CreateFilterService(allowedExtensions: allowedExtensions);

        var rootNode = new FileTreeNode
        {
            Name = "Root",
            FullPath = "C:\\Project",
            IsDirectory = true,
            Children =
            {
                new FileTreeNode
                {
                    Name = "test.cs",
                    FullPath = "C:\\Project\\test.cs",
                    IsDirectory = false
                },
                new FileTreeNode
                {
                    Name = "test.txt",
                    FullPath = "C:\\Project\\test.txt",
                    IsDirectory = false
                }
            }
        };

        // Act
        await service.ApplyFiltersAsync(rootNode);

        // Assert
        rootNode.Children[0].IsVisible.Should().BeTrue("test.cs has allowed extension");
        rootNode.Children[1].IsVisible.Should().BeFalse("test.txt does not have allowed extension");
        rootNode.IsVisible.Should().BeTrue("root directory should be visible when it has visible children");
    }

    [Fact]
    public async Task ApplyFiltersAsync_FilteredDirectory_HidesAllChildren()
    {
        // Arrange
        var allowedExtensions = new List<string> { ".cs" };
        var gitignorePatterns = new List<string> { "bin/" };
        var service = CreateFilterService(
            allowedExtensions: allowedExtensions,
            gitignorePatterns: gitignorePatterns,
            basePath: "C:\\Project");

        var binDirectory = new FileTreeNode
        {
            Name = "bin",
            FullPath = "C:\\Project\\bin",
            IsDirectory = true,
            Children =
            {
                new FileTreeNode
                {
                    Name = "test.cs",
                    FullPath = "C:\\Project\\bin\\test.cs",
                    IsDirectory = false
                }
            }
        };

        var rootNode = new FileTreeNode
        {
            Name = "Root",
            FullPath = "C:\\Project",
            IsDirectory = true,
            Children = { binDirectory }
        };

        // Act
        await service.ApplyFiltersAsync(rootNode);

        // Assert
        binDirectory.IsVisible.Should().BeFalse("bin directory should be filtered out");
        rootNode.IsVisible.Should().BeFalse("root should be invisible when all children are filtered");
    }

    [Fact]
    public async Task ApplyFiltersAsync_NestedStructure_FiltersCorrectly()
    {
        // Arrange
        var allowedExtensions = new List<string> { ".cs", ".json" };
        var filters = new List<IgnoreFilter>
        {
            new IgnoreFilter
            {
                Name = "Designer Files",
                Patterns = new List<string> { "*.Designer.cs" }
            }
        };
        var service = CreateFilterService(
            allowedExtensions: allowedExtensions,
            activeFilters: filters,
            basePath: "C:\\Project");

        var rootNode = new FileTreeNode
        {
            Name = "Root",
            FullPath = "C:\\Project",
            IsDirectory = true,
            Children =
            {
                new FileTreeNode
                {
                    Name = "Source",
                    FullPath = "C:\\Project\\Source",
                    IsDirectory = true,
                    Children =
                    {
                        new FileTreeNode
                        {
                            Name = "Program.cs",
                            FullPath = "C:\\Project\\Source\\Program.cs",
                            IsDirectory = false
                        },
                        new FileTreeNode
                        {
                            Name = "Form1.Designer.cs",
                            FullPath = "C:\\Project\\Source\\Form1.Designer.cs",
                            IsDirectory = false
                        }
                    }
                },
                new FileTreeNode
                {
                    Name = "config.json",
                    FullPath = "C:\\Project\\config.json",
                    IsDirectory = false
                }
            }
        };

        // Act
        await service.ApplyFiltersAsync(rootNode);

        // Assert
        rootNode.Children[0].Children[0].IsVisible.Should().BeTrue("Program.cs should be visible");
        rootNode.Children[0].Children[1].IsVisible.Should().BeFalse("Designer.cs should be filtered");
        rootNode.Children[0].IsVisible.Should().BeTrue("Source directory should be visible (has visible children)");
        rootNode.Children[1].IsVisible.Should().BeTrue("config.json should be visible");
        rootNode.IsVisible.Should().BeTrue("root should be visible");
    }

    [Fact]
    public async Task ApplyFiltersAsync_WithProgressReporter_ReportsProgress()
    {
        // Arrange
        var allowedExtensions = new List<string> { ".cs" };
        var service = CreateFilterService(allowedExtensions: allowedExtensions);

        var progressReports = new List<(string status, double? percent)>();
        var progressMock = new Mock<IProgressReporter>();
        progressMock.Setup(p => p.Report(It.IsAny<string>(), It.IsAny<double?>()))
            .Callback<string, double?>((status, percent) => progressReports.Add((status, percent)));
        progressMock.Setup(p => p.CancellationToken).Returns(CancellationToken.None);

        // Create a tree with 100+ nodes to trigger progress reporting
        var rootNode = new FileTreeNode
        {
            Name = "Root",
            FullPath = "C:\\Project",
            IsDirectory = true
        };

        for (int i = 0; i < 60; i++)
        {
            rootNode.Children.Add(new FileTreeNode
            {
                Name = $"file{i}.cs",
                FullPath = $"C:\\Project\\file{i}.cs",
                IsDirectory = false
            });
        }

        // Act
        await service.ApplyFiltersAsync(rootNode, progressMock.Object);

        // Assert
        progressReports.Should().NotBeEmpty("progress should be reported for 60+ nodes");
        progressReports.Should().Contain(r => r.percent > 0, "progress percentage should be reported");
        progressMock.Verify(p => p.Clear(), Times.Once, "progress should be cleared at the end");
    }

    [Fact]
    public async Task ApplyFiltersAsync_WithCancellation_StopsProcessing()
    {
        // Arrange
        var allowedExtensions = new List<string> { ".cs" };
        var service = CreateFilterService(allowedExtensions: allowedExtensions);

        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        var progressMock = new Mock<IProgressReporter>();
        progressMock.Setup(p => p.CancellationToken).Returns(cts.Token);

        var rootNode = new FileTreeNode
        {
            Name = "Root",
            FullPath = "C:\\Project",
            IsDirectory = true,
            Children =
            {
                new FileTreeNode
                {
                    Name = "test.cs",
                    FullPath = "C:\\Project\\test.cs",
                    IsDirectory = false
                }
            }
        };

        // Act
        await service.ApplyFiltersAsync(rootNode, progressMock.Object);

        // Assert
        // When cancelled immediately, the first node (root) is processed but no children
        // This is expected behavior - cancellation is checked before processing children
        progressMock.Verify(p => p.Clear(), Times.Once);
    }

    [Fact]
    public async Task ApplyFiltersAsync_WithoutProgressReporter_CompletesSuccessfully()
    {
        // Arrange
        var allowedExtensions = new List<string> { ".cs", ".txt" };
        var service = CreateFilterService(allowedExtensions: allowedExtensions);

        var rootNode = new FileTreeNode
        {
            Name = "Root",
            FullPath = "C:\\Project",
            IsDirectory = true,
            Children =
            {
                new FileTreeNode
                {
                    Name = "test.cs",
                    FullPath = "C:\\Project\\test.cs",
                    IsDirectory = false
                }
            }
        };

        // Act
        Func<Task> act = async () => await service.ApplyFiltersAsync(rootNode, null);

        // Assert
        await act.Should().NotThrowAsync("should work without progress reporter");
        rootNode.Children[0].IsVisible.Should().BeTrue();
    }

    [Fact]
    public async Task ApplyFiltersAsync_EmptyTree_CompletesSuccessfully()
    {
        // Arrange
        var service = CreateFilterService();
        var rootNode = new FileTreeNode
        {
            Name = "Root",
            FullPath = "C:\\Project",
            IsDirectory = true
        };

        // Act
        await service.ApplyFiltersAsync(rootNode);

        // Assert
        rootNode.IsVisible.Should().BeFalse("empty directory should be invisible");
    }

    #endregion

    #region Pattern Matching Tests

    [Theory]
    [InlineData("test.cs", "*.cs", true)]
    [InlineData("test.txt", "*.cs", false)]
    [InlineData("Program.cs", "Program.*", true)]
    [InlineData("Test.cs", "test.cs", true)] // Case insensitive
    [InlineData("folder/test.cs", "test.cs", true)] // Match in subdirectory
    public void ShouldIncludeFile_WithWildcardPattern_MatchesCorrectly(string filePath, string pattern, bool expected)
    {
        // Arrange
        var allowedExtensions = new List<string> { ".cs", ".txt" };
        var filters = new List<IgnoreFilter>
        {
            new IgnoreFilter
            {
                Name = "Test Filter",
                Patterns = new List<string> { pattern }
            }
        };
        var service = CreateFilterService(
            allowedExtensions: allowedExtensions,
            activeFilters: filters,
            basePath: "C:\\Project");

        // Act
        var result = service.ShouldIncludeFile($"C:\\Project\\{filePath}");

        // Assert
        result.Should().Be(!expected, $"pattern '{pattern}' should {(expected ? "match" : "not match")} '{filePath}'");
    }

    [Theory]
    [InlineData("bin/Debug/test.cs", "bin/", true)]
    [InlineData("src/bin/test.cs", "bin/", true)]
    [InlineData("binaries/test.cs", "bin/", false)]
    [InlineData("obj/x64/test.cs", "obj/", true)]
    public void ShouldIncludeFile_WithDirectoryPattern_MatchesCorrectly(string filePath, string pattern, bool shouldFilter)
    {
        // Arrange
        var allowedExtensions = new List<string> { ".cs" };
        var gitignorePatterns = new List<string> { pattern };
        var service = CreateFilterService(
            allowedExtensions: allowedExtensions,
            gitignorePatterns: gitignorePatterns,
            basePath: "C:\\Project");

        // Act
        var result = service.ShouldIncludeFile($"C:\\Project\\{filePath}");

        // Assert
        result.Should().Be(!shouldFilter, $"pattern '{pattern}' should {(shouldFilter ? "filter" : "not filter")} '{filePath}'");
    }

    [Theory]
    [InlineData("bin/", "bin/Debug/test.cs", true)]
    [InlineData("obj/", "obj/x64/test.dll", true)]
    [InlineData("*.log", "app.log", true)]
    [InlineData("*.log", "app.cs", false)]
    public void ShouldIncludeFile_WithCommonGitignorePatterns_MatchesCorrectly(string pattern, string filePath, bool shouldFilter)
    {
        // Arrange
        var allowedExtensions = new List<string> { ".log", ".js", ".cs", ".dll" };
        var gitignorePatterns = new List<string> { pattern };
        var service = CreateFilterService(
            allowedExtensions: allowedExtensions,
            gitignorePatterns: gitignorePatterns,
            basePath: "C:\\Project");

        // Act
        var result = service.ShouldIncludeFile($"C:\\Project\\{filePath}");

        // Assert
        result.Should().Be(!shouldFilter, $"pattern '{pattern}' should {(shouldFilter ? "filter" : "not filter")} '{filePath}'");
    }

    #endregion

    #region Helper Methods

    private FilterService CreateFilterService(
        List<string>? allowedExtensions = null,
        List<IgnoreFilter>? activeFilters = null,
        List<string>? gitignorePatterns = null,
        string basePath = "C:\\Project")
    {
        return new FilterService(
            allowedExtensions ?? new List<string>(),
            activeFilters ?? new List<IgnoreFilter>(),
            gitignorePatterns ?? new List<string>(),
            basePath,
            _loggerMock.Object);
    }

    #endregion
}
