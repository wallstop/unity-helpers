namespace WallstopStudios.UnityHelpers.Core.Serialization.JsonConverters
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public sealed class TypeConverter : JsonConverter<Type>
    {
        public static readonly TypeConverter Instance = new();

        private TypeConverter() { }

        public override Type Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            string typeName = reader.GetString();
            return string.IsNullOrWhiteSpace(typeName) ? null : Type.GetType(typeName);
        }

        public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value?.AssemblyQualifiedName);
        }
    }
}
