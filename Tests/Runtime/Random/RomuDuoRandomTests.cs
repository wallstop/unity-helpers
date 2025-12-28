// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Runtime.Random
{
    using WallstopStudios.UnityHelpers.Core.Random;

    public sealed class RomuDuoRandomTests : RandomTestBase
    {
        protected override IRandom NewRandom() =>
            new RomuDuo(DeterministicSeed64, DeterministicSeed64B);
    }
}
