using System;
using System.Threading;
using System.Windows;
using AIContextPacker.Services.Interfaces;

namespace AIContextPacker.Services;

/// <summary>
/// Implements progress reporting for WPF applications.
/// Thread-safe and dispatches updates to the UI thread.
/// </summary>
public class ProgressReporter : IProgressReporter
{
    private readonly Action<string, double?> _reportAction;
    private readonly CancellationTokenSource _cts;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressReporter"/> class.
    /// </summary>
    /// <param name="reportAction">Action to invoke when progress is reported. Receives status and percent complete.</param>
    /// <param name="cancellationToken">Optional cancellation token to monitor.</param>
    public ProgressReporter(Action<string, double?> reportAction, CancellationToken cancellationToken = default)
    {
        _reportAction = reportAction ?? throw new ArgumentNullException(nameof(reportAction));
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    }

    /// <inheritdoc/>
    public CancellationToken CancellationToken => _cts.Token;

    /// <inheritdoc/>
    public bool IsCancelled => _cts.Token.IsCancellationRequested;

    /// <inheritdoc/>
    public void Report(string status, double? percentComplete = null)
    {
        if (_cts.Token.IsCancellationRequested)
            return;

        // Ensure we're on the UI thread
        Application.Current?.Dispatcher.Invoke(() =>
        {
            _reportAction(status, percentComplete);
        });
    }

    /// <inheritdoc/>
    public void Clear()
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            _reportAction(string.Empty, null);
        });
    }

    /// <summary>
    /// Requests cancellation of the operation.
    /// </summary>
    public void Cancel()
    {
        _cts.Cancel();
    }

    /// <summary>
    /// Disposes the cancellation token source.
    /// </summary>
    public void Dispose()
    {
        _cts?.Dispose();
    }
}
