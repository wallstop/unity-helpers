namespace WallstopStudios.UnityHelpers.Tests.Random
{
    using WallstopStudios.UnityHelpers.Core.Random;

    public sealed class WaveSplatRandomTests : RandomTestBase
    {
        protected override IRandom NewRandom()
        {
            return new WaveSplatRandom();
        }
    }
}
