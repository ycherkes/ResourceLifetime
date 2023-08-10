using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ResourceLifetime.Disposables;

public sealed class DisposableGroup : IAsyncDisposable, IDisposable, IEnumerable
{
    private readonly bool _throwExceptions;
    private bool _disposed;
    private readonly List<object> _disposables = new();

    public DisposableGroup(bool throwExceptions = false)
    {
        _throwExceptions = throwExceptions;
    }

    public void Add(IDisposable disposable)
    {
        if (disposable == null)
        {
            throw new ArgumentNullException(nameof(disposable));
        }

        AddCore(disposable);
    }

    public void Add(IAsyncDisposable asyncDisposable)
    {
        if (asyncDisposable == null)
        {
            throw new ArgumentNullException(nameof(asyncDisposable));
        }

        AddCore(asyncDisposable);
    }

    public void Add<T>(T disposable) where T : IDisposable, IAsyncDisposable
    {
        if (disposable == null)
        {
            throw new ArgumentNullException(nameof(disposable));
        }

        AddCore(disposable);
    }
    
    private void AddCore(object disposable)
    {
        lock (_disposables)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(typeof(DisposableGroup).FullName);
            }

            _disposables.Add(disposable);
        }
    }

    // Copied from the source code of Microsoft.Extensions.DependencyInjection
    // https://github.com/dotnet/runtime/blob/219392ee65562aebaeee9cb27a60a6536eb60ef7/src/libraries/Microsoft.Extensions.DependencyInjection/src/ServiceLookup/ServiceProviderEngineScope.cs#L120
    public void Dispose()
    {
        var toDispose = BeginDispose();

        if (toDispose == null)
        {
            return;
        }

        for (var i = toDispose.Count - 1; i >= 0; i--)
        {
            if (toDispose[i] is IDisposable disposable)
            {
                disposable.Dispose();
            }
            else if(_throwExceptions)
            {
                throw new InvalidOperationException($"{toDispose[i].GetType().FullName}' type only implements IAsyncDisposable. Use DisposeAsync to dispose.");
            }
        }
    }

    public ValueTask DisposeAsync()
    {
        var toDispose = BeginDispose();

        if (toDispose == null)
        {
            return default;
        }

        try
        {
            for (var i = toDispose.Count - 1; i >= 0; i--)
            {
                var disposable = toDispose[i];
                if (disposable is IAsyncDisposable asyncDisposable)
                {
                    var vt = asyncDisposable.DisposeAsync();
                    if (!vt.IsCompletedSuccessfully)
                    {
                        return Await(i, vt, toDispose);
                    }

                    // If its a IValueTaskSource backed ValueTask,
                    // inform it its result has been read so it can reset
                    vt.GetAwaiter().GetResult();
                }
                else
                {
                    ((IDisposable)disposable).Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            return new ValueTask(Task.FromException(ex));
        }

        return default;

        static async ValueTask Await(int i, ValueTask vt, IReadOnlyList<object> toDispose)
        {
            await vt.ConfigureAwait(false);
            // vt is acting on the disposable at index i,
            // decrement it and move to the next iteration
            i--;

            for (; i >= 0; i--)
            {
                var disposable = toDispose[i];
                if (disposable is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                }
                else
                {
                    ((IDisposable)disposable).Dispose();
                }
            }
        }
    }

    private List<object>? BeginDispose()
    {
        lock (_disposables)
        {
            if (_disposed)
            {
                return null;
            }

            _disposed = true;
        }

        return _disposables;
    }

    public IEnumerator GetEnumerator()
    {
        return _disposables.GetEnumerator();
    }
}
