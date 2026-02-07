// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Utils
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Non-generic registry to manage ScriptableObjectSingleton instance clearing.
    /// This class exists to work around Unity 6.3's restriction on
    /// [RuntimeInitializeOnLoadMethod] in generic classes.
    /// </summary>
    internal static class ScriptableObjectSingletonRegistry
    {
        private static readonly HashSet<Action> _clearActions = new();

        /// <summary>
        /// Registers a clear action for a singleton type.
        /// </summary>
        internal static void Register(Action clearAction)
        {
            if (clearAction == null)
            {
                return;
            }

            lock (_clearActions)
            {
                _clearActions.Add(clearAction);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ClearAllInstances()
        {
            lock (_clearActions)
            {
                foreach (Action clearAction in _clearActions)
                {
                    try
                    {
                        clearAction.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }
            }
        }
    }
}
