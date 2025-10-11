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
        public void QuaternionConverterSerializeAndDeserializeSuccess()
        {
            Quaternion original = new(0.1f, 0.2f, 0.3f, 0.9f);
            string json = Serializer.JsonStringify(original);
            Quaternion deserialized = Serializer.JsonDeserialize<Quaternion>(json);

            Assert.AreEqual(original.x, deserialized.x);
            Assert.AreEqual(original.y, deserialized.y);
            Assert.AreEqual(original.z, deserialized.z);
            Assert.AreEqual(original.w, deserialized.w);
        }

        [Test]
        public void QuaternionConverterIdentityDefaultWSuccess()
        {
            string json = "{\"x\":0.0,\"y\":0.0,\"z\":0.0}";
            Quaternion deserialized = Serializer.JsonDeserialize<Quaternion>(json);

            Assert.AreEqual(0f, deserialized.x);
            Assert.AreEqual(0f, deserialized.y);
            Assert.AreEqual(0f, deserialized.z);
            Assert.AreEqual(1f, deserialized.w);
        }

        [Test]
        public void QuaternionConverterInvalidTokenTypeThrowsException()
        {
            string invalidJson = "\"not an object\"";
            Assert.Throws<JsonException>(() => Serializer.JsonDeserialize<Quaternion>(invalidJson));
        }

        [Test]
        public void QuaternionConverterUnknownPropertyThrowsException()
        {
            string invalidJson = "{\"x\":0,\"y\":0,\"z\":0,\"w\":1,\"extra\":0}";
            Assert.Throws<JsonException>(() => Serializer.JsonDeserialize<Quaternion>(invalidJson));
        }

        [Test]
        public void QuaternionConverterIncompleteJsonThrowsException()
        {
            string incompleteJson = "{\"x\":0.0,\"y\":0.0";
            Assert.Throws<JsonException>(() =>
                Serializer.JsonDeserialize<Quaternion>(incompleteJson)
            );
        }

        [Test]
        public void Color32ConverterSerializeAndDeserializeSuccess()
        {
            Color32 original = new Color32(10, 20, 30, 40);
            string json = Serializer.JsonStringify(original);
            Color32 deserialized = Serializer.JsonDeserialize<Color32>(json);

            Assert.AreEqual(original.r, deserialized.r);
            Assert.AreEqual(original.g, deserialized.g);
            Assert.AreEqual(original.b, deserialized.b);
            Assert.AreEqual(original.a, deserialized.a);
        }

        [Test]
        public void Color32ConverterBoundaryValuesSuccess()
        {
            Color32 original = new Color32(0, 255, 0, 255);
            string json = Serializer.JsonStringify(original);
            Color32 deserialized = Serializer.JsonDeserialize<Color32>(json);

            Assert.AreEqual(0, deserialized.r);
            Assert.AreEqual(255, deserialized.g);
            Assert.AreEqual(0, deserialized.b);
            Assert.AreEqual(255, deserialized.a);
        }

        [Test]
        public void Color32ConverterDefaultAlphaValueSuccess()
        {
            string json = "{\"r\":128,\"g\":64,\"b\":32}";
            Color32 deserialized = Serializer.JsonDeserialize<Color32>(json);

            Assert.AreEqual(128, deserialized.r);
            Assert.AreEqual(64, deserialized.g);
            Assert.AreEqual(32, deserialized.b);
            Assert.AreEqual(255, deserialized.a);
        }

        [Test]
        public void Color32ConverterInvalidTokenTypeThrowsException()
        {
            string invalidJson = "\"not an object\"";
            Assert.Throws<JsonException>(() => Serializer.JsonDeserialize<Color32>(invalidJson));
        }

        [Test]
        public void Color32ConverterUnknownPropertyThrowsException()
        {
            string invalidJson = "{\"r\":1,\"g\":2,\"b\":3,\"a\":4,\"x\":5}";
            Assert.Throws<JsonException>(() => Serializer.JsonDeserialize<Color32>(invalidJson));
        }

        [Test]
        public void Color32ConverterChannelOutOfRangeThrowsException()
        {
            string invalidJson = "{\"r\":-1,\"g\":0,\"b\":0,\"a\":0}";
            Assert.Throws<JsonException>(() => Serializer.JsonDeserialize<Color32>(invalidJson));
        }

        [Test]
        public void Color32ConverterChannelAboveRangeThrowsException()
        {
            string invalidJson = "{\"r\":256,\"g\":0,\"b\":0,\"a\":0}";
            Assert.Throws<JsonException>(() => Serializer.JsonDeserialize<Color32>(invalidJson));
        }

        [Test]
        public void Vector2IntConverterSerializeAndDeserializeSuccess()
        {
            Vector2Int original = new(5, -7);
            string json = Serializer.JsonStringify(original);
            Vector2Int deserialized = Serializer.JsonDeserialize<Vector2Int>(json);

            Assert.AreEqual(original.x, deserialized.x);
            Assert.AreEqual(original.y, deserialized.y);
        }

        [Test]
        public void Vector2IntConverterInvalidTokenTypeThrowsException()
        {
            string invalidJson = "\"not an object\"";
            Assert.Throws<JsonException>(() => Serializer.JsonDeserialize<Vector2Int>(invalidJson));
        }

        [Test]
        public void Vector2IntConverterUnknownPropertyThrowsException()
        {
            string invalidJson = "{\"x\":1,\"y\":2,\"z\":3}";
            Assert.Throws<JsonException>(() => Serializer.JsonDeserialize<Vector2Int>(invalidJson));
        }

        [Test]
        public void Vector2IntConverterIncompleteJsonThrowsException()
        {
            string incompleteJson = "{\"x\":1";
            Assert.Throws<JsonException>(() =>
                Serializer.JsonDeserialize<Vector2Int>(incompleteJson)
            );
        }

        [Test]
        public void Vector3IntConverterSerializeAndDeserializeSuccess()
        {
            Vector3Int original = new(1, -2, 3);
            string json = Serializer.JsonStringify(original);
            Vector3Int deserialized = Serializer.JsonDeserialize<Vector3Int>(json);

            Assert.AreEqual(original.x, deserialized.x);
            Assert.AreEqual(original.y, deserialized.y);
            Assert.AreEqual(original.z, deserialized.z);
        }

        [Test]
        public void Vector3IntConverterInvalidTokenTypeThrowsException()
        {
            string invalidJson = "\"not an object\"";
            Assert.Throws<JsonException>(() => Serializer.JsonDeserialize<Vector3Int>(invalidJson));
        }

        [Test]
        public void Vector3IntConverterUnknownPropertyThrowsException()
        {
            string invalidJson = "{\"x\":1,\"y\":2,\"z\":3,\"w\":4}";
            Assert.Throws<JsonException>(() => Serializer.JsonDeserialize<Vector3Int>(invalidJson));
        }

        [Test]
        public void Vector3IntConverterIncompleteJsonThrowsException()
        {
            string incompleteJson = "{\"x\":1,\"y\":2";
            Assert.Throws<JsonException>(() =>
                Serializer.JsonDeserialize<Vector3Int>(incompleteJson)
            );
        }

        [Test]
        public void RectConverterSerializeAndDeserializeSuccess()
        {
            Rect original = new(1.5f, 2.5f, 10.0f, 20.0f);
            string json = Serializer.JsonStringify(original);
            Rect deserialized = Serializer.JsonDeserialize<Rect>(json);

            Assert.AreEqual(original.x, deserialized.x);
            Assert.AreEqual(original.y, deserialized.y);
            Assert.AreEqual(original.width, deserialized.width);
            Assert.AreEqual(original.height, deserialized.height);
        }

        [Test]
        public void RectConverterInvalidTokenTypeThrowsException()
        {
            string invalidJson = "\"not an object\"";
            Assert.Throws<JsonException>(() => Serializer.JsonDeserialize<Rect>(invalidJson));
        }

        [Test]
        public void RectConverterUnknownPropertyThrowsException()
        {
            string invalidJson = "{\"x\":0,\"y\":0,\"width\":1,\"height\":1,\"extra\":0}";
            Assert.Throws<JsonException>(() => Serializer.JsonDeserialize<Rect>(invalidJson));
        }

        [Test]
        public void RectConverterIncompleteJsonThrowsException()
        {
            string incompleteJson = "{\"x\":0,\"y\":0";
            Assert.Throws<JsonException>(() => Serializer.JsonDeserialize<Rect>(incompleteJson));
        }

        [Test]
        public void RectConverterDefaultsWhenMissingWidthHeight()
        {
            string json = "{\"x\":1.0,\"y\":2.0}";
            Rect deserialized = Serializer.JsonDeserialize<Rect>(json);
            Assert.AreEqual(1.0f, deserialized.x);
            Assert.AreEqual(2.0f, deserialized.y);
            Assert.AreEqual(0.0f, deserialized.width);
            Assert.AreEqual(0.0f, deserialized.height);
        }

        [Test]
        public void RectIntConverterSerializeAndDeserializeSuccess()
        {
            RectInt original = new(1, 2, 3, 4);
            string json = Serializer.JsonStringify(original);
            RectInt deserialized = Serializer.JsonDeserialize<RectInt>(json);

            Assert.AreEqual(original.x, deserialized.x);
            Assert.AreEqual(original.y, deserialized.y);
            Assert.AreEqual(original.width, deserialized.width);
            Assert.AreEqual(original.height, deserialized.height);
        }

        [Test]
        public void RectIntConverterInvalidTokenTypeThrowsException()
        {
            string invalidJson = "\"not an object\"";
            Assert.Throws<JsonException>(() => Serializer.JsonDeserialize<RectInt>(invalidJson));
        }

        [Test]
        public void RectIntConverterUnknownPropertyThrowsException()
        {
            string invalidJson = "{\"x\":0,\"y\":0,\"width\":1,\"height\":1,\"extra\":0}";
            Assert.Throws<JsonException>(() => Serializer.JsonDeserialize<RectInt>(invalidJson));
        }

        [Test]
        public void RectIntConverterIncompleteJsonThrowsException()
        {
            string incompleteJson = "{\"x\":0,\"y\":0";
            Assert.Throws<JsonException>(() => Serializer.JsonDeserialize<RectInt>(incompleteJson));
        }

        [Test]
        public void BoundsConverterSerializeAndDeserializeSuccess()
        {
            Bounds original = new(new Vector3(1, 2, 3), new Vector3(4, 5, 6));
            string json = Serializer.JsonStringify(original);
            Bounds deserialized = Serializer.JsonDeserialize<Bounds>(json);

            Assert.AreEqual(original.center, deserialized.center);
            Assert.AreEqual(original.size, deserialized.size);
        }

        [Test]
        public void BoundsConverterInvalidTokenTypeThrowsException()
        {
            string invalidJson = "\"not an object\"";
            Assert.Throws<JsonException>(() => Serializer.JsonDeserialize<Bounds>(invalidJson));
        }

        [Test]
        public void BoundsConverterUnknownPropertyThrowsException()
        {
            string invalidJson =
                "{\"center\":{\"x\":0,\"y\":0,\"z\":0},\"size\":{\"x\":1,\"y\":1,\"z\":1},\"extra\":1}";
            Assert.Throws<JsonException>(() => Serializer.JsonDeserialize<Bounds>(invalidJson));
        }

        [Test]
        public void BoundsConverterIncompleteJsonThrowsException()
        {
            string incompleteJson = "{\"center\":{\"x\":0}}";
            Assert.Throws<JsonException>(() => Serializer.JsonDeserialize<Bounds>(incompleteJson));
        }

        [Test]
        public void BoundsConverterDefaultsWhenMissingSize()
        {
            string json = "{\"center\":{\"x\":1,\"y\":2,\"z\":3}}";
            Bounds deserialized = Serializer.JsonDeserialize<Bounds>(json);
            Assert.AreEqual(new Vector3(1, 2, 3), deserialized.center);
            Assert.AreEqual(Vector3.zero, deserialized.size);
        }

        [Test]
        public void BoundsIntConverterSerializeAndDeserializeSuccess()
        {
            BoundsInt original = new(new Vector3Int(1, 2, 3), new Vector3Int(4, 5, 6));
            string json = Serializer.JsonStringify(original);
            BoundsInt deserialized = Serializer.JsonDeserialize<BoundsInt>(json);

            Assert.AreEqual(original.position, deserialized.position);
            Assert.AreEqual(original.size, deserialized.size);
        }

        [Test]
        public void BoundsIntConverterInvalidTokenTypeThrowsException()
        {
            string invalidJson = "\"not an object\"";
            Assert.Throws<JsonException>(() => Serializer.JsonDeserialize<BoundsInt>(invalidJson));
        }

        [Test]
        public void BoundsIntConverterUnknownPropertyThrowsException()
        {
            string invalidJson =
                "{\"position\":{\"x\":0,\"y\":0,\"z\":0},\"size\":{\"x\":1,\"y\":1,\"z\":1},\"extra\":1}";
            Assert.Throws<JsonException>(() => Serializer.JsonDeserialize<BoundsInt>(invalidJson));
        }

        [Test]
        public void BoundsIntConverterIncompleteJsonThrowsException()
        {
            string incompleteJson = "{\"position\":{\"x\":0}}";
            Assert.Throws<JsonException>(() =>
                Serializer.JsonDeserialize<BoundsInt>(incompleteJson)
            );
        }

        [Test]
        public void BoundsIntConverterDefaultsWhenMissingSize()
        {
            string json = "{\"position\":{\"x\":1,\"y\":2,\"z\":3}}";
            BoundsInt deserialized = Serializer.JsonDeserialize<BoundsInt>(json);
            Assert.AreEqual(new Vector3Int(1, 2, 3), deserialized.position);
            Assert.AreEqual(Vector3Int.zero, deserialized.size);
        }

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
