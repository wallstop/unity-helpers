// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Runtime.Random
{
    using WallstopStudios.UnityHelpers.Core.Random;

    public sealed class StormDropRandomTests : RandomTestBase
    {
        protected override IRandom NewRandom()
        {
            return new StormDropRandom(DeterministicSeed32);
        }
    }
}
