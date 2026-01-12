// MIT License - Copyright (c) 2026 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

// Safe event invocation - never let subscriber exceptions crash the publisher

namespace WallstopStudios.UnityHelpers.Examples
{
    using System;
    using UnityEngine;

    public sealed class SafeEventInvocationExamples
    {
        public event Action<int> OnValueChanged;
        public event Action OnEvent;

        // Safe single-cast event invocation
        public void RaiseValueChanged(int newValue)
        {
            if (OnValueChanged == null)
            {
                return;
            }

            // Copy delegate to avoid race conditions
            Action<int> handler = OnValueChanged;

            try
            {
                handler.Invoke(newValue);
            }
            catch (Exception ex)
            {
                // Never let subscriber exceptions crash the publisher
                Debug.LogError(
                    $"[{nameof(SafeEventInvocationExamples)}] Exception in OnValueChanged handler: {ex}"
                );
            }
        }

        // Safe multi-cast delegate invocation - calls each handler individually
        public void RaiseEvent()
        {
            Delegate[] handlers = OnEvent?.GetInvocationList();
            if (handlers == null)
            {
                return;
            }

            for (int i = 0; i < handlers.Length; i++)
            {
                try
                {
                    ((Action)handlers[i]).Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError(
                        $"[{nameof(SafeEventInvocationExamples)}] Exception in event handler: {ex}"
                    );
                }
            }
        }
    }
}
