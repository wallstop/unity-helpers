namespace WallstopStudios.UnityHelpers.Tests.Runtime.Random
{
    using WallstopStudios.UnityHelpers.Core.Random;

    public sealed class PcgRandomTests : RandomTestBase
    {
        protected override IRandom NewRandom() => new PcgRandom(DeterministicGuid);
    }
}
