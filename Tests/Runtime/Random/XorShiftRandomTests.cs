namespace WallstopStudios.UnityHelpers.Tests.Random
{
    using WallstopStudios.UnityHelpers.Core.Random;

    public sealed class XorShiftRandomTests : RandomTestBase
    {
        protected override IRandom NewRandom() => new XorShiftRandom();
    }
}
