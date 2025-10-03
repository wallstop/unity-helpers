namespace WallstopStudios.UnityHelpers.Tests.DataStructures
{
    using NUnit.Framework;

    [TestFixture]
    public sealed class BalancedKDTree3DTests : KDTree3DTestsBase
    {
        protected override bool IsBalanced => true;
    }
}
