namespace UnityHelpers.Tests.Random
{
    using Core.Random;

    public sealed class WyRandomTests : RandomTestBase<WyRandom>
    {
        protected override WyRandom NewRandom() => new WyRandom();
    }
}
