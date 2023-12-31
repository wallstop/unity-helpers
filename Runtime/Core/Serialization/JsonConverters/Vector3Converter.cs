namespace UnityHelpers.Core.Serialization.JsonConverters
{
    using Extension;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using UnityEngine;

    public sealed class Vector3Converter : JsonConverter
    {
        public static readonly Vector3Converter Instance = new Vector3Converter();

        private Vector3Converter() { }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            JToken.FromObject(((Vector3)value).ToJsonString()).WriteTo(writer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            object instance = serializer.Deserialize(reader);
            return JsonConvert.DeserializeObject<Vector3>(instance.ToString());
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Vector3);
        }
    }
}
