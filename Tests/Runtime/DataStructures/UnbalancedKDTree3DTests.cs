namespace WallstopStudios.UnityHelpers.Tests.DataStructures
{
    using NUnit.Framework;

    [TestFixture]
    public sealed class UnbalancedKDTree3DTests : KDTree3DTestsBase
    {
        protected override bool IsBalanced => false;
    }
}
