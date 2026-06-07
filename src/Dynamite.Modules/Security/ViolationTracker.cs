// src/Dynamite.Modules/Security/ViolationTracker.cs
namespace Dynamite.Modules.Security;

using System.Collections.Concurrent;

/// <summary>
/// In-memory tracker for message rates and violation counts.
/// Singleton — all state lives here, resets on bot restart.
/// Thread-safe via ConcurrentDictionary.
/// </summary>
public class ViolationTracker
{
    // Sliding window: (guildId, userId) → timestamps of recent messages
    private readonly ConcurrentDictionary<(ulong, ulong), Queue<DateTime>> _messageWindows = new();

    // Violation count: (guildId, userId) → number of violations
    private readonly ConcurrentDictionary<(ulong, ulong), int> _violations = new();

    // Raid tracking: guildId → timestamps of recent joins
    private readonly ConcurrentDictionary<ulong, Queue<DateTime>> _joinWindows = new();

    // Cleanup timer — remove stale entries every 5 minutes
    private readonly Timer _cleanupTimer;

    public ViolationTracker()
    {
        _cleanupTimer = new Timer(_ => Cleanup(), null,
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// Records a message and returns how many messages this user sent
    /// within the given window. Thread-safe.
    /// </summary>
    public int RecordMessage(ulong guildId, ulong userId, int windowSeconds)
    {
        var key = (guildId, userId);
        var now = DateTime.UtcNow;
        var cutoff = now.AddSeconds(-windowSeconds);

        var queue = _messageWindows.GetOrAdd(key, _ => new Queue<DateTime>());

        lock (queue)
        {
            // Remove messages outside the window
            while (queue.Count > 0 && queue.Peek() < cutoff)
                queue.Dequeue();

            queue.Enqueue(now);
            return queue.Count;
        }
    }

    /// <summary>
    /// Increments violation count and returns the new total.
    /// </summary>
    public int IncrementViolation(ulong guildId, ulong userId)
        => _violations.AddOrUpdate((guildId, userId), 1, (_, old) => old + 1);

    public int GetViolationCount(ulong guildId, ulong userId)
        => _violations.TryGetValue((guildId, userId), out var count) ? count : 0;

    public void ResetViolations(ulong guildId, ulong userId)
        => _violations.TryRemove((guildId, userId), out _);

    /// <summary>
    /// Records a join event and returns how many joins occurred
    /// within the last 10 seconds for this guild.
    /// </summary>
    public int RecordJoin(ulong guildId)
    {
        var now = DateTime.UtcNow;
        var cutoff = now.AddSeconds(-10);

        var queue = _joinWindows.GetOrAdd(guildId, _ => new Queue<DateTime>());

        lock (queue)
        {
            while (queue.Count > 0 && queue.Peek() < cutoff)
                queue.Dequeue();

            queue.Enqueue(now);
            return queue.Count;
        }
    }

    private void Cleanup()
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-2);

        foreach (var key in _messageWindows.Keys)
        {
            if (_messageWindows.TryGetValue(key, out var queue))
            {
                lock (queue)
                {
                    if (queue.Count == 0 || queue.Peek() < cutoff)
                        _messageWindows.TryRemove(key, out _);
                }
            }
        }

        foreach (var key in _joinWindows.Keys)
        {
            if (_joinWindows.TryGetValue(key, out var queue))
            {
                lock (queue)
                {
                    if (queue.Count == 0 || queue.Peek() < cutoff)
                        _joinWindows.TryRemove(key, out _);
                }
            }
        }
    }
}