// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Helper
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Tests.Core;

    [TestFixture]
    [NUnit.Framework.Category("Fast")]
    public sealed class EnumerablesTests : CommonTestBase
    {
        [Test]
        public void OfReturnsSingleElementSequence()
        {
            int value = 99;
            int[] result = Enumerables.Of(value).ToArray();

            Assert.That(result, Is.EquivalentTo(new[] { value }));
        }

        [Test]
        public void OfReturnsProvidedArrayReference()
        {
            int[] values = { 1, 2, 3 };
            IEnumerable<int> result = Enumerables.Of(values);

            Assert.AreSame(values, result);
        }

        [Test]
        public void OfReturnsExpectedElements()
        {
            int[] expected = { 1, 2, 3 };
            IEnumerable<int> result = Enumerables.Of(1, 2, 3);

            Assert.That(expected, Is.EquivalentTo(result));
        }

        [Test]
        public void OfReturnsEmpty()
        {
            IEnumerable<int> result = Enumerables.Of<int>();
            Assert.That(Array.Empty<int>(), Is.EquivalentTo(result));
        }
    }
}
