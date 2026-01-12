// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Editor.Internal
{
    using System;

    internal static class InlineInspectorContext
    {
        [ThreadStatic]
        private static int _scopeDepth;

        public static bool IsActive => _scopeDepth > 0;

        public static IDisposable Enter()
        {
            _scopeDepth++;
            return new Scope();
        }

        private sealed class Scope : IDisposable
        {
            private bool _disposed;

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                if (_scopeDepth > 0)
                {
                    _scopeDepth--;
                }
            }
        }
    }
}
