namespace UnityHelpers.Tests.Random
{
    using System;
    using UnityHelpers.Core.Random;

    public sealed class SystemRandomTests : RandomTestBase<SystemRandom>
    {
        protected override SystemRandom NewRandom() =>
            new SystemRandom(Guid.NewGuid().GetHashCode());
    }
}
