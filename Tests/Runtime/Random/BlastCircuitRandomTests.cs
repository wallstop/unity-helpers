namespace WallstopStudios.UnityHelpers.Tests.Runtime.Random
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
