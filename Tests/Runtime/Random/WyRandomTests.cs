namespace UnityHelpers.Tests.Random
{
    using Core.Random;

    public sealed class WyRandomTests : RandomTestBase
    {
        protected override IRandom NewRandom() => new WyRandom();
    }
}
