namespace UnityHelpers.Core.Serialization
{
    using System;
    using System.ComponentModel;
    using Extension;
    using JsonConverters;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;

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
                IncludeFields = true,
                Converters =
                {
                    new JsonStringEnumConverter(),
                    Vector3Converter.Instance,
                    Vector2Converter.Instance
                },
            };

            PrettyJsonOptions = new JsonSerializerOptions
            {
                IncludeFields = true,
                Converters =
                {
                    new JsonStringEnumConverter(),
                    Vector3Converter.Instance,
                    Vector2Converter.Instance
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
                        nameof(serializationType), (int)serializationType, typeof(SerializationType));
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
                        nameof(serializationType), (int)serializationType, typeof(SerializationType));
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

        public static T JsonDeserialize<T>(string data)
        {
            return JsonSerializer.Deserialize<T>(data, SerializerEncoding.NormalJsonOptions);
        }

        public static byte[] JsonSerialize<T>(T input)
        {
            return JsonStringify(input).GetBytes();
        }

        public static string JsonStringify<T>(T input, bool pretty = false)
        {
            JsonSerializerOptions options =
                pretty ? SerializerEncoding.PrettyJsonOptions : SerializerEncoding.NormalJsonOptions;
            if (typeof(T) == typeof(object))
            {
                object data = input;
                Type type = data?.GetType();
                return JsonSerializer.Serialize(data, data?.GetType(), options);
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