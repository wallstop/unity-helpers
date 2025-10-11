namespace WallstopStudios.UnityHelpers.Tests.Serialization
{
    using System.Text.Json;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Core.Serialization;

    public sealed class AdaptersJsonTests
    {
        [Test]
        public void FastVector2IntRoundTrips()
        {
            FastVector2Int v = new(-3, 7);
            string json = Serializer.JsonStringify(v);
            FastVector2Int again = Serializer.JsonDeserialize<FastVector2Int>(json);
            Assert.AreEqual(v, again, "FastVector2Int should round-trip by value");
        }

        [Test]
        public void FastVector3IntRoundTrips()
        {
            FastVector3Int v = new(1, -2, 3);
            string json = Serializer.JsonStringify(v);
            FastVector3Int again = Serializer.JsonDeserialize<FastVector3Int>(json);
            Assert.AreEqual(v, again, "FastVector3Int should round-trip by value");
        }

        [Test]
        public void KVector2RoundTrips()
        {
            KVector2 v = new(1.25f, -2.5f);
            string json = Serializer.JsonStringify(v);
            KVector2 again = Serializer.JsonDeserialize<KVector2>(json);
            Assert.AreEqual(v, again, "KVector2 should round-trip by value");
        }

        [Test]
        public void KGuidRoundTrips()
        {
            KGuid id = KGuid.NewGuid();
            string json = Serializer.JsonStringify(id);
            KGuid again = Serializer.JsonDeserialize<KGuid>(json);
            Assert.AreEqual(id, again, "KGuid should round-trip by value");
        }

        [Test]
        public void FastOptionsAdaptersRoundTrip()
        {
            FastVector2Int v2 = new(9, -4);
            FastVector3Int v3 = new(5, 6, -7);
            KVector2 kv = new(-3.5f, 8.25f);
            JsonSerializerOptions options = Serializer.CreateFastJsonOptions();

            string j2 = Serializer.JsonStringify(v2, options);
            string j3 = Serializer.JsonStringify(v3, options);
            string jk = Serializer.JsonStringify(kv, options);

            FastVector2Int v2b = Serializer.JsonDeserialize<FastVector2Int>(j2, null, options);
            FastVector3Int v3b = Serializer.JsonDeserialize<FastVector3Int>(j3, null, options);
            KVector2 kvb = Serializer.JsonDeserialize<KVector2>(jk, null, options);

            Assert.AreEqual(v2, v2b, "FastVector2Int should round-trip with fast options");
            Assert.AreEqual(v3, v3b, "FastVector3Int should round-trip with fast options");
            Assert.AreEqual(kv, kvb, "KVector2 should round-trip with fast options");
        }
    }
}
