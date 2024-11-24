namespace UnityHelpers.Tests.Random
{
    using UnityHelpers.Core.Random;

    public sealed class UnityRandomTests : RandomTestBase<UnityRandom>
    {
        protected override UnityRandom NewRandom() => UnityRandom.Instance;
    }
}
