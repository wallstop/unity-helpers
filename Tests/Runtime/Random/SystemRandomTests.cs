namespace UnityHelpers.Tests.Random
{
    using System;
    using UnityHelpers.Core.Random;

    public sealed class SystemRandomTests : RandomTestBase
    {
        protected override IRandom NewRandom() => new SystemRandom(Guid.NewGuid().GetHashCode());
    }
}
