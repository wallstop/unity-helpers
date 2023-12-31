namespace Tests.Runtime.Scripts
{
    using UnityHelpers.Core.Random;
    using UnityHelpers.Tests;

    public sealed class XorShiftRandomTests : RandomTestBase
    {
        protected override IRandom NewRandom() => new XorShiftRandom();
    }
}
