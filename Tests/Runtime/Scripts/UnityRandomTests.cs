namespace UnityHelpers.Tests
{
    using Core.Random;

    public sealed class UnityRandomTests : RandomTestBase
    {
        protected override IRandom NewRandom() => UnityRandom.Instance;
    }
}
