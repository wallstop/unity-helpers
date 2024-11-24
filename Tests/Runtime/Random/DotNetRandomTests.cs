namespace UnityHelpers.Tests.Random
{
    using Core.Random;

    public sealed class DotNetRandomTests : RandomTestBase
    {
        protected override IRandom NewRandom() => new DotNetRandom();
    }
}
