using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AIContextPacker.Models;

namespace AIContextPacker.Services.Interfaces;

/// <summary>
/// Represents options for file content search operations.
/// </summary>
public record SearchOptions
{
    /// <summary>
    /// Gets the search term to find in file contents.
    /// </summary>
    public required string SearchTerm { get; init; }

    /// <summary>
    /// Gets a value indicating whether the search should be case-sensitive.
    /// </summary>
    public bool IsCaseSensitive { get; init; }

    /// <summary>
    /// Gets a value indicating whether the search term should be treated as a regular expression.
    /// </summary>
    public bool UseRegex { get; init; }

    /// <summary>
    /// Gets a value indicating whether the search should match whole words only.
    /// </summary>
    public bool MatchWholeWord { get; init; }
}

/// <summary>
/// Represents the result of a file content search operation.
/// </summary>
public record SearchResult
{
    /// <summary>
    /// Gets the number of files that were searched.
    /// </summary>
    public int FilesSearched { get; init; }

    /// <summary>
    /// Gets the number of files that contained matches.
    /// </summary>
    public int FilesMatched { get; init; }

    /// <summary>
    /// Gets the list of file nodes that matched the search criteria.
    /// </summary>
    public List<FileTreeNode> MatchedNodes { get; init; } = new();
}

/// <summary>
/// Service responsible for searching file contents in the project tree.
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// Searches for content in visible files within the file tree.
    /// </summary>
    /// <param name="rootNode">The root node of the file tree to search.</param>
    /// <param name="options">Search options including term and matching rules.</param>
    /// <param name="cancellationToken">Token to cancel the search operation.</param>
    /// <returns>A task that represents the asynchronous search operation, containing the search results.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when rootNode or options is null.</exception>
    /// <exception cref="System.OperationCanceledException">Thrown when the operation is cancelled.</exception>
    Task<SearchResult> SearchInFileContentAsync(
        FileTreeNode rootNode, 
        SearchOptions options, 
        CancellationToken cancellationToken);

    /// <summary>
    /// Clears search highlight state from all nodes in the tree.
    /// </summary>
    /// <param name="rootNode">The root node of the file tree.</param>
    void ClearSearchHighlight(FileTreeNode rootNode);
}
