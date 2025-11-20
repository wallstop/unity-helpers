namespace WallstopStudios.UnityHelpers.Tests.Random
{
    using WallstopStudios.UnityHelpers.Core.Random;

    public sealed class FlurryBurstRandomTests : RandomTestBase
    {
        protected override IRandom NewRandom()
        {
            return new FlurryBurstRandom(DeterministicGuid);
        }
    }
}
