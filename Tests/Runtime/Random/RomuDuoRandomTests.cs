namespace WallstopStudios.UnityHelpers.Tests.Random
{
    using WallstopStudios.UnityHelpers.Core.Random;

    public sealed class RomuDuoRandomTests : RandomTestBase
    {
        protected override IRandom NewRandom() => new RomuDuo();
    }
}
