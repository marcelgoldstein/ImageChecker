using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ImageChecker.Concurrent;

/// <summary>
/// Implementation of PauseTokenSource pattern based on the blog post: 
/// http://blogs.msdn.com/b/pfxteam/archive/2013/01/13/cooperatively-pausing-async-methods.aspx 
/// </summary>
public class PauseTokenSource
{

    private TaskCompletionSource<bool> _paused;
    internal static readonly Task s_completedTask = Task.FromResult(true);

    private readonly Stopwatch _timer = new();
    private List<TimeSpan> _pauses;
    public List<TimeSpan> Pauses
    {
        get { if (_pauses == null) _pauses = new List<TimeSpan>(); return _pauses; }
    }


    public bool IsPauseRequested { get { return _paused != null; } }

    public void Pause()
    {
        _timer.Restart();
        if (!IsPauseRequested)
        {
            Interlocked.CompareExchange(
                            ref _paused, new TaskCompletionSource<bool>(), null);
        }
    }

    public void Unpause()
    {
        _timer.Stop();
        Pauses.Add(_timer.Elapsed);

        if (IsPauseRequested)
        {
            while (true)
            {
                var tcs = _paused;
                if (tcs == null) return;
                if (Interlocked.CompareExchange(ref _paused, null, tcs) == tcs)
                {
                    tcs.SetResult(true);
                    break;
                }
            }
        }
    }

    public PauseToken Token
    {
        get
        {
            return new PauseToken(this);
        }
    }

    internal Task WaitWhilePausedAsync()
    {
        var cur = _paused;
        return cur != null ? cur.Task : s_completedTask;
    }
}

public struct PauseToken
{
    private readonly PauseTokenSource _source;
    internal PauseToken(PauseTokenSource source) { _source = source; }

    public bool IsPausedRequested { get { return _source != null && _source.IsPauseRequested; } }

    public Task WaitWhilePausedAsync()
    {
        return IsPausedRequested ?
            _source.WaitWhilePausedAsync() :
            PauseTokenSource.s_completedTask;
    }
}
