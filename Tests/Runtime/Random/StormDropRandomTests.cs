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
