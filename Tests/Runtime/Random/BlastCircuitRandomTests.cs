namespace WallstopStudios.UnityHelpers.Tests.Random
{
    using WallstopStudios.UnityHelpers.Core.Random;

    public sealed class BlastCircuitRandomTests : RandomTestBase
    {
        protected override IRandom NewRandom()
        {
            return new BlastCircuitRandom(DeterministicSeed64);
        }
    }
}
