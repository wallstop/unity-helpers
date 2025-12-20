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
            private readonly float _leftPadding;
            private readonly float _rightPadding;
            private readonly bool _trackScopeDepth;
            private bool _disposed;

            internal WidthPaddingScope(float horizontalPadding)
            {
                float resolved = Mathf.Max(0f, horizontalPadding);
                float split = resolved * 0.5f;
                _padding = resolved;
                _leftPadding = split;
                _rightPadding = resolved - split;
                _trackScopeDepth = true;

                _scopeDepth++;

                if (_padding <= 0f)
                {
                    return;
                }

                _totalPadding += _padding;
                _totalLeftPadding += _leftPadding;
                _totalRightPadding += _rightPadding;
            }

            internal WidthPaddingScope(
                float horizontalPadding,
                float leftPadding,
                float rightPadding
            )
            {
                _leftPadding = Mathf.Max(0f, leftPadding);
                _rightPadding = Mathf.Max(0f, rightPadding);
                float combined = Mathf.Max(0f, horizontalPadding);
                _trackScopeDepth = true;
                if (combined <= 0f)
                {
                    combined = _leftPadding + _rightPadding;
                }

                if (combined <= 0f)
                {
                    _padding = 0f;
                    _leftPadding = 0f;
                    _rightPadding = 0f;
                    _scopeDepth++;
                    return;
                }

                _padding = combined;
                float resolvedLeft = _leftPadding;
                float resolvedRight = _rightPadding;
                if (resolvedLeft <= 0f && resolvedRight <= 0f)
                {
                    float split = combined * 0.5f;
                    resolvedLeft = split;
                    resolvedRight = combined - split;
                }

                _leftPadding = resolvedLeft;
                _rightPadding = resolvedRight;

                _scopeDepth++;
                _totalPadding += _padding;
                _totalLeftPadding += _leftPadding;
                _totalRightPadding += _rightPadding;
            }

            public void Dispose()
            {
                if (_disposed || !_trackScopeDepth)
                {
                    return;
                }

                _disposed = true;

                if (_padding > 0f || _leftPadding > 0f || _rightPadding > 0f)
                {
                    _totalPadding = Mathf.Max(0f, _totalPadding - _padding);
                    _totalLeftPadding = Mathf.Max(0f, _totalLeftPadding - _leftPadding);
                    _totalRightPadding = Mathf.Max(0f, _totalRightPadding - _rightPadding);
                }

                _scopeDepth = Mathf.Max(0, _scopeDepth - 1);
            }
        }

        private static float _totalPadding;
        private static float _totalLeftPadding;
        private static float _totalRightPadding;
        private static int _scopeDepth;

        internal static float CurrentHorizontalPadding => _totalPadding;
        internal static float CurrentLeftPadding => _totalLeftPadding;
        internal static float CurrentRightPadding => _totalRightPadding;
        internal static int CurrentScopeDepth => _scopeDepth;

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        internal static void ResetForTests()
        {
            _totalPadding = 0f;
            _totalLeftPadding = 0f;
            _totalRightPadding = 0f;
            _scopeDepth = 0;
        }

        internal static IDisposable PushContentPadding(float horizontalPadding)
        {
            return new WidthPaddingScope(horizontalPadding);
        }

        internal static IDisposable PushContentPadding(
            float horizontalPadding,
            float leftPadding,
            float rightPadding
        )
        {
            return new WidthPaddingScope(horizontalPadding, leftPadding, rightPadding);
        }

        internal static Rect ApplyCurrentPadding(Rect rect)
        {
            float leftPadding = _totalLeftPadding;
            float rightPadding = _totalRightPadding;
            if (leftPadding <= 0f && rightPadding <= 0f)
            {
                return rect;
            }

            Rect adjusted = rect;
            adjusted.xMin += leftPadding;
            adjusted.xMax -= rightPadding;
            if (adjusted.width < 0f || float.IsNaN(adjusted.width))
            {
                adjusted.width = 0f;
            }

            return adjusted;
        }

        internal static float CalculateHorizontalPadding(GUIStyle containerStyle)
        {
            return CalculateHorizontalPadding(containerStyle, out _, out _);
        }

        internal static float CalculateHorizontalPadding(
            GUIStyle containerStyle,
            out float leftPadding,
            out float rightPadding
        )
        {
            leftPadding = 0f;
            rightPadding = 0f;
            if (containerStyle == null)
            {
                return 0f;
            }

            RectOffset padding = containerStyle.padding;
            if (padding == null)
            {
                return 0f;
            }

            leftPadding = Mathf.Max(0f, padding.left);
            rightPadding = Mathf.Max(0f, padding.right);
            int total = padding.left + padding.right;
            return Mathf.Max(0f, total);
        }
    }
#endif
}
