namespace UnityHelpers.Tests.Random
{
    using UnityHelpers.Core.Random;

    public sealed class PcgRandomTests : RandomTestBase
    {
        protected override IRandom NewRandom() => new PcgRandom();
    }
}
