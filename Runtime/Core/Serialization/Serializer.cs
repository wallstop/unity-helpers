namespace WallstopStudios.UnityHelpers.Core.Serialization
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using Extension;
    using JsonConverters;
    using TypeConverter = JsonConverters.TypeConverter;

    internal static class SerializerEncoding
    {
        public static readonly Encoding Encoding;
        public static readonly JsonSerializerOptions NormalJsonOptions;
        public static readonly JsonSerializerOptions PrettyJsonOptions;

        static SerializerEncoding()
        {
            Encoding = Encoding.UTF8;
            NormalJsonOptions = new JsonSerializerOptions
            {
                IgnoreReadOnlyFields = false,
                IgnoreReadOnlyProperties = false,
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                IncludeFields = true,
                PropertyNameCaseInsensitive = true,
                Converters =
                {
                    new JsonStringEnumConverter(),
                    Vector3Converter.Instance,
                    Vector2Converter.Instance,
                    Vector4Converter.Instance,
                    Matrix4x4Converter.Instance,
                    TypeConverter.Instance,
                    GameObjectConverter.Instance,
                    ColorConverter.Instance,
                },
            };

            PrettyJsonOptions = new JsonSerializerOptions
            {
                IgnoreReadOnlyFields = false,
                IgnoreReadOnlyProperties = false,
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                PropertyNameCaseInsensitive = true,
                IncludeFields = true,
                Converters =
                {
                    new JsonStringEnumConverter(),
                    Vector3Converter.Instance,
                    Vector2Converter.Instance,
                    Vector4Converter.Instance,
                    Matrix4x4Converter.Instance,
                    TypeConverter.Instance,
                    GameObjectConverter.Instance,
                    ColorConverter.Instance,
                },
                WriteIndented = true,
            };
        }
    }

    public enum SerializationType
    {
        SystemBinary,
        Protobuf,
    }

    public static class Serializer
    {
        public static T Deserialize<T>(byte[] serialized, SerializationType serializationType)
        {
            switch (serializationType)
            {
                case SerializationType.SystemBinary:
                    return BinaryDeserialize<T>(serialized);
                case SerializationType.Protobuf:
                    return ProtoDeserialize<T>(serialized);
                default:
                    throw new InvalidEnumArgumentException(
                        nameof(serializationType),
                        (int)serializationType,
                        typeof(SerializationType)
                    );
            }
        }

        public static byte[] Serialize<T>(T instance, SerializationType serializationType)
        {
            switch (serializationType)
            {
                case SerializationType.SystemBinary:
                    return BinarySerialize(instance);
                case SerializationType.Protobuf:
                    return ProtoSerialize(instance);
                default:
                    throw new InvalidEnumArgumentException(
                        nameof(serializationType),
                        (int)serializationType,
                        typeof(SerializationType)
                    );
            }
        }

        public static T BinaryDeserialize<T>(byte[] data)
        {
            using MemoryStream memoryStream = new(data);
            BinaryFormatter binaryFormatter = new();
            memoryStream.Position = 0;
            return (T)binaryFormatter.Deserialize(memoryStream);
        }

        public static byte[] BinarySerialize<T>(T input)
        {
            using MemoryStream memoryStream = new();
            BinaryFormatter binaryFormatter = new();
            binaryFormatter.Serialize(memoryStream, input);
            return memoryStream.ToArray();
        }

        public static T ProtoDeserialize<T>(byte[] data)
        {
            using MemoryStream memoryStream = new(data);
            return ProtoBuf.Serializer.Deserialize<T>(memoryStream);
        }

        public static T ProtoDeserialize<T>(byte[] data, Type type)
        {
            using MemoryStream memoryStream = new(data);
            return (T)ProtoBuf.Serializer.Deserialize(type, memoryStream);
        }

        public static byte[] ProtoSerialize<T>(T input)
        {
            using MemoryStream memoryStream = new();
            ProtoBuf.Serializer.Serialize(memoryStream, input);
            return memoryStream.ToArray();
        }

        public static T JsonDeserialize<T>(string data, Type type = null)
        {
            return (T)
                JsonSerializer.Deserialize(
                    data,
                    type ?? typeof(T),
                    SerializerEncoding.NormalJsonOptions
                );
        }

        public static byte[] JsonSerialize<T>(T input)
        {
            return JsonStringify(input).GetBytes();
        }

        public static string JsonStringify<T>(T input, bool pretty = false)
        {
            JsonSerializerOptions options = pretty
                ? SerializerEncoding.PrettyJsonOptions
                : SerializerEncoding.NormalJsonOptions;
            Type parameterType = typeof(T);
            if (
                parameterType.IsAbstract
                || parameterType.IsInterface
                || parameterType == typeof(object)
            )
            {
                object data = input;
                if (data == null)
                {
                    return "{}";
                }

                Type type = data.GetType();
                return JsonSerializer.Serialize(data, type, options);
            }

            return JsonSerializer.Serialize(input, options);
        }

        public static T ReadFromJsonFile<T>(string path)
        {
            string settingsAsText = File.ReadAllText(path, SerializerEncoding.Encoding);
            return JsonDeserialize<T>(settingsAsText);
        }

        public static void WriteToJsonFile<T>(T input, string path, bool pretty = true)
        {
            string jsonAsText = JsonStringify(input, pretty);
            File.WriteAllText(path, jsonAsText);
        }
    }
}
