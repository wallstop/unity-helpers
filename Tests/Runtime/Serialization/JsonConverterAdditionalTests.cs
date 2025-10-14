namespace WallstopStudios.UnityHelpers.Tests.Serialization
{
    using System.Text.Json;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Serialization;
    using WallstopStudios.UnityHelpers.Tests.TestUtils;

    public sealed class JsonConverterAdditionalTests : CommonTestBase
    {
        [Test]
        public void GameObjectConverterWritesStructuredJson()
        {
            GameObject go = Track(new GameObject("ConverterTestObject"));
            int expectedId = go.GetInstanceID();
            string json = Serializer.JsonStringify(go);
            Assert.IsFalse(string.IsNullOrWhiteSpace(json));

            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            Assert.AreEqual(JsonValueKind.Object, root.ValueKind);
            Assert.True(root.TryGetProperty("name", out JsonElement name));
            Assert.True(root.TryGetProperty("type", out JsonElement type));
            Assert.True(root.TryGetProperty("instanceId", out JsonElement id));
            Assert.AreEqual("ConverterTestObject", name.GetString());
            StringAssert.Contains("UnityEngine.GameObject", type.GetString());
            Assert.AreEqual(expectedId, id.GetInt32());
        }
    }
}
