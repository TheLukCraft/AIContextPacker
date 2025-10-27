using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public int NodesSearched { get; set; }
}

/// <summary>
/// Provides file and folder name search functionality for the project tree.
/// </summary>
public class SearchService : ISearchService
{
    private readonly ILogger<SearchService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchService"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public SearchService(ILogger<SearchService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Searches for files and folders by name within the file tree.
    /// </summary>
    public async Task<SearchResult> SearchAsync(
        FileTreeNode rootNode,
        SearchOptions options,
        CancellationToken cancellationToken)
    {
        if (rootNode == null)
            throw new ArgumentNullException(nameof(rootNode));
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        _logger.LogInformation(
            "Starting name search: Term='{SearchTerm}', CaseSensitive={CaseSensitive}, Regex={UseRegex}, WholeWord={WholeWord}",
            options.SearchTerm, options.IsCaseSensitive, options.UseRegex, options.MatchWholeWord);

        var stopwatch = Stopwatch.StartNew();
        var matchedNodes = new List<FileTreeNode>();
        var searchState = new SearchState();

        try
        {
            await Task.Run(() =>
                SearchRecursive(rootNode, options, matchedNodes, searchState, cancellationToken),
                cancellationToken).ConfigureAwait(false);

            stopwatch.Stop();

            var result = new SearchResult
            {
                FilesSearched = searchState.NodesSearched,
                FilesMatched = matchedNodes.Count,
                MatchedNodes = matchedNodes
            };

            _logger.LogInformation(
                "Search completed in {ElapsedMs}ms: {NodesSearched} nodes searched, {MatchesFound} matches found",
                stopwatch.ElapsedMilliseconds, result.FilesSearched, result.FilesMatched);

            return result;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogInformation(
                "Search cancelled after {ElapsedMs}ms: {NodesSearched} nodes searched, {MatchesFound} matches found before cancellation",
                stopwatch.ElapsedMilliseconds, searchState.NodesSearched, matchedNodes.Count);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Search failed after {ElapsedMs}ms: {NodesSearched} nodes searched",
                stopwatch.ElapsedMilliseconds, searchState.NodesSearched);
            throw new AIContextPackerException(
                $"File name search failed: {ex.Message}",
                innerException: ex);
        }
    }

    /// <summary>
    /// Recursively searches through the file tree for matching names.
    /// </summary>
    private void SearchRecursive(
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

        searchState.NodesSearched++;

        // Check if node name matches
        if (IsNameMatch(node.Name, options))
        {
            matchedNodes.Add(node);
            _logger.LogDebug("Match found: {NodeName} ({Type})", 
                node.Name, node.IsDirectory ? "folder" : "file");
        }

        // Recursively search children
        foreach (var child in node.Children)
        {
            SearchRecursive(child, options, matchedNodes, searchState, cancellationToken);
        }
    }

    /// <summary>
    /// Determines if a node name matches the search criteria.
    /// </summary>
    private bool IsNameMatch(string nodeName, SearchOptions options)
    {
        if (string.IsNullOrEmpty(nodeName))
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
                    ? $@"^{Regex.Escape(options.SearchTerm)}$"
                    : options.SearchTerm;

                return Regex.IsMatch(nodeName, pattern, regexOptions);
            }
            else if (options.MatchWholeWord)
            {
                // Exact match for whole word (entire name)
                var comparison = options.IsCaseSensitive
                    ? StringComparison.Ordinal
                    : StringComparison.OrdinalIgnoreCase;

                return nodeName.Equals(options.SearchTerm, comparison);
            }
            else
            {
                // Simple string contains
                var comparison = options.IsCaseSensitive
                    ? StringComparison.Ordinal
                    : StringComparison.OrdinalIgnoreCase;

                return nodeName.Contains(options.SearchTerm, comparison);
            }
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid regex pattern: {SearchTerm}", options.SearchTerm);
            return false;
        }
    }
}
