// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Helper
{
#if UNITY_EDITOR
    using System;

    public readonly struct PromptScope : IDisposable
    {
        private readonly Action _dispose;

        private PromptScope(Action dispose)
        {
            _dispose = dispose;
        }

        public void Dispose()
        {
            _dispose?.Invoke();
        }

        public static PromptScope Suppress(Func<bool> getter, Action<bool> setter)
        {
            bool prev = getter();
            setter(true);
            return new PromptScope(() => setter(prev));
        }
    }
#endif
}
