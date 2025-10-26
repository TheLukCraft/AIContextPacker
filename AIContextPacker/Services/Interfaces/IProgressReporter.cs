using System;
using System.Threading;

namespace AIContextPacker.Services.Interfaces;

/// <summary>
/// Provides progress reporting functionality for long-running operations.
/// </summary>
public interface IProgressReporter
{
    /// <summary>
    /// Reports progress for the current operation.
    /// </summary>
    /// <param name="status">A description of the current operation status.</param>
    /// <param name="percentComplete">The percentage of completion (0-100), or null if unknown.</param>
    void Report(string status, double? percentComplete = null);

    /// <summary>
    /// Gets the cancellation token for the current operation.
    /// </summary>
    CancellationToken CancellationToken { get; }

    /// <summary>
    /// Indicates whether the operation has been cancelled.
    /// </summary>
    bool IsCancelled { get; }

    /// <summary>
    /// Clears the progress status.
    /// </summary>
    void Clear();
}
