namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Utils;

    public sealed class SerializedStringComparerTests
    {
        [Test]
        public void EqualsOrdinalModeIsCaseSensitive()
        {
            SerializedStringComparer comparer = new(
                SerializedStringComparer.StringCompareMode.Ordinal
            );

            Assert.IsTrue(comparer.Equals("Alpha", "Alpha"));
            Assert.IsFalse(comparer.Equals("Alpha", "alpha"));
        }

        [Test]
        public void EqualsOrdinalIgnoreCaseModeIsCaseInsensitive()
        {
            SerializedStringComparer comparer = new(
                SerializedStringComparer.StringCompareMode.OrdinalIgnoreCase
            );

            Assert.IsTrue(comparer.Equals("Alpha", "alpha"));
        }

        [Test]
        public void EqualsCurrentCultureRespectsCultureSettings()
        {
            CultureInfo originalCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
                SerializedStringComparer comparer = new(
                    SerializedStringComparer.StringCompareMode.CurrentCultureIgnoreCase
                );

                Assert.IsTrue(comparer.Equals("straße", "STRASSE"));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }

        [Test]
        public void EqualsInvariantCultureMatchesInvariantRules()
        {
            SerializedStringComparer comparer = new(
                SerializedStringComparer.StringCompareMode.InvariantCultureIgnoreCase
            );
            Assert.IsTrue(comparer.Equals("Invariant", "INVARIANT"));
        }

        [Test]
        public void GetHashCodeMatchesEqualsSemantics()
        {
            SerializedStringComparer comparer = new(
                SerializedStringComparer.StringCompareMode.OrdinalIgnoreCase
            );

            int hashA = comparer.GetHashCode("Value");
            int hashB = comparer.GetHashCode("VALUE");

            Assert.AreEqual(hashA, hashB);
        }

        [Test]
        public void InvalidModeThrows()
        {
            SerializedStringComparer comparer = new(
                (SerializedStringComparer.StringCompareMode)999
            );

            Assert.Throws<InvalidEnumArgumentException>(() => comparer.Equals("a", "b"));
            Assert.Throws<InvalidEnumArgumentException>(() => comparer.GetHashCode("a"));
        }
    }
}
