using System;
using System.Threading;
using System.Threading.Tasks;

namespace ResourceLifetime.Disposables;

/// <summary>
/// Provides a set of static methods for creating <see cref="IDisposable"/> and <see cref="IAsyncDisposable"/> objects.
/// </summary>
public static class Disposable
{
    private sealed class AnonymousDisposable : IDisposable
    {
        private volatile Action? _dispose;
        public AnonymousDisposable(Action dispose)
        {
            _dispose = dispose;
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref _dispose, null)?.Invoke();
        }
    }

    private sealed class AnonymousAsyncDisposable2 : IAsyncDisposable
    {
        private volatile Func<Task>? _dispose;
        public AnonymousAsyncDisposable2(Func<Task> dispose)
        {
            _dispose = dispose;
        }

        public async ValueTask DisposeAsync()
        {
            var result = Interlocked.Exchange(ref _dispose, null)?.Invoke();

            if (result == null)
            {
                return;
            }

            if (!result.IsCompletedSuccessfully)
            {
                await result;
            }

            // If its a IValueTaskSource backed ValueTask,
            // inform it its result has been read so it can reset
            result.GetAwaiter().GetResult();
        }
    }

    private sealed class AnonymousAsyncDisposable3 : IAsyncDisposable
    {
        private volatile Func<ValueTask>? _dispose;
        public AnonymousAsyncDisposable3(Func<ValueTask> dispose)
        {
            _dispose = dispose;
        }

        public async ValueTask DisposeAsync()
        {
            var result = Interlocked.Exchange(ref _dispose, null)?.Invoke();

            if (result == null)
            {
                return;
            }

            if (!result.Value.IsCompletedSuccessfully)
            {
                await result.Value;
            }

            // If its a IValueTaskSource backed ValueTask,
            // inform it its result has been read so it can reset
            result.Value.GetAwaiter().GetResult();
        }
    }

    /// <summary>
    /// Creates a disposable object that invokes the specified action when disposed.
    /// </summary>
    /// <param name="dispose">Action to run during the first call to <see cref="IDisposable.Dispose"/>. The action is guaranteed to be run at most once.</param>
    /// <returns>The disposable object that runs the given action upon disposal.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="dispose"/> is <c>null</c>.</exception>
    public static IDisposable Create(Action dispose)
    {
        if (dispose == null)
        {
            throw new ArgumentNullException(nameof(dispose));
        }

        return new AnonymousDisposable(dispose);
    }

    /// <summary>
    /// Creates an asyncDisposable object that invokes the specified func returning a Task when disposed.
    /// </summary>
    public static IAsyncDisposable Create(Func<Task> dispose)
    {
        if (dispose == null)
        {
            throw new ArgumentNullException(nameof(dispose));
        }

        return new AnonymousAsyncDisposable2(dispose);
    }

    /// <summary>
    /// Creates an asyncDisposable object that invokes the specified func returning a ValueTask when disposed.
    /// </summary>
    public static IAsyncDisposable Create(Func<ValueTask> dispose)
    {
        if (dispose == null)
        {
            throw new ArgumentNullException(nameof(dispose));
        }

        return new AnonymousAsyncDisposable3(dispose);
    }
}