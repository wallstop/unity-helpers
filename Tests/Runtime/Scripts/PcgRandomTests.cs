namespace UnityHelpers.Tests
{
    using Core.Random;

    public sealed class PcgRandomTests : RandomTestBase
    {
        protected override IRandom NewRandom() => new PcgRandom();
    }
}
