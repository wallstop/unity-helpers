namespace UnityHelpers.Tests.Random
{
    using Core.Random;

    public sealed class XorShiroRandomTests : RandomTestBase
    {
        protected override IRandom NewRandom() => new XorShiroRandom();
    }
}
