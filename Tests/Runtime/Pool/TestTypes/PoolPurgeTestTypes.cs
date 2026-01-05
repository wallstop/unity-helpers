// MIT License - Copyright (c) 2026 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Runtime.Pool.TestTypes
{
    using WallstopStudios.UnityHelpers.Utils;

    /// <summary>
    /// A custom type without any built-in purge defaults for testing fallback to global defaults.
    /// </summary>
    internal sealed class CustomTypeWithoutDefaults
    {
        public int Value { get; set; }
    }

    /// <summary>
    /// A type decorated with <see cref="PoolPurgePolicyAttribute"/> for testing attribute-based configuration.
    /// Uses constructor: (bool enabled, float idleTimeoutSeconds, int minRetainCount).
    /// </summary>
    [PoolPurgePolicy(true, 999f, 42)]
    internal sealed class TypeWithPurgePolicyAttribute
    {
        public int Value { get; set; }
    }
}
