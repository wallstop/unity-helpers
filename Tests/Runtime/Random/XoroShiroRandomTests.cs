// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Runtime.Random
{
    using WallstopStudios.UnityHelpers.Core.Random;

    public sealed class XoroShiroRandomTests : RandomTestBase
    {
        protected override IRandom NewRandom() =>
            new XoroShiroRandom(DeterministicSeed64, DeterministicSeed64B);
    }
}
