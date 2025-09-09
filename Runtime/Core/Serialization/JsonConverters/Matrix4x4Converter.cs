namespace WallstopStudios.UnityHelpers.Core.Serialization.JsonConverters
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using UnityEngine;

    public sealed class Matrix4x4Converter : JsonConverter<Matrix4x4>
    {
        private static readonly string[] PropertyNames =
        {
            "m00",
            "m01",
            "m02",
            "m03",
            "m10",
            "m11",
            "m12",
            "m13",
            "m20",
            "m21",
            "m22",
            "m23",
            "m30",
            "m31",
            "m32",
            "m33",
        };

        public static readonly Matrix4x4Converter Instance = new();

        private Matrix4x4Converter() { }

        public override Matrix4x4 Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected StartObject token when parsing Matrix4x4.");
            }

            float m00 = 0;
            float m01 = 0;
            float m02 = 0;
            float m03 = 0;
            float m10 = 0;
            float m11 = 0;
            float m12 = 0;
            float m13 = 0;
            float m20 = 0;
            float m21 = 0;
            float m22 = 0;
            float m23 = 0;
            float m30 = 0;
            float m31 = 0;
            float m32 = 0;
            float m33 = 0;

            bool[] found = new bool[16];

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException("Expected property name in Matrix4x4 JSON.");
                }
                string propertyName = reader.GetString();
                if (!reader.Read())
                {
                    throw new JsonException($"Expected value for property '{propertyName}'.");
                }
                float value;
                try
                {
                    value = reader.GetSingle();
                }
                catch (Exception ex)
                {
                    throw new JsonException($"Invalid value for property '{propertyName}'.", ex);
                }
                switch (propertyName)
                {
                    case "m00":
                        m00 = value;
                        found[0] = true;
                        break;
                    case "m01":
                        m01 = value;
                        found[1] = true;
                        break;
                    case "m02":
                        m02 = value;
                        found[2] = true;
                        break;
                    case "m03":
                        m03 = value;
                        found[3] = true;
                        break;
                    case "m10":
                        m10 = value;
                        found[4] = true;
                        break;
                    case "m11":
                        m11 = value;
                        found[5] = true;
                        break;
                    case "m12":
                        m12 = value;
                        found[6] = true;
                        break;
                    case "m13":
                        m13 = value;
                        found[7] = true;
                        break;
                    case "m20":
                        m20 = value;
                        found[8] = true;
                        break;
                    case "m21":
                        m21 = value;
                        found[9] = true;
                        break;
                    case "m22":
                        m22 = value;
                        found[10] = true;
                        break;
                    case "m23":
                        m23 = value;
                        found[11] = true;
                        break;
                    case "m30":
                        m30 = value;
                        found[12] = true;
                        break;
                    case "m31":
                        m31 = value;
                        found[13] = true;
                        break;
                    case "m32":
                        m32 = value;
                        found[14] = true;
                        break;
                    case "m33":
                        m33 = value;
                        found[15] = true;
                        break;
                }
            }

            for (int i = 0; i < found.Length; i++)
            {
                if (!found[i])
                {
                    throw new JsonException(
                        $"Missing property '{PropertyNames[i]}' for Matrix4x4."
                    );
                }
            }

            Matrix4x4 matrix = new()
            {
                m00 = m00,
                m01 = m01,
                m02 = m02,
                m03 = m03,
                m10 = m10,
                m11 = m11,
                m12 = m12,
                m13 = m13,
                m20 = m20,
                m21 = m21,
                m22 = m22,
                m23 = m23,
                m30 = m30,
                m31 = m31,
                m32 = m32,
                m33 = m33,
            };

            return matrix;
        }

        public override void Write(
            Utf8JsonWriter writer,
            Matrix4x4 matrix,
            JsonSerializerOptions options
        )
        {
            writer.WriteStartObject();

            writer.WriteNumber("m00", matrix.m00);
            writer.WriteNumber("m01", matrix.m01);
            writer.WriteNumber("m02", matrix.m02);
            writer.WriteNumber("m03", matrix.m03);
            writer.WriteNumber("m10", matrix.m10);
            writer.WriteNumber("m11", matrix.m11);
            writer.WriteNumber("m12", matrix.m12);
            writer.WriteNumber("m13", matrix.m13);
            writer.WriteNumber("m20", matrix.m20);
            writer.WriteNumber("m21", matrix.m21);
            writer.WriteNumber("m22", matrix.m22);
            writer.WriteNumber("m23", matrix.m23);
            writer.WriteNumber("m30", matrix.m30);
            writer.WriteNumber("m31", matrix.m31);
            writer.WriteNumber("m32", matrix.m32);
            writer.WriteNumber("m33", matrix.m33);

            writer.WriteEndObject();
        }
    }
}
