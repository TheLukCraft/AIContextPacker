using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AIContextPacker.Exceptions;
using AIContextPacker.Models;
using AIContextPacker.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace AIContextPacker.Services;

/// <summary>
/// Helper class to track search state across recursive calls.
/// </summary>
internal class SearchState
{
    public int FilesSearched { get; set; }
}

/// <summary>
/// Provides file content search functionality for the project tree.
/// </summary>
public class SearchService : ISearchService
{
    private readonly IFileSystemService _fileSystemService;
    private readonly ILogger<SearchService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchService"/> class.
    /// </summary>
    /// <param name="fileSystemService">Service for file system operations.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public SearchService(
        IFileSystemService fileSystemService,
        ILogger<SearchService> logger)
    {
        _fileSystemService = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Searches for content in visible files within the file tree.
    /// </summary>
    public async Task<SearchResult> SearchInFileContentAsync(
        FileTreeNode rootNode,
        SearchOptions options,
        CancellationToken cancellationToken)
    {
        if (rootNode == null)
            throw new ArgumentNullException(nameof(rootNode));
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        _logger.LogInformation(
            "Starting file content search: Term='{SearchTerm}', CaseSensitive={CaseSensitive}, Regex={UseRegex}, WholeWord={WholeWord}",
            options.SearchTerm, options.IsCaseSensitive, options.UseRegex, options.MatchWholeWord);

        var stopwatch = Stopwatch.StartNew();
        var matchedNodes = new List<FileTreeNode>();
        var searchState = new SearchState();

        try
        {
            await SearchRecursiveAsync(rootNode, options, matchedNodes, searchState, cancellationToken)
                .ConfigureAwait(false);

            stopwatch.Stop();

            var result = new SearchResult
            {
                FilesSearched = searchState.FilesSearched,
                FilesMatched = matchedNodes.Count,
                MatchedNodes = matchedNodes
            };

            _logger.LogInformation(
                "Search completed in {ElapsedMs}ms: {FilesSearched} files searched, {FilesMatched} matches found",
                stopwatch.ElapsedMilliseconds, result.FilesSearched, result.FilesMatched);

            return result;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogInformation(
                "Search cancelled after {ElapsedMs}ms: {FilesSearched} files searched, {FilesMatched} matches found before cancellation",
                stopwatch.ElapsedMilliseconds, searchState.FilesSearched, matchedNodes.Count);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Search failed after {ElapsedMs}ms: {FilesSearched} files searched",
                stopwatch.ElapsedMilliseconds, searchState.FilesSearched);
            throw new AIContextPackerException(
                $"File content search failed: {ex.Message}",
                innerException: ex);
        }
    }

    /// <summary>
    /// Clears search highlight state from all nodes in the tree.
    /// </summary>
    public void ClearSearchHighlight(FileTreeNode rootNode)
    {
        if (rootNode == null)
            throw new ArgumentNullException(nameof(rootNode));

        _logger.LogDebug("Clearing search highlights");

        ClearSearchHighlightRecursive(rootNode);
    }

    /// <summary>
    /// Recursively searches through the file tree for matching content.
    /// </summary>
    private async Task SearchRecursiveAsync(
        FileTreeNode node,
        SearchOptions options,
        List<FileTreeNode> matchedNodes,
        SearchState searchState,
        CancellationToken cancellationToken)
    {
        // Check for cancellation
        cancellationToken.ThrowIfCancellationRequested();

        // Only process visible nodes
        if (!node.IsVisible)
            return;

        // If it's a file, search its content
        if (!node.IsDirectory)
        {
            searchState.FilesSearched++;

            try
            {
                var content = await _fileSystemService.ReadFileContentAsync(node.FullPath)
                    .ConfigureAwait(false);

                if (IsMatch(content, options))
                {
                    matchedNodes.Add(node);
                    _logger.LogDebug("Match found in file: {FilePath}", node.FullPath);
                }
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Failed to read file for search: {FilePath}", node.FullPath);
                // Continue searching other files
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Access denied to file: {FilePath}", node.FullPath);
                // Continue searching other files
            }
        }

        // Recursively search children
        foreach (var child in node.Children)
        {
            await SearchRecursiveAsync(child, options, matchedNodes, searchState, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Determines if content matches the search criteria.
    /// </summary>
    private bool IsMatch(string content, SearchOptions options)
    {
        if (string.IsNullOrEmpty(content))
            return false;

        try
        {
            if (options.UseRegex)
            {
                // Use regex matching
                var regexOptions = options.IsCaseSensitive 
                    ? RegexOptions.None 
                    : RegexOptions.IgnoreCase;

                var pattern = options.MatchWholeWord
                    ? $@"\b{Regex.Escape(options.SearchTerm)}\b"
                    : options.SearchTerm;

                return Regex.IsMatch(content, pattern, regexOptions);
            }
            else if (options.MatchWholeWord)
            {
                // Use regex for whole word matching without treating search term as regex
                var regexOptions = options.IsCaseSensitive
                    ? RegexOptions.None
                    : RegexOptions.IgnoreCase;

                var pattern = $@"\b{Regex.Escape(options.SearchTerm)}\b";

                return Regex.IsMatch(content, pattern, regexOptions);
            }
            else
            {
                // Simple string contains
                var comparison = options.IsCaseSensitive
                    ? StringComparison.Ordinal
                    : StringComparison.OrdinalIgnoreCase;

                return content.Contains(options.SearchTerm, comparison);
            }
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid regex pattern: {SearchTerm}", options.SearchTerm);
            return false;
        }
    }

    /// <summary>
    /// Recursively clears search highlight state from all nodes.
    /// </summary>
    private void ClearSearchHighlightRecursive(FileTreeNode node)
    {
        node.IsSearchMatch = false;

        foreach (var child in node.Children)
        {
            ClearSearchHighlightRecursive(child);
        }
    }
}
