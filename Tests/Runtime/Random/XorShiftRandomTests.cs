// MIT License - Copyright (c) 2023 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Runtime.Random
{
    using WallstopStudios.UnityHelpers.Core.Random;

    public sealed class XorShiftRandomTests : RandomTestBase
    {
        protected override IRandom NewRandom() => new XorShiftRandom(DeterministicSeedInt);
    }
}
