using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ImageChecker.Concurrent
{
    /// <summary>
    /// Implementation of PauseTokenSource pattern based on the blog post: 
    /// http://blogs.msdn.com/b/pfxteam/archive/2013/01/13/cooperatively-pausing-async-methods.aspx 
    /// </summary>
    public class PauseTokenSource
    {

        private TaskCompletionSource<bool> m_paused;
        internal static readonly Task s_completedTask = Task.FromResult(true);

        private Stopwatch timer = new Stopwatch();
        private List<TimeSpan> pauses;
        public List<TimeSpan> Pauses
        {
            get { if (pauses == null) pauses = new List<TimeSpan>(); return pauses; }
        }


        public bool IsPauseRequested { get { return m_paused != null; } }

        public void Pause()
        {
            timer.Restart();
            if (!IsPauseRequested)
            {
                Interlocked.CompareExchange(
                                ref m_paused, new TaskCompletionSource<bool>(), null); 
            }
        }

        public void Unpause()
        {
            timer.Stop();
            Pauses.Add(timer.Elapsed);

            if (IsPauseRequested)
            {
                while (true)
                {
                    var tcs = m_paused;
                    if (tcs == null) return;
                    if (Interlocked.CompareExchange(ref m_paused, null, tcs) == tcs)
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
            var cur = m_paused;
            return cur != null ? cur.Task : s_completedTask;
        }
    }

    public struct PauseToken
    {
        private readonly PauseTokenSource m_source;
        internal PauseToken(PauseTokenSource source) { m_source = source; }

        public bool IsPausedRequested { get { return m_source != null && m_source.IsPauseRequested; } }

        public Task WaitWhilePausedAsync()
        {
            return IsPausedRequested ?
                m_source.WaitWhilePausedAsync() :
                PauseTokenSource.s_completedTask;
        }
    }
}
