// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.DataStructures
{
    using NUnit.Framework;

    [TestFixture]
    public sealed class BalancedKDTree3DTests : KDTree3DTestsBase
    {
        protected override bool IsBalanced => true;
    }
}
