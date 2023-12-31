namespace UnityHelpers.Core.Serialization
{
    using System;
    using System.ComponentModel;
    using Extension;
    using JsonConverters;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Text;
    using UnityEngine;

    internal static class SerializerEncoding
    {
        public static readonly Encoding Encoding = Encoding.Default;

        public static readonly JsonConverter[] Converters =
            {new StringEnumConverter(), Vector3Converter.Instance, Vector2Converter.Instance};

        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Converters = Converters
        };
        
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
                    throw new InvalidEnumArgumentException(nameof(serializationType), (int)serializationType, typeof(SerializationType));
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
                    throw new InvalidEnumArgumentException(nameof(serializationType), (int)serializationType, typeof(SerializationType));
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
            return (T) ProtoBuf.Serializer.Deserialize(type, memoryStream);
        }

        public static byte[] ProtoSerialize<T>(T input)
        {
            using MemoryStream memoryStream = new();
            ProtoBuf.Serializer.Serialize(memoryStream, input);
            return memoryStream.ToArray();
        }

        public static T JsonDeserialize<T>(string data)
        {
            return JsonConvert.DeserializeObject<T>(data);
        }

        public static byte[] JsonSerialize(object input)
        {
            return JsonStringify(input).GetBytes();
        }

        public static string JsonStringify(object input, bool pretty = false)
        {
            return JsonConvert.SerializeObject(input, pretty ? Formatting.Indented : Formatting.None,
                SerializerEncoding.Settings);
        }

        public static T ReadFromJsonFile<T>(string path)
        {
            string settingsAsText = File.ReadAllText(path, SerializerEncoding.Encoding);
            return JsonDeserialize<T>(settingsAsText);
        }

        public static void WriteToJsonFile<T>(T input, string path)
        {
            string jsonAsText = JsonUtility.ToJson(input);
            File.WriteAllText(path, jsonAsText);
        }
    }
}
