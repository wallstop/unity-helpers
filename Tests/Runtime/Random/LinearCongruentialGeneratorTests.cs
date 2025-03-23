namespace UnityHelpers.Tests.Random
{
    using Core.Random;

    public sealed class LinearCongruentialGeneratorTests : RandomTestBase
    {
        protected override IRandom NewRandom()
        {
            return new LinearCongruentialGenerator();
        }
    }
}
