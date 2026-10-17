namespace Tickify.Tests.Helpers;

/// <summary>Lets several concurrent callers reach the same point before any of them continues (to force a race).</summary>
public sealed class Rendezvous
{
    private readonly int _parties;
    private readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _arrived;

    public Rendezvous(int parties) => _parties = parties;

    public async Task ArriveAsync()
    {
        if (Interlocked.Increment(ref _arrived) == _parties)
            _released.TrySetResult();

        await _released.Task.WaitAsync(TimeSpan.FromSeconds(15));
    }
}
