// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Serialization
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Core.Serialization.JsonConverters;

    public sealed class TypeConverterTests
    {
        private sealed class TypeHolder
        {
            public Type T { get; set; }
        }

        [Test]
        public void JsonTypeConverterResolvesTypes()
        {
            TypeHolder holder = new() { T = typeof(ReflectionHelpers) };
            JsonSerializerOptions options = new()
            {
                IncludeFields = true,
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter(), TypeConverter.Instance },
            };

            string json = JsonSerializer.Serialize(holder, options);
            TypeHolder roundtrip = JsonSerializer.Deserialize<TypeHolder>(json, options);
            Assert.IsNotNull(roundtrip);
            Assert.AreEqual(typeof(ReflectionHelpers), roundtrip.T);
        }
    }
}
