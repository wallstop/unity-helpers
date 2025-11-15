namespace WallstopStudios.UnityHelpers.Editor.Utils
{
#if UNITY_EDITOR
    using System;
    using UnityEngine;

    internal static class GroupGUIWidthUtility
    {
        private sealed class WidthPaddingScope : IDisposable
        {
            private readonly float _padding;
            private bool _disposed;

            internal WidthPaddingScope(float padding)
            {
                _padding = Mathf.Max(0f, padding);
                if (_padding <= 0f)
                {
                    return;
                }

                _totalPadding += _padding;
            }

            public void Dispose()
            {
                if (_disposed || _padding <= 0f)
                {
                    return;
                }

                _disposed = true;
                _totalPadding = Mathf.Max(0f, _totalPadding - _padding);
            }
        }

        private static float _totalPadding;

        internal static float CurrentHorizontalPadding => _totalPadding;

        internal static IDisposable PushContentPadding(float horizontalPadding)
        {
            return new WidthPaddingScope(horizontalPadding);
        }

        internal static float CalculateHorizontalPadding(GUIStyle containerStyle)
        {
            if (containerStyle == null)
            {
                return 0f;
            }

            RectOffset padding = containerStyle.padding;
            if (padding == null)
            {
                return 0f;
            }

            int total = padding.left + padding.right;
            return Mathf.Max(0f, total);
        }
    }
#endif
}
