namespace WallstopStudios.UnityHelpers.Tests.Runtime.Random
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
