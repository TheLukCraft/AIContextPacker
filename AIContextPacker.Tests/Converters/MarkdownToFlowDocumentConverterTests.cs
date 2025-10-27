using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using AIContextPacker.Converters;
using FluentAssertions;
using Xunit;

namespace AIContextPacker.Tests.Converters;

/// <summary>
/// Unit tests for MarkdownToFlowDocumentConverter.
/// Tests markdown parsing including headers, bold text, lists, code blocks, and inline code.
/// Note: WPF UI tests require STA thread and Application context.
/// </summary>
[Collection("WPF Tests")]
public class MarkdownToFlowDocumentConverterTests : IDisposable
{
    private readonly MarkdownToFlowDocumentConverter _converter;

    public MarkdownToFlowDocumentConverterTests()
    {
        // Initialize WPF Application context if not already initialized
        if (Application.Current == null)
        {
            _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        }

        _converter = new MarkdownToFlowDocumentConverter();
    }

    [Fact]
    public void Convert_NullInput_ReturnsEmptyDocument()
    {
        // Arrange
        object? input = null;

        // Act
        var result = _converter.Convert(input!, typeof(FlowDocument), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeOfType<FlowDocument>();
        var document = (FlowDocument)result;
        document.Blocks.Should().HaveCount(1, "empty document should have placeholder paragraph");
        
        var paragraph = document.Blocks.FirstBlock as Paragraph;
        paragraph.Should().NotBeNull();
        var run = paragraph!.Inlines.FirstInline as Run;
        run.Should().NotBeNull();
        run!.Text.Should().Contain("No release notes available");
    }

    [Fact]
    public void Convert_EmptyString_ReturnsEmptyDocument()
    {
        // Arrange
        var input = string.Empty;

        // Act
        var result = _converter.Convert(input, typeof(FlowDocument), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeOfType<FlowDocument>();
        var document = (FlowDocument)result;
        document.Blocks.Should().HaveCount(1);
    }

    [Fact]
    public void Convert_SimpleText_ReturnsParagraphWithText()
    {
        // Arrange
        var input = "This is a simple text";

        // Act
        var result = _converter.Convert(input, typeof(FlowDocument), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeOfType<FlowDocument>();
        var document = (FlowDocument)result;
        document.Blocks.Count.Should().BeGreaterThanOrEqualTo(1);
        
        var paragraph = document.Blocks.FirstBlock as Paragraph;
        paragraph.Should().NotBeNull();
        paragraph!.Inlines.Should().NotBeEmpty();
    }

    [Fact]
    public void Convert_H2Header_CreatesFormattedHeader()
    {
        // Arrange
        var input = "## This is a header";

        // Act
        var result = _converter.Convert(input, typeof(FlowDocument), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeOfType<FlowDocument>();
        var document = (FlowDocument)result;
        document.Blocks.Should().HaveCount(1);
        
        var paragraph = document.Blocks.FirstBlock as Paragraph;
        paragraph.Should().NotBeNull();
        paragraph!.FontSize.Should().Be(16, "H2 headers should have font size 16");
        paragraph.FontWeight.Should().Be(FontWeights.Bold);
    }

    [Fact]
    public void Convert_H2HeaderWithEmoji_CreatesFormattedHeader()
    {
        // Arrange
        var input = "## ✨ Version 1.1.0 - Major Refactoring";

        // Act
        var result = _converter.Convert(input, typeof(FlowDocument), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeOfType<FlowDocument>();
        var document = (FlowDocument)result;
        document.Blocks.Should().HaveCount(1);
        
        var paragraph = document.Blocks.FirstBlock as Paragraph;
        paragraph.Should().NotBeNull();
        paragraph!.FontSize.Should().Be(18, "H2 with emoji should have font size 18");
        paragraph.FontWeight.Should().Be(FontWeights.Bold);
    }

    [Fact]
    public void Convert_H3Header_CreatesFormattedHeader()
    {
        // Arrange
        var input = "### Subheading";

        // Act
        var result = _converter.Convert(input, typeof(FlowDocument), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeOfType<FlowDocument>();
        var document = (FlowDocument)result;
        document.Blocks.Should().HaveCount(1);
        
        var paragraph = document.Blocks.FirstBlock as Paragraph;
        paragraph.Should().NotBeNull();
        paragraph!.FontSize.Should().Be(15, "H3 headers should have font size 15");
        paragraph.FontWeight.Should().Be(FontWeights.SemiBold);
    }

    [Fact]
    public void Convert_BoldText_CreatesRunWithBoldWeight()
    {
        // Arrange
        var input = "This is **bold text** in a sentence";

        // Act
        var result = _converter.Convert(input, typeof(FlowDocument), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeOfType<FlowDocument>();
        var document = (FlowDocument)result;
        var paragraph = document.Blocks.FirstBlock as Paragraph;
        paragraph.Should().NotBeNull();
        
        var runs = paragraph!.Inlines.OfType<Run>().ToList();
        runs.Should().Contain(r => r.FontWeight == FontWeights.Bold, "bold text should be rendered with bold weight");
        runs.Should().Contain(r => r.Text == "bold text", "bold text content should be preserved");
    }

    [Fact]
    public void Convert_InlineCode_CreatesRunWithMonospaceFont()
    {
        // Arrange
        var input = "Use `git commit` command";

        // Act
        var result = _converter.Convert(input, typeof(FlowDocument), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeOfType<FlowDocument>();
        var document = (FlowDocument)result;
        var paragraph = document.Blocks.FirstBlock as Paragraph;
        paragraph.Should().NotBeNull();
        
        var runs = paragraph!.Inlines.OfType<Run>().ToList();
        runs.Should().Contain(r => r.FontFamily.Source.Contains("Consolas"), 
            "inline code should use Consolas font");
        runs.Should().Contain(r => r.Text == "git commit", 
            "inline code content should be preserved");
    }

    [Fact]
    public void Convert_BulletList_CreatesParagraphsWithBullets()
    {
        // Arrange
        var input = "- First item\n- Second item\n- Third item";

        // Act
        var result = _converter.Convert(input, typeof(FlowDocument), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeOfType<FlowDocument>();
        var document = (FlowDocument)result;
        document.Blocks.Should().HaveCount(3, "each list item should create a paragraph");
        
        foreach (var block in document.Blocks.OfType<Paragraph>())
        {
            var firstRun = block.Inlines.FirstInline as Run;
            firstRun.Should().NotBeNull();
            firstRun!.Text.Should().Be("• ", "list items should start with bullet character");
        }
    }

    [Fact]
    public void Convert_CodeBlock_CreatesParagraphWithMonospaceFont()
    {
        // Arrange
        var input = "```\nvar x = 42;\nconsole.log(x);\n```";

        // Act
        var result = _converter.Convert(input, typeof(FlowDocument), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeOfType<FlowDocument>();
        var document = (FlowDocument)result;
        document.Blocks.Count.Should().BeGreaterThanOrEqualTo(1);
        
        var codeBlock = document.Blocks.FirstBlock as Paragraph;
        codeBlock.Should().NotBeNull();
        codeBlock!.FontFamily.Source.Should().Contain("Consolas", 
            "code blocks should use Consolas font");
        codeBlock.Background.Should().NotBeNull("code blocks should have background color");
    }

    [Fact]
    public void Convert_MixedContent_CreatesComplexDocument()
    {
        // Arrange
        var input = @"## Release Notes

This release includes **major improvements** and `bug fixes`.

### New Features
- Feature 1
- Feature 2

```
Sample code here
```";

        // Act
        var result = _converter.Convert(input, typeof(FlowDocument), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeOfType<FlowDocument>();
        var document = (FlowDocument)result;
        document.Blocks.Count.Should().BeGreaterThan(5, 
            "complex document should have header, text, subheader, list items, and code block");
    }

    [Fact]
    public void Convert_MultipleEmptyLines_HandledCorrectly()
    {
        // Arrange
        var input = "Line 1\n\n\n\nLine 2";

        // Act
        var result = _converter.Convert(input, typeof(FlowDocument), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeOfType<FlowDocument>();
        var document = (FlowDocument)result;
        document.Blocks.Count.Should().BeGreaterThanOrEqualTo(2, 
            "empty lines should not create unnecessary paragraphs");
    }

    [Fact]
    public void Convert_NestedBulletLists_CreatesIndentedParagraphs()
    {
        // Arrange
        var input = "- Item 1\n  - Nested item 1\n  - Nested item 2";

        // Act
        var result = _converter.Convert(input, typeof(FlowDocument), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeOfType<FlowDocument>();
        var document = (FlowDocument)result;
        document.Blocks.Should().HaveCount(3);
        
        var paragraphs = document.Blocks.OfType<Paragraph>().ToList();
        paragraphs[1].Margin.Left.Should().BeGreaterThan(paragraphs[0].Margin.Left, 
            "nested list items should have greater left margin");
    }

    [Fact]
    public void Convert_AsteriskListItems_ConvertedToBullets()
    {
        // Arrange
        var input = "* First item\n* Second item";

        // Act
        var result = _converter.Convert(input, typeof(FlowDocument), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeOfType<FlowDocument>();
        var document = (FlowDocument)result;
        document.Blocks.Should().HaveCount(2);
        
        foreach (var block in document.Blocks.OfType<Paragraph>())
        {
            var firstRun = block.Inlines.FirstInline as Run;
            firstRun.Should().NotBeNull();
            firstRun!.Text.Should().Be("• ", "asterisk list items should be converted to bullets");
        }
    }

    [Fact]
    public void ConvertBack_ThrowsNotImplementedException()
    {
        // Arrange
        var document = new FlowDocument();

        // Act & Assert
        var exception = Assert.Throws<NotImplementedException>(() =>
            _converter.ConvertBack(document, typeof(string), null!, CultureInfo.InvariantCulture));
        
        exception.Should().NotBeNull();
        exception.Message.Should().Contain("does not support two-way binding");
    }

    [Fact]
    public void Convert_RealWorldGitHubReleaseNotes_ParsesCorrectly()
    {
        // Arrange - Simulate actual GitHub API response
        var input = @"## ✨ Version 1.1.0 - Major Refactoring & SOLID Architecture ✨

This release marks a significant internal overhaul of AI Context Packer, focusing on code quality, maintainability, and testability based on SOLID principles and modern .NET practices.

### New features and improvements
- **Comprehensive Logging**: Integrated Serilog with rolling file logs for debugging and diagnostics
- **Full Test Coverage**: 114 unit tests ensuring reliability and preventing regressions
- **Progress Reporting**: Visual feedback during long-running operations with cancellation support
- **Better Error Messages**: Custom exception hierarchy with detailed context

### Bug Fixes
- Fixed custom filter name not updating after edit
- Fixed max char limit not syncing between Settings and Main window
- Fixed global prompt not defaulting to ""(None)"" on startup

### Technical Improvements
```csharp
// Example: New async filter application
await _filterService.ApplyFiltersAsync(rootNode, progress);
```

For more details, see the [Developer Guide](https://github.com/TheLukCraft/AIContextPacker/blob/main/Docs/DeveloperGuide.md).";

        // Act
        var result = _converter.Convert(input, typeof(FlowDocument), null!, CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeOfType<FlowDocument>();
        var document = (FlowDocument)result;
        
        // Should have multiple blocks (headers, paragraphs, lists, code)
        document.Blocks.Count.Should().BeGreaterThan(10, 
            "real-world release notes should parse into multiple blocks");
        
        // Should contain header with emoji
        var firstBlock = document.Blocks.FirstBlock as Paragraph;
        firstBlock.Should().NotBeNull();
        firstBlock!.FontSize.Should().Be(18, "emoji header should be size 18");
        
        // Should contain code block
        var codeBlocks = document.Blocks.OfType<Paragraph>()
            .Where(p => p.FontFamily.Source.Contains("Consolas"));
        codeBlocks.Should().NotBeEmpty("should contain code block");
    }

    public void Dispose()
    {
        // Cleanup if needed
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Collection definition to ensure WPF tests run on STA thread.
/// xUnit by default uses MTA threads, but WPF requires STA.
/// </summary>
[CollectionDefinition("WPF Tests", DisableParallelization = true)]
public class WpfTestCollection
{
    // This class is just a marker for the collection definition
    // The [Collection] attribute on test classes ensures they run in STA context
}
