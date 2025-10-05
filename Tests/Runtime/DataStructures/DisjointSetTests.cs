namespace WallstopStudios.UnityHelpers.Tests.DataStructures
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.DataStructure;

    public sealed class DisjointSetTests
    {
        [Test]
        public void InitiallyAllElementsSeparate()
        {
            DisjointSet ds = new(5);

            Assert.AreEqual(5, ds.Count);
            Assert.AreEqual(5, ds.SetCount);
        }

        [Test]
        public void TryUnionConnectsTwoElements()
        {
            DisjointSet ds = new(5);

            Assert.IsTrue(ds.TryUnion(0, 1));
            Assert.AreEqual(4, ds.SetCount);
        }

        [Test]
        public void TryUnionReturnsFalseForSameSet()
        {
            DisjointSet ds = new(5);
            ds.TryUnion(0, 1);

            Assert.IsFalse(ds.TryUnion(0, 1));
            Assert.AreEqual(4, ds.SetCount);
        }

        [Test]
        public void TryIsConnectedWorks()
        {
            DisjointSet ds = new(5);
            ds.TryUnion(0, 1);
            ds.TryUnion(1, 2);

            Assert.IsTrue(ds.TryIsConnected(0, 2, out bool connected));
            Assert.IsTrue(connected);

            Assert.IsTrue(ds.TryIsConnected(0, 3, out bool notConnected));
            Assert.IsFalse(notConnected);
        }

        [Test]
        public void TryFindReturnsRepresentative()
        {
            DisjointSet ds = new(5);
            ds.TryUnion(0, 1);
            ds.TryUnion(1, 2);

            Assert.IsTrue(ds.TryFind(0, out int rep0));
            Assert.IsTrue(ds.TryFind(2, out int rep2));
            Assert.AreEqual(rep0, rep2);
        }

        [Test]
        public void TryGetSetReturnsCorrectElements()
        {
            DisjointSet ds = new(5);
            ds.TryUnion(0, 1);
            ds.TryUnion(1, 2);

            List<int> results = new();
            List<int> set = ds.TryGetSet(0, results);

            Assert.IsNotNull(set);
            Assert.AreEqual(3, set.Count);
            CollectionAssert.Contains(set, 0);
            CollectionAssert.Contains(set, 1);
            CollectionAssert.Contains(set, 2);
        }

        [Test]
        public void ResetSeparatesAllElements()
        {
            DisjointSet ds = new(5);
            ds.TryUnion(0, 1);
            ds.TryUnion(2, 3);

            ds.Reset();

            Assert.AreEqual(5, ds.SetCount);
        }

        [Test]
        public void GenericVersionWorks()
        {
            List<string> elements = new() { "a", "b", "c", "d" };
            DisjointSet<string> ds = new(elements);

            Assert.IsTrue(ds.TryUnion("a", "b"));
            Assert.IsTrue(ds.TryIsConnected("a", "b", out bool connected));
            Assert.IsTrue(connected);
        }
    }
}
