namespace WallstopStudios.UnityHelpers.Tests.Random
{
    using WallstopStudios.UnityHelpers.Core.Random;

    public sealed class GroundZeroRandomTests : RandomTestBase
    {
        protected override IRandom NewRandom()
        {
            return new GroundZeroRandom();
        }
    }
}
