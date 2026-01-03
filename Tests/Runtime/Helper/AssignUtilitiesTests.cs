// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Helper
{
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Tests.Core;

    public sealed class AssignUtilitiesTests : CommonTestBase
    {
        [Test]
        public void ExchangeReplacesValueAndReturnsOriginal()
        {
            int current = 5;
            int result = AssignUtilities.Exchange(ref current, 42);

            Assert.AreEqual(5, result);
            Assert.AreEqual(42, current);
        }

        [Test]
        public void ExchangeWorksWithReferenceTypes()
        {
            string value = "initial";
            string changed = AssignUtilities.Exchange(ref value, "updated");

            Assert.AreEqual("initial", changed);
            Assert.AreEqual("updated", value);
        }

        [Test]
        public void ExchangeWorksWithSelf()
        {
            string value = "initial";
            string changed = AssignUtilities.Exchange(ref value, value);

            Assert.AreEqual("initial", changed);
            Assert.AreEqual("initial", value);
        }
    }
}
