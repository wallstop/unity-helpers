namespace UnityHelpers.Tests.Random
{
    using UnityHelpers.Core.Random;

    public sealed class XorShiftRandomTests : RandomTestBase
    {
        protected override IRandom NewRandom() => new XorShiftRandom();
    }
}
