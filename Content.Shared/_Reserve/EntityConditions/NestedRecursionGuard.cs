using System;
using System.Collections.Concurrent;

namespace Content.Shared._Reserve.EntityConditions;

public sealed class NestedRecursionGuard
{
    private readonly ConcurrentDictionary<int, int> _depths = new();

    public bool CanEnter(int maxDepth)
    {
        return !_depths.TryGetValue(Environment.CurrentManagedThreadId, out var depth) || depth < maxDepth;
    }

    public void Enter()
    {
        var threadId = Environment.CurrentManagedThreadId;
        _depths.AddOrUpdate(threadId, 1, static (_, depth) => depth + 1);
    }

    public void Exit()
    {
        var threadId = Environment.CurrentManagedThreadId;

        while (_depths.TryGetValue(threadId, out var depth))
        {
            if (depth <= 1)
            {
                if (_depths.TryRemove(threadId, out _))
                    return;

                continue;
            }

            if (_depths.TryUpdate(threadId, depth - 1, depth))
                return;
        }
    }
}
