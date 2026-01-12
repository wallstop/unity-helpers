// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.DataStructures
{
    using NUnit.Framework;

    [TestFixture]
    [NUnit.Framework.Category("Fast")]
    public sealed class BalancedKDTree3DTests : KDTree3DTestsBase
    {
        protected override bool IsBalanced => true;
    }
}
