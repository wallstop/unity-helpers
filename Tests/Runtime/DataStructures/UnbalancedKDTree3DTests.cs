// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.DataStructures
{
    using NUnit.Framework;

    [TestFixture]
    public sealed class UnbalancedKDTree3DTests : KDTree3DTestsBase
    {
        protected override bool IsBalanced => false;
    }
}
