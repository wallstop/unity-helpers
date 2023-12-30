namespace Core.Serialization.JsonConverters
{
    using Extension;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using UnityEngine;

    public sealed class Vector2Converter : JsonConverter
    {
        public static readonly Vector2Converter Instance = new Vector2Converter();

        private Vector2Converter() { }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            JToken.FromObject(((Vector2)value).ToJsonString()).WriteTo(writer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            object instance = serializer.Deserialize(reader);
            return JsonConvert.DeserializeObject<Vector2>(instance.ToString());
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Vector2);
        }
    }
}
