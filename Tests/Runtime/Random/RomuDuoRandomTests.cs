namespace UnityHelpers.Tests.Random
{
    using Core.Random;

    public sealed class RomuDuoRandomTests : RandomTestBase
    {
        protected override IRandom NewRandom() => new RomuDuo();
    }
}
