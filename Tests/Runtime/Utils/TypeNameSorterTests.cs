namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Utils;

    public sealed class TypeNameSorterTests
    {
        private sealed class Alpha { }

        private sealed class beta { }

        [Test]
        public void CompareOrdersByTypeNameIgnoringCase()
        {
            List<Type> types = new() { typeof(beta), typeof(Alpha) };

            types.Sort(TypeNameSorter.Instance);

            Assert.That(types, Is.EqualTo(new[] { typeof(Alpha), typeof(beta) }));
        }

        [Test]
        public void CompareHandlesNullTypes()
        {
            int compareNullAgainstType = TypeNameSorter.Instance.Compare(null, typeof(Alpha));
            int compareTypeAgainstNull = TypeNameSorter.Instance.Compare(typeof(Alpha), null);

            Assert.Less(compareNullAgainstType, 0);
            Assert.Greater(compareTypeAgainstNull, 0);
        }
    }
}
