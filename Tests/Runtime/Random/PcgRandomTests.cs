namespace WallstopStudios.UnityHelpers.Tests.Random
{
    using WallstopStudios.UnityHelpers.Core.Random;

    public sealed class PcgRandomTests : RandomTestBase
    {
        protected override IRandom NewRandom() => new PcgRandom();
    }
}
