namespace WallstopStudios.UnityHelpers.Tests.Random
{
    using WallstopStudios.UnityHelpers.Core.Random;

    public sealed class XoroShiroEnhancedRandomTests : RandomTestBase
    {
        protected override IRandom NewRandom()
        {
            return new XoroShiroEnhancedRandom();
        }
    }
}
