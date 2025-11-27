namespace WallstopStudios.UnityHelpers.Tests.Runtime.Random
{
    using WallstopStudios.UnityHelpers.Core.Random;

    public sealed class WaveSplatRandomTests : RandomTestBase
    {
        protected override IRandom NewRandom()
        {
            return new WaveSplatRandom(DeterministicSeed64);
        }
    }
}
