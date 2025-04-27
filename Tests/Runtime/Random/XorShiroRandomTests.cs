namespace WallstopStudios.UnityHelpers.Tests.Random
{
    using WallstopStudios.UnityHelpers.Core.Random;

    public sealed class XorShiroRandomTests : RandomTestBase
    {
        protected override IRandom NewRandom() => new XorShiroRandom();
    }
}
