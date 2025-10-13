namespace WallstopStudios.UnityHelpers.Tests.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Text.Json;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Core.Serialization;
    using WallstopStudios.UnityHelpers.Tests.TestUtils;

    [TestFixture]
    public sealed class UnityEngineObjectJsonTests : CommonTestBase
    {
        [Test]
        public void GameObjectSerializationContainsExpectedFields()
        {
            GameObject go = Track(new GameObject("GO_Main"));
            int expectedId = go.GetInstanceID();
            string json = Serializer.JsonStringify(go);
            Assert.IsFalse(string.IsNullOrWhiteSpace(json), json);

            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            Assert.AreEqual(JsonValueKind.Object, root.ValueKind);
            Assert.True(root.TryGetProperty("name", out JsonElement name));
            Assert.True(root.TryGetProperty("type", out JsonElement type));
            Assert.True(root.TryGetProperty("instanceId", out JsonElement id));
            Assert.AreEqual("GO_Main", name.GetString());
            StringAssert.Contains("UnityEngine.GameObject", type.GetString());
            Assert.AreEqual(expectedId, id.GetInt32());
        }

        [Test]
        public void GameObjectSerializationHandlesQuotesAndEscapes()
        {
            string tricky = "Quote \" and Backslash \\ and Newline\n and Unicode ☺";
            GameObject go = Track(new GameObject(tricky));
            string json = Serializer.JsonStringify(go);
            using JsonDocument doc = JsonDocument.Parse(json);
            Assert.AreEqual(tricky, doc.RootElement.GetProperty("name").GetString());
        }

        [Test]
        public void ObjectTypedSerializationUsesRuntimeTypeConverter()
        {
            GameObject go = Track(new GameObject("ObjectTypedGO"));
            int expectedId = go.GetInstanceID();
            object goAsObject = go;
            string json = Serializer.JsonStringify(goAsObject);
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            Assert.AreEqual("ObjectTypedGO", root.GetProperty("name").GetString());
            StringAssert.Contains("UnityEngine.GameObject", root.GetProperty("type").GetString());
            Assert.AreEqual(expectedId, root.GetProperty("instanceId").GetInt32());
        }

        [Test]
        public void GameObjectSerializationPrettyPrintFormatsAndParses()
        {
            GameObject go = Track(new GameObject("Pretty ☃ Object ✓"));
            int expectedId = go.GetInstanceID();
            string json = Serializer.JsonStringify(go, pretty: true);
            Assert.IsFalse(string.IsNullOrWhiteSpace(json), json);
            StringAssert.Contains("\n", json);

            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            Assert.True(root.TryGetProperty("name", out JsonElement name));
            Assert.AreEqual("Pretty ☃ Object ✓", name.GetString());
            Assert.True(root.TryGetProperty("instanceId", out JsonElement id));
            Assert.AreEqual(expectedId, id.GetInt32());
        }

        [Test]
        public void GameObjectSerializationStableInstanceIdAcrossSerializations()
        {
            GameObject go = Track(new GameObject("StableId"));
            int expectedId = go.GetInstanceID();
            string j1 = Serializer.JsonStringify(go);
            string j2 = Serializer.JsonStringify(go);

            using JsonDocument d1 = JsonDocument.Parse(j1);
            using JsonDocument d2 = JsonDocument.Parse(j2);
            int id1 = d1.RootElement.GetProperty("instanceId").GetInt32();
            int id2 = d2.RootElement.GetProperty("instanceId").GetInt32();
            Assert.AreEqual(expectedId, id1);
            Assert.AreEqual(id1, id2);
        }

        [Test]
        public void GameObjectDeserializationIsNotSupported()
        {
            GameObject go = Track(new GameObject("NoReadSupport"));
            string json = Serializer.JsonStringify(go);

            // Our converter intentionally does not implement Read. Ensure this throws.
            Assert.Throws<NotImplementedException>(() =>
                Serializer.JsonDeserialize<GameObject>(json)
            );
        }

        [UnityTest]
        public System.Collections.IEnumerator GameObjectArraySerializationWorksWithNullAndDestroyed()
        {
            GameObject alive = Track(new GameObject("Alive"));
            int aliveId = alive.GetInstanceID();
            GameObject dead = Track(new GameObject("Dead"));
            dead.Destroy();
            yield return null; // ensure Unity nullification
            Assert.IsTrue(dead == null);

            GameObject[] data = { null, alive, dead };
            string json = Serializer.JsonStringify(data);
            Assert.IsFalse(string.IsNullOrWhiteSpace(json), json);

            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            Assert.AreEqual(JsonValueKind.Array, root.ValueKind);
            Assert.AreEqual(3, root.GetArrayLength());
            Assert.AreEqual(JsonValueKind.Null, root[0].ValueKind);

            JsonElement mid = root[1];
            Assert.AreEqual(JsonValueKind.Object, mid.ValueKind);
            Assert.AreEqual("Alive", mid.GetProperty("name").GetString());
            Assert.AreEqual(aliveId, mid.GetProperty("instanceId").GetInt32());

            Assert.AreEqual(JsonValueKind.Null, root[2].ValueKind);
        }

        [UnityTest]
        public System.Collections.IEnumerator GameObjectDictionarySerializationWorksWithNullAndDestroyed()
        {
            GameObject alive = Track(new GameObject("Alive_Dict"));
            int aliveId = alive.GetInstanceID();
            GameObject dead = Track(new GameObject("Dead_Dict"));
            dead.Destroy();
            yield return null; // ensure Unity nullification
            Assert.IsTrue(dead == null);

            Dictionary<string, GameObject> dict = new()
            {
                ["none"] = null,
                ["alive"] = alive,
                ["dead"] = dead,
            };

            string json = Serializer.JsonStringify(dict);
            Assert.IsFalse(string.IsNullOrWhiteSpace(json), json);

            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            Assert.AreEqual(JsonValueKind.Object, root.ValueKind);

            Assert.True(root.TryGetProperty("none", out JsonElement none));
            Assert.AreEqual(JsonValueKind.Null, none.ValueKind);

            Assert.True(root.TryGetProperty("alive", out JsonElement aliveEl));
            Assert.AreEqual("Alive_Dict", aliveEl.GetProperty("name").GetString());
            Assert.AreEqual(aliveId, aliveEl.GetProperty("instanceId").GetInt32());

            Assert.True(root.TryGetProperty("dead", out JsonElement deadEl));
            Assert.AreEqual(JsonValueKind.Null, deadEl.ValueKind);
        }

        private sealed class GoHolder
        {
            public GameObject Go { get; set; }
        }

        private sealed class GoFieldHolder
        {
            public GameObject go;
        }

        [Test]
        public void NestedObjectSerializationWithGameObjectProperty()
        {
            GameObject go = Track(new GameObject("Nested"));
            int expectedId = go.GetInstanceID();
            GoHolder holder = new() { Go = go };
            string json = Serializer.JsonStringify(holder);
            Assert.IsFalse(string.IsNullOrWhiteSpace(json), json);

            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            Assert.True(root.TryGetProperty("Go", out JsonElement goEl));
            Assert.AreEqual("Nested", goEl.GetProperty("name").GetString());
            Assert.AreEqual(expectedId, goEl.GetProperty("instanceId").GetInt32());
        }

        [Test]
        public void NestedObjectSerializationWithGameObjectField()
        {
            GameObject go = Track(new GameObject("NestedField"));
            int expectedId = go.GetInstanceID();
            GoFieldHolder holder = new() { go = go };
            string json = Serializer.JsonStringify(holder);
            Assert.IsFalse(string.IsNullOrWhiteSpace(json), json);

            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            Assert.True(root.TryGetProperty("go", out JsonElement goEl));
            Assert.AreEqual("NestedField", goEl.GetProperty("name").GetString());
            Assert.AreEqual(expectedId, goEl.GetProperty("instanceId").GetInt32());
        }

        [Test]
        public void FastSerializerProducesValidGameObjectJson()
        {
            GameObject go = Track(new GameObject("FastGO"));
            int expectedId = go.GetInstanceID();
            byte[] bytes = Serializer.JsonSerializeFast(go);
            Assert.NotNull(bytes);
            string json = Encoding.UTF8.GetString(bytes);
            Assert.IsFalse(string.IsNullOrWhiteSpace(json), json);

            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            Assert.AreEqual("FastGO", root.GetProperty("name").GetString());
            Assert.AreEqual(expectedId, root.GetProperty("instanceId").GetInt32());
        }

        [Test]
        public void SerializeIntoCallerBufferProducesValidGameObjectJson()
        {
            GameObject go = Track(new GameObject("BufferedGO"));
            int expectedId = go.GetInstanceID();
            byte[] buffer = null;
            int len = Serializer.JsonSerialize(
                go,
                SerializerEncoding.GetNormalJsonOptions(),
                ref buffer
            );
            Assert.Greater(len, 0);
            Assert.NotNull(buffer);

            string json = Encoding.UTF8.GetString(buffer, 0, len);
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            Assert.AreEqual("BufferedGO", root.GetProperty("name").GetString());
            Assert.AreEqual(expectedId, root.GetProperty("instanceId").GetInt32());
        }
    }
}
