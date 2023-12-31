namespace UnityHelpers.Tests
{
    using System;
    using Core.Random;

    public sealed class SystemRandomTests : RandomTestBase
    {
        protected override IRandom NewRandom() => new SystemRandom(Guid.NewGuid().GetHashCode());
    }
}
