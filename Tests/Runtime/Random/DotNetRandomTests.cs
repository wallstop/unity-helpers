namespace UnityHelpers.Tests.Random
{
    using Core.Random;

    public sealed class DotNetRandomTests : RandomTestBase<DotNetRandom>
    {
        protected override DotNetRandom NewRandom() => new DotNetRandom();
    }
}
