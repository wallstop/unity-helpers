namespace UnityHelpers.Core.Helper
{
    using System;
    using System.Collections.Concurrent;
    using UnityEngine;
    using Utils;

    public sealed class UnityMainThreadDispatcher : RuntimeSingleton<UnityMainThreadDispatcher>
    {
        private readonly ConcurrentQueue<Action> _actions = new();

        public void RunOnMainThread(Action action)
        {
            _actions.Enqueue(action);
        }

        private void Update()
        {
            while (_actions.TryDequeue(out Action action))
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    Debug.LogException(e, this);
                }
            }
        }
    }
}
