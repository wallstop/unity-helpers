namespace UnityHelpers.Tests.Random
{
    using UnityHelpers.Core.Random;

    public sealed class UnityRandomTests : RandomTestBase
    {
        protected override IRandom NewRandom() => UnityRandom.Instance;
    }
}
