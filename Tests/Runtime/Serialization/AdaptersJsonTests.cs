namespace WallstopStudios.UnityHelpers.Tests.Serialization
{
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Core.Serialization;

    public sealed class AdaptersJsonTests
    {
        [Test]
        public void FastVector2IntRoundTrips()
        {
            var v = new FastVector2Int(-3, 7);
            string json = Serializer.JsonStringify(v);
            var again = Serializer.JsonDeserialize<FastVector2Int>(json);
            Assert.AreEqual(v, again, "FastVector2Int should round-trip by value");
        }

        [Test]
        public void FastVector3IntRoundTrips()
        {
            var v = new FastVector3Int(1, -2, 3);
            string json = Serializer.JsonStringify(v);
            var again = Serializer.JsonDeserialize<FastVector3Int>(json);
            Assert.AreEqual(v, again, "FastVector3Int should round-trip by value");
        }

        [Test]
        public void KVector2RoundTrips()
        {
            var v = new KVector2(1.25f, -2.5f);
            string json = Serializer.JsonStringify(v);
            var again = Serializer.JsonDeserialize<KVector2>(json);
            Assert.AreEqual(v, again, "KVector2 should round-trip by value");
        }

        [Test]
        public void KGuidRoundTrips()
        {
            var id = KGuid.NewGuid();
            string json = Serializer.JsonStringify(id);
            var again = Serializer.JsonDeserialize<KGuid>(json);
            Assert.AreEqual(id, again, "KGuid should round-trip by value");
        }

        [Test]
        public void FastOptionsAdaptersRoundTrip()
        {
            var v2 = new FastVector2Int(9, -4);
            var v3 = new FastVector3Int(5, 6, -7);
            var kv = new KVector2(-3.5f, 8.25f);
            var options = Serializer.CreateFastJsonOptions();

            string j2 = Serializer.JsonStringify(v2, options);
            string j3 = Serializer.JsonStringify(v3, options);
            string jk = Serializer.JsonStringify(kv, options);

            var v2b = Serializer.JsonDeserialize<FastVector2Int>(j2, null, options);
            var v3b = Serializer.JsonDeserialize<FastVector3Int>(j3, null, options);
            var kvb = Serializer.JsonDeserialize<KVector2>(jk, null, options);

            Assert.AreEqual(v2, v2b, "FastVector2Int should round-trip with fast options");
            Assert.AreEqual(v3, v3b, "FastVector3Int should round-trip with fast options");
            Assert.AreEqual(kv, kvb, "KVector2 should round-trip with fast options");
        }
    }
}
