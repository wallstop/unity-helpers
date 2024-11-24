namespace UnityHelpers.Tests.Random
{
    using UnityHelpers.Core.Random;

    public sealed class XorShiftRandomTests : RandomTestBase<XorShiftRandom>
    {
        protected override XorShiftRandom NewRandom() => new XorShiftRandom();
    }
}
