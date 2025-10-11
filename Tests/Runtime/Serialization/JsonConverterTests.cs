namespace WallstopStudios.UnityHelpers.Tests.Serialization
{
    using System;
    using System.Text.Json;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Serialization;

    [TestFixture]
    public sealed class JsonConverterTests
    {
        [Test]
        public void Vector2ConverterSerializeAndDeserializeSuccess()
        {
            Vector2 original = new(1.5f, 2.5f);
            string json = Serializer.JsonStringify(original);
            Vector2 deserialized = Serializer.JsonDeserialize<Vector2>(json);

            Assert.AreEqual(original.x, deserialized.x);
            Assert.AreEqual(original.y, deserialized.y);
        }

        [Test]
        public void Vector2ConverterZeroVectorSuccess()
        {
            Vector2 original = Vector2.zero;
            string json = Serializer.JsonStringify(original);
            Vector2 deserialized = Serializer.JsonDeserialize<Vector2>(json);

            Assert.AreEqual(original, deserialized);
        }

        [Test]
        public void Vector2ConverterNegativeValuesSuccess()
        {
            Vector2 original = new(-10.5f, -20.3f);
            string json = Serializer.JsonStringify(original);
            Vector2 deserialized = Serializer.JsonDeserialize<Vector2>(json);

            Assert.AreEqual(original.x, deserialized.x);
            Assert.AreEqual(original.y, deserialized.y);
        }

        [Test]
        public void Vector2ConverterInvalidTokenTypeThrowsException()
        {
            string invalidJson = "\"not an object\"";
            Assert.Throws<JsonException>(() => Serializer.JsonDeserialize<Vector2>(invalidJson));
        }

        [Test]
        public void Vector2ConverterUnknownPropertyThrowsException()
        {
            string invalidJson = "{\"x\":1.0,\"y\":2.0,\"z\":3.0}";
            Assert.Throws<JsonException>(() => Serializer.JsonDeserialize<Vector2>(invalidJson));
        }

        [Test]
        public void Vector2ConverterIncompleteJsonThrowsException()
        {
            string incompleteJson = "{\"x\":1.0";
            Assert.Throws<JsonException>(() => Serializer.JsonDeserialize<Vector2>(incompleteJson));
        }

        [Test]
        public void Vector3ConverterSerializeAndDeserializeSuccess()
        {
            Vector3 original = new(1.5f, 2.5f, 3.5f);
            string json = Serializer.JsonStringify(original);
            Vector3 deserialized = Serializer.JsonDeserialize<Vector3>(json);

            Assert.AreEqual(original.x, deserialized.x);
            Assert.AreEqual(original.y, deserialized.y);
            Assert.AreEqual(original.z, deserialized.z);
        }

        [Test]
        public void Vector3ConverterZeroVectorSuccess()
        {
            Vector3 original = Vector3.zero;
            string json = Serializer.JsonStringify(original);
            Vector3 deserialized = Serializer.JsonDeserialize<Vector3>(json);

            Assert.AreEqual(original, deserialized);
        }

        [Test]
        public void Vector3ConverterNegativeValuesSuccess()
        {
            Vector3 original = new(-10.5f, -20.3f, -30.7f);
            string json = Serializer.JsonStringify(original);
            Vector3 deserialized = Serializer.JsonDeserialize<Vector3>(json);

            Assert.AreEqual(original.x, deserialized.x);
            Assert.AreEqual(original.y, deserialized.y);
            Assert.AreEqual(original.z, deserialized.z);
        }

        [Test]
        public void Vector3ConverterInvalidTokenTypeThrowsException()
        {
            string invalidJson = "\"not an object\"";
            Assert.Throws<JsonException>(() => Serializer.JsonDeserialize<Vector3>(invalidJson));
        }

        [Test]
        public void Vector3ConverterUnknownPropertyThrowsException()
        {
            string invalidJson = "{\"x\":1.0,\"y\":2.0,\"z\":3.0,\"w\":4.0}";
            Assert.Throws<JsonException>(() => Serializer.JsonDeserialize<Vector3>(invalidJson));
        }

        [Test]
        public void Vector3ConverterIncompleteJsonThrowsException()
        {
            string incompleteJson = "{\"x\":1.0,\"y\":2.0";
            Assert.Throws<JsonException>(() => Serializer.JsonDeserialize<Vector3>(incompleteJson));
        }

        [Test]
        public void Vector4ConverterSerializeAndDeserializeSuccess()
        {
            Vector4 original = new(1.5f, 2.5f, 3.5f, 4.5f);
            string json = Serializer.JsonStringify(original);
            Vector4 deserialized = Serializer.JsonDeserialize<Vector4>(json);

            Assert.AreEqual(original.x, deserialized.x);
            Assert.AreEqual(original.y, deserialized.y);
            Assert.AreEqual(original.z, deserialized.z);
            Assert.AreEqual(original.w, deserialized.w);
        }

        [Test]
        public void Vector4ConverterZeroVectorSuccess()
        {
            Vector4 original = Vector4.zero;
            string json = Serializer.JsonStringify(original);
            Vector4 deserialized = Serializer.JsonDeserialize<Vector4>(json);

            Assert.AreEqual(original, deserialized);
        }

        [Test]
        public void Vector4ConverterNegativeValuesSuccess()
        {
            Vector4 original = new(-10.5f, -20.3f, -30.7f, -40.1f);
            string json = Serializer.JsonStringify(original);
            Vector4 deserialized = Serializer.JsonDeserialize<Vector4>(json);

            Assert.AreEqual(original.x, deserialized.x);
            Assert.AreEqual(original.y, deserialized.y);
            Assert.AreEqual(original.z, deserialized.z);
            Assert.AreEqual(original.w, deserialized.w);
        }

        [Test]
        public void Vector4ConverterInvalidTokenTypeThrowsException()
        {
            string invalidJson = "\"not an object\"";
            Assert.Throws<JsonException>(() => Serializer.JsonDeserialize<Vector4>(invalidJson));
        }

        [Test]
        public void Vector4ConverterUnknownPropertyThrowsException()
        {
            string invalidJson = "{\"x\":1.0,\"y\":2.0,\"z\":3.0,\"w\":4.0,\"extra\":5.0}";
            Assert.Throws<JsonException>(() => Serializer.JsonDeserialize<Vector4>(invalidJson));
        }

        [Test]
        public void Vector4ConverterIncompleteJsonThrowsException()
        {
            string incompleteJson = "{\"x\":1.0,\"y\":2.0,\"z\":3.0";
            Assert.Throws<JsonException>(() => Serializer.JsonDeserialize<Vector4>(incompleteJson));
        }

        [Test]
        public void ColorConverterSerializeAndDeserializeSuccess()
        {
            Color original = new(0.1f, 0.2f, 0.3f, 0.4f);
            string json = Serializer.JsonStringify(original);
            Color deserialized = Serializer.JsonDeserialize<Color>(json);

            Assert.AreEqual(original.r, deserialized.r);
            Assert.AreEqual(original.g, deserialized.g);
            Assert.AreEqual(original.b, deserialized.b);
            Assert.AreEqual(original.a, deserialized.a);
        }

        [Test]
        public void ColorConverterBlackColorSuccess()
        {
            Color original = Color.black;
            string json = Serializer.JsonStringify(original);
            Color deserialized = Serializer.JsonDeserialize<Color>(json);

            Assert.AreEqual(original.r, deserialized.r);
            Assert.AreEqual(original.g, deserialized.g);
            Assert.AreEqual(original.b, deserialized.b);
            Assert.AreEqual(original.a, deserialized.a);
        }

        [Test]
        public void ColorConverterWhiteColorSuccess()
        {
            Color original = Color.white;
            string json = Serializer.JsonStringify(original);
            Color deserialized = Serializer.JsonDeserialize<Color>(json);

            Assert.AreEqual(original.r, deserialized.r);
            Assert.AreEqual(original.g, deserialized.g);
            Assert.AreEqual(original.b, deserialized.b);
            Assert.AreEqual(original.a, deserialized.a);
        }

        [Test]
        public void ColorConverterDefaultAlphaValueSuccess()
        {
            Color original = new(0.5f, 0.6f, 0.7f);
            string json = Serializer.JsonStringify(original);
            Color deserialized = Serializer.JsonDeserialize<Color>(json);

            Assert.AreEqual(original.r, deserialized.r);
            Assert.AreEqual(original.g, deserialized.g);
            Assert.AreEqual(original.b, deserialized.b);
            Assert.AreEqual(original.a, deserialized.a);
        }

        [Test]
        public void ColorConverterInvalidTokenTypeThrowsException()
        {
            string invalidJson = "\"not an object\"";
            Assert.Throws<JsonException>(() => Serializer.JsonDeserialize<Color>(invalidJson));
        }

        [Test]
        public void ColorConverterUnknownPropertyThrowsException()
        {
            string invalidJson = "{\"r\":0.5,\"g\":0.5,\"b\":0.5,\"a\":1.0,\"extra\":0.0}";
            Assert.Throws<JsonException>(() => Serializer.JsonDeserialize<Color>(invalidJson));
        }

        [Test]
        public void ColorConverterIncompleteJsonThrowsException()
        {
            string incompleteJson = "{\"r\":0.5,\"g\":0.5";
            Assert.Throws<JsonException>(() => Serializer.JsonDeserialize<Color>(incompleteJson));
        }

        [Test]
        public void Matrix4X4ConverterSerializeAndDeserializeSuccess()
        {
            Matrix4x4 original = Matrix4x4.identity;
            string json = Serializer.JsonStringify(original);
            Matrix4x4 deserialized = Serializer.JsonDeserialize<Matrix4x4>(json);

            Assert.AreEqual(original, deserialized);
        }

        [Test]
        public void Matrix4X4ConverterCustomMatrixSuccess()
        {
            Matrix4x4 original = new();
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    original[row, col] = row * 4 + col;
                }
            }

            string json = Serializer.JsonStringify(original);
            Matrix4x4 deserialized = Serializer.JsonDeserialize<Matrix4x4>(json);

            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    Assert.AreEqual(original[row, col], deserialized[row, col]);
                }
            }
        }

        [Test]
        public void Matrix4X4ConverterZeroMatrixSuccess()
        {
            Matrix4x4 original = Matrix4x4.zero;
            string json = Serializer.JsonStringify(original);
            Matrix4x4 deserialized = Serializer.JsonDeserialize<Matrix4x4>(json);

            Assert.AreEqual(original, deserialized);
        }

        [Test]
        public void Matrix4X4ConverterTranslationMatrixSuccess()
        {
            Matrix4x4 original = Matrix4x4.Translate(new Vector3(1, 2, 3));
            string json = Serializer.JsonStringify(original);
            Matrix4x4 deserialized = Serializer.JsonDeserialize<Matrix4x4>(json);

            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    Assert.AreEqual(original[row, col], deserialized[row, col], 0.0001f);
                }
            }
        }

        [Test]
        public void Matrix4X4ConverterInvalidTokenTypeThrowsException()
        {
            string invalidJson = "\"not an object\"";
            Assert.Throws<JsonException>(() => Serializer.JsonDeserialize<Matrix4x4>(invalidJson));
        }

        [Test]
        public void Matrix4X4ConverterMissingPropertyThrowsException()
        {
            string incompleteJson = "{\"m00\":1.0,\"m01\":0.0,\"m02\":0.0,\"m03\":0.0}";
            Assert.Throws<JsonException>(() =>
                Serializer.JsonDeserialize<Matrix4x4>(incompleteJson)
            );
        }

        [Test]
        public void Matrix4X4ConverterInvalidPropertyValueThrowsException()
        {
            string invalidJson =
                "{\"m00\":\"invalid\",\"m01\":0.0,\"m02\":0.0,\"m03\":0.0,\"m10\":0.0,\"m11\":1.0,\"m12\":0.0,\"m13\":0.0,\"m20\":0.0,\"m21\":0.0,\"m22\":1.0,\"m23\":0.0,\"m30\":0.0,\"m31\":0.0,\"m32\":0.0,\"m33\":1.0}";
            Assert.Throws<JsonException>(() => Serializer.JsonDeserialize<Matrix4x4>(invalidJson));
        }

        // Note: TypeConverter is tested within object contexts in JsonSerializationTest.SerializationWorks
        // Direct Type serialization tests are covered by the existing test suite

        private class TypeContainerForTest
        {
            public Type TypeProperty { get; set; }
            public System.Collections.Generic.List<Type> TypeList { get; set; }
        }

        [Test]
        public void TypeConverterWithinObjectSuccess()
        {
            TypeContainerForTest original = new()
            {
                TypeProperty = typeof(string),
                TypeList = new System.Collections.Generic.List<Type>
                {
                    typeof(int),
                    typeof(JsonConverterTests),
                    typeof(System.Collections.Generic.List<int>),
                },
            };

            string json = Serializer.JsonStringify(original);
            TypeContainerForTest deserialized = Serializer.JsonDeserialize<TypeContainerForTest>(
                json
            );

            Assert.AreEqual(original.TypeProperty, deserialized.TypeProperty);
            Assert.That(original.TypeList, Is.EquivalentTo(deserialized.TypeList));
        }

        [Test]
        public void TypeConverterNullTypeInObjectSuccess()
        {
            TypeContainerForTest original = new() { TypeProperty = null, TypeList = null };

            string json = Serializer.JsonStringify(original);
            TypeContainerForTest deserialized = Serializer.JsonDeserialize<TypeContainerForTest>(
                json
            );

            Assert.IsNull(deserialized.TypeProperty);
            Assert.IsNull(deserialized.TypeList);
        }

        [Test]
        public void TypeConverterEmptyStringReturnsNull()
        {
            string json = "\"\"";
            Type deserialized = Serializer.JsonDeserialize<Type>(json);

            Assert.IsNull(deserialized);
        }

        [Test]
        public void TypeConverterWhitespaceStringReturnsNull()
        {
            string json = "\"   \"";
            Type deserialized = Serializer.JsonDeserialize<Type>(json);

            Assert.IsNull(deserialized);
        }

        [Test]
        public void TypeConverterInvalidTypeNameReturnsNull()
        {
            string json = "\"NonExistent.Type.Name\"";
            Type deserialized = Serializer.JsonDeserialize<Type>(json);

            Assert.IsNull(deserialized);
        }

        [Test]
        public void GameObjectConverterSerializeValidGameObjectSuccess()
        {
            GameObject original = new("TestObject");
            string json = Serializer.JsonStringify(original);

            Assert.IsFalse(string.IsNullOrWhiteSpace(json));
            Assert.IsTrue(json.Contains("TestObject"));
            Assert.IsTrue(json.Contains("UnityEngine.GameObject"));

            UnityEngine.Object.DestroyImmediate(original);
        }

        [Test]
        public void GameObjectConverterSerializeNullGameObjectSuccess()
        {
            GameObject original = null;
            string json = Serializer.JsonStringify(original);

            Assert.AreEqual("null", json);
        }

        [Test]
        public void GameObjectConverterSerializeDestroyedGameObjectReturnsEmptyObject()
        {
            GameObject original = new("TestObject");
            UnityEngine.Object.DestroyImmediate(original);

            string json = Serializer.JsonStringify(original);
            Assert.IsFalse(string.IsNullOrWhiteSpace(json));
        }

        [Test]
        public void GameObjectConverterReadThrowsNotImplementedException()
        {
            string json = "{\"name\":\"Test\"}";

            Assert.Throws<NotImplementedException>(() =>
                Serializer.JsonDeserialize<GameObject>(json)
            );
        }
    }
}
