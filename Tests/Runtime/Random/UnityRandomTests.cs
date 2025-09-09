namespace WallstopStudios.UnityHelpers.Tests.Random
{
    using WallstopStudios.UnityHelpers.Core.Random;

    public sealed class UnityRandomTests : RandomTestBase
    {
        protected override IRandom NewRandom() => UnityRandom.Instance;
    }
}
