namespace WallstopStudios.UnityHelpers.Core.Serialization.JsonConverters
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using UnityEngine;

    public sealed class GameObjectConverter : JsonConverter<GameObject>
    {
        public static readonly GameObjectConverter Instance = new();

        private GameObjectConverter() { }

        public override GameObject Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            throw new NotImplementedException(nameof(Read));
        }

        public override void Write(
            Utf8JsonWriter writer,
            GameObject value,
            JsonSerializerOptions options
        )
        {
            writer.WriteStringValue(
                value == null
                    ? "{}"
                    : new { name = value.name, type = value.GetType().FullName }.ToString()
            );
        }
    }
}
