// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using System;
    using System.Linq;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Utils;

    [TestFixture]
    [NUnit.Framework.Category("Fast")]
    public sealed class SerializedStringComparerEdgeTests
    {
        [Test]
        public void GetHashCodeThrowsOnNullForAllModes()
        {
            SerializedStringComparer.StringCompareMode[] allModes = Enum.GetValues(
                    typeof(SerializedStringComparer.StringCompareMode)
                )
                .OfType<SerializedStringComparer.StringCompareMode>()
                .ToArray();

            foreach (SerializedStringComparer.StringCompareMode mode in allModes)
            {
                SerializedStringComparer comparer = new(mode);
                Assert.Throws<ArgumentNullException>(
                    () => comparer.GetHashCode(null),
                    mode.ToString()
                );
            }
        }

        [Test]
        public void EqualsNullHandlingConsistentAcrossModes()
        {
            SerializedStringComparer.StringCompareMode[] allModes = Enum.GetValues(
                    typeof(SerializedStringComparer.StringCompareMode)
                )
                .OfType<SerializedStringComparer.StringCompareMode>()
                .ToArray();

            foreach (SerializedStringComparer.StringCompareMode mode in allModes)
            {
                SerializedStringComparer comparer = new(mode);
                Assert.IsTrue(comparer.Equals(null, null), mode.ToString());
                Assert.IsFalse(comparer.Equals("a", null), mode.ToString());
                Assert.IsFalse(comparer.Equals(null, "a"), mode.ToString());
            }
        }
    }
}
