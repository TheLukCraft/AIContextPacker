using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace AIContextPacker.Helpers;

/// <summary>
/// Provides debouncing functionality for delaying execution of actions.
/// Used to prevent excessive operations during rapid user input.
/// </summary>
public class Debouncer
{
    private readonly TimeSpan _delay;
    private CancellationTokenSource? _cts;
    private readonly Dispatcher _dispatcher;

    /// <summary>
    /// Initializes a new instance of the <see cref="Debouncer"/> class.
    /// </summary>
    /// <param name="delay">The delay duration before executing the action.</param>
    /// <param name="dispatcher">Optional dispatcher for UI thread synchronization. If null, uses current dispatcher.</param>
    public Debouncer(TimeSpan delay, Dispatcher? dispatcher = null)
    {
        _delay = delay;
        _dispatcher = dispatcher ?? Dispatcher.CurrentDispatcher;
    }

    /// <summary>
    /// Debounces an asynchronous action. Each call cancels the previous pending action.
    /// </summary>
    /// <param name="action">The asynchronous action to execute after the delay.</param>
    public void Debounce(Func<Task> action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        // Cancel previous pending action
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        // Schedule new action
        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(_delay, token).ConfigureAwait(false);

                if (!token.IsCancellationRequested)
                {
                    // Execute action on UI thread if dispatcher is available
                    await _dispatcher.InvokeAsync(async () =>
                    {
                        if (!token.IsCancellationRequested)
                        {
                            await action().ConfigureAwait(false);
                        }
                    });
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when debounce is cancelled
            }
        }, token);
    }

    /// <summary>
    /// Cancels any pending debounced action.
    /// </summary>
    public void Cancel()
    {
        _cts?.Cancel();
    }
}
