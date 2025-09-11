namespace WallstopStudios.UnityHelpers.Tests.Extensions
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.DataStructure;
    using WallstopStudios.UnityHelpers.Core.Extension;

    public sealed class IReadonlyListExtensionTests
    {
        [Test]
        public void IndexOfArray()
        {
            IReadOnlyList<int> list = new int[] { 1, 2, 3 };
            int index = list.IndexOf(1);
            Assert.AreEqual(0, index);
            index = list.IndexOf(2);
            Assert.AreEqual(1, index);
            index = list.IndexOf(3);
            Assert.AreEqual(2, index);
            index = list.IndexOf(4);
            Assert.IsTrue(index < 0);
            index = list.IndexOf(-1);
            Assert.IsTrue(index < 0);
        }

        [Test]
        public void IndexOfList()
        {
            IReadOnlyList<int> list = new List<int>() { 1, 2, 3 };
            int index = list.IndexOf(1);
            Assert.AreEqual(0, index);
            index = list.IndexOf(2);
            Assert.AreEqual(1, index);
            index = list.IndexOf(3);
            Assert.AreEqual(2, index);
            index = list.IndexOf(4);
            Assert.IsTrue(index < 0);
            index = list.IndexOf(-1);
            Assert.IsTrue(index < 0);
        }

        [Test]
        public void IndexOfCyclicBuffer()
        {
            CyclicBuffer<int> list = new(3) { 1, 2, 3 };
            int index = list.IndexOf(1);
            Assert.AreEqual(0, index);
            index = list.IndexOf(2);
            Assert.AreEqual(1, index);
            index = list.IndexOf(3);
            Assert.AreEqual(2, index);
            index = list.IndexOf(4);
            Assert.IsTrue(index < 0);
            index = list.IndexOf(-1);
            Assert.IsTrue(index < 0);
        }
    }
}
