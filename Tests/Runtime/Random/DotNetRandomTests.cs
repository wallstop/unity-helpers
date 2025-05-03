namespace WallstopStudios.UnityHelpers.Tests.Random
{
    using WallstopStudios.UnityHelpers.Core.Random;

    public sealed class DotNetRandomTests : RandomTestBase
    {
        protected override IRandom NewRandom() => new DotNetRandom();
    }
}
