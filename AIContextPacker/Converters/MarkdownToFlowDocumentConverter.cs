using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace AIContextPacker.Converters;

/// <summary>
/// Converts Markdown text to a FlowDocument with formatted content.
/// Supports headers (##, ###), bold (**text**), code blocks (```), and lists.
/// </summary>
public class MarkdownToFlowDocumentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        try
        {
            if (value is not string markdown || string.IsNullOrWhiteSpace(markdown))
            {
                return CreateEmptyDocument();
            }

            var document = new FlowDocument
            {
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 13,
                LineHeight = 22,
                PagePadding = new Thickness(0)
            };

        var lines = markdown.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        Paragraph? currentParagraph = null;
        bool inCodeBlock = false;
        Paragraph? codeBlockParagraph = null;

        foreach (var line in lines)
        {
            // Handle code blocks
            if (line.Trim().StartsWith("```"))
            {
                if (!inCodeBlock)
                {
                    // Start code block
                    inCodeBlock = true;
                    codeBlockParagraph = new Paragraph
                    {
                        Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                        Padding = new Thickness(10),
                        Margin = new Thickness(0, 5, 0, 5),
                        FontFamily = new FontFamily("Consolas")
                    };
                    document.Blocks.Add(codeBlockParagraph);
                    currentParagraph = null;
                }
                else
                {
                    // End code block
                    inCodeBlock = false;
                    codeBlockParagraph = null;
                    currentParagraph = null;
                }
                continue;
            }

            if (inCodeBlock)
            {
                // Add line to code block
                if (codeBlockParagraph != null)
                {
                    if (codeBlockParagraph.Inlines.Count > 0)
                    {
                        codeBlockParagraph.Inlines.Add(new LineBreak());
                    }
                    codeBlockParagraph.Inlines.Add(new Run(line)
                    {
                        Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220))
                    });
                }
                continue;
            }

            // Handle headers
            if (line.StartsWith("## ✨") || line.StartsWith("##✨"))
            {
                var headerText = Regex.Replace(line, @"^##\s*✨?\s*", "");
                var headerParagraph = new Paragraph(new Run(headerText))
                {
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 10, 0, 5),
                    Foreground = Application.Current.TryFindResource("TextBrush") as SolidColorBrush 
                                 ?? new SolidColorBrush(Colors.White)
                };
                document.Blocks.Add(headerParagraph);
                currentParagraph = null;
                continue;
            }

            if (line.StartsWith("### "))
            {
                var headerText = line.Substring(4);
                var headerParagraph = new Paragraph(new Run(headerText))
                {
                    FontSize = 15,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 8, 0, 4),
                    Foreground = Application.Current.TryFindResource("TextBrush") as SolidColorBrush 
                                 ?? new SolidColorBrush(Colors.White)
                };
                document.Blocks.Add(headerParagraph);
                currentParagraph = null;
                continue;
            }

            if (line.StartsWith("## "))
            {
                var headerText = line.Substring(3);
                var headerParagraph = new Paragraph(new Run(headerText))
                {
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 10, 0, 5),
                    Foreground = Application.Current.TryFindResource("TextBrush") as SolidColorBrush 
                                 ?? new SolidColorBrush(Colors.White)
                };
                document.Blocks.Add(headerParagraph);
                currentParagraph = null;
                continue;
            }

            // Handle empty lines
            if (string.IsNullOrWhiteSpace(line))
            {
                currentParagraph = null;
                continue;
            }

            // Handle list items
            if (line.TrimStart().StartsWith("- ") || line.TrimStart().StartsWith("* "))
            {
                var indent = line.TakeWhile(char.IsWhiteSpace).Count();
                var listText = Regex.Replace(line.TrimStart(), @"^[-*]\s*", "");
                
                var listParagraph = new Paragraph
                {
                    Margin = new Thickness(indent * 10 + 15, 2, 0, 2),
                    TextIndent = -10
                };
                
                listParagraph.Inlines.Add(new Run("• ") 
                { 
                    Foreground = Application.Current.TryFindResource("PrimaryBrush") as SolidColorBrush 
                                 ?? new SolidColorBrush(Color.FromRgb(92, 107, 192))
                });
                
                AddFormattedText(listParagraph, listText);
                document.Blocks.Add(listParagraph);
                currentParagraph = null;
                continue;
            }

            // Create new paragraph if needed for normal text
            if (currentParagraph == null)
            {
                currentParagraph = new Paragraph
                {
                    Margin = new Thickness(0, 3, 0, 3),
                    TextAlignment = TextAlignment.Left
                };
                document.Blocks.Add(currentParagraph);
            }

            // Add normal text with inline formatting
            if (currentParagraph.Inlines.Count > 0)
            {
                currentParagraph.Inlines.Add(new Run(" "));
            }
            AddFormattedText(currentParagraph, line);
        }

        return document;
        }
        catch (Exception ex)
        {
            // Fallback to simple document if conversion fails
            System.Diagnostics.Debug.WriteLine($"MarkdownToFlowDocumentConverter error: {ex.Message}");
            return CreateEmptyDocument();
        }
    }

    /// <summary>
    /// Adds formatted text with support for bold (**text**), inline code (`code`).
    /// </summary>
    private void AddFormattedText(Paragraph paragraph, string text)
    {
        // Pattern for bold (**text**) and inline code (`code`)
        var pattern = @"(\*\*(.+?)\*\*)|(`(.+?)`)";
        var lastIndex = 0;

        foreach (Match match in Regex.Matches(text, pattern))
        {
            // Add text before match
            if (match.Index > lastIndex)
            {
                var normalText = text.Substring(lastIndex, match.Index - lastIndex);
                paragraph.Inlines.Add(new Run(normalText)
                {
                    Foreground = Application.Current.TryFindResource("TextBrush") as SolidColorBrush 
                                 ?? new SolidColorBrush(Colors.White)
                });
            }

            if (match.Groups[2].Success) // Bold
            {
                paragraph.Inlines.Add(new Run(match.Groups[2].Value)
                {
                    FontWeight = FontWeights.Bold,
                    Foreground = Application.Current.TryFindResource("TextBrush") as SolidColorBrush 
                                 ?? new SolidColorBrush(Colors.White)
                });
            }
            else if (match.Groups[4].Success) // Inline code
            {
                paragraph.Inlines.Add(new Run(match.Groups[4].Value)
                {
                    FontFamily = new FontFamily("Consolas"),
                    Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                    Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                    FontSize = 12
                });
            }

            lastIndex = match.Index + match.Length;
        }

        // Add remaining text
        if (lastIndex < text.Length)
        {
            var remainingText = text.Substring(lastIndex);
            paragraph.Inlines.Add(new Run(remainingText)
            {
                Foreground = Application.Current.TryFindResource("TextBrush") as SolidColorBrush 
                             ?? new SolidColorBrush(Colors.White)
            });
        }
    }

    /// <summary>
    /// Creates an empty FlowDocument with a placeholder message.
    /// </summary>
    private FlowDocument CreateEmptyDocument()
    {
        var document = new FlowDocument();
        var paragraph = new Paragraph(new Run("No release notes available."))
        {
            FontStyle = FontStyles.Italic,
            Foreground = Application.Current.TryFindResource("SecondaryTextBrush") as SolidColorBrush 
                         ?? new SolidColorBrush(Colors.Gray)
        };
        document.Blocks.Add(paragraph);
        return document;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException("MarkdownToFlowDocumentConverter does not support two-way binding.");
    }
}
