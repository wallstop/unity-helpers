namespace WallstopStudios.UnityHelpers.Tests.Random
{
    using WallstopStudios.UnityHelpers.Core.Random;

    public sealed class LinearCongruentialGeneratorTests : RandomTestBase
    {
        protected override IRandom NewRandom()
        {
            return new LinearCongruentialGenerator();
        }
    }
}
