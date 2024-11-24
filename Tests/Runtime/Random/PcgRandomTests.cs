namespace UnityHelpers.Tests.Random
{
    using UnityHelpers.Core.Random;

    public sealed class PcgRandomTests : RandomTestBase<PcgRandom>
    {
        protected override PcgRandom NewRandom() => new PcgRandom();
    }
}
