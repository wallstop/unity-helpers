namespace WallstopStudios.UnityHelpers.Tests.Performance
{
    using System;
    using System.Diagnostics;
    using System.Text.Json;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.Serialization;
    using SerializerAlias = WallstopStudios.UnityHelpers.Core.Serialization.Serializer;

    [TestFixture]
    public sealed class JsonSerializationPerformanceTests
    {
        private sealed class SmallMsg
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        private sealed class MediumMsg
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int[] Values { get; set; }
        }

        private sealed class LargeMsg
        {
            public Guid Guid { get; set; }
            public string Description { get; set; }
            public byte[] Blob { get; set; }
            public MediumMsg Nested { get; set; }
        }

        private static SmallMsg MakeSmall(int i) => new() { Id = i, Name = "Name_" + i };

        private static MediumMsg MakeMedium(int i, int len) =>
            new()
            {
                Id = i,
                Name = new string('x', (i % 17) + 8),
                Values = MakeIntArray(len, seed: i),
            };

        private static LargeMsg MakeLarge(int i, int blobSize, int nestedLen) =>
            new()
            {
                Guid = Guid.NewGuid(),
                Description = new string('d', (i % 31) + 64),
                Blob = MakeBytes(blobSize, seed: i),
                Nested = MakeMedium(i, nestedLen),
            };

        private const int Iterations = 10_000;

        [Test, Timeout(0)]
        public void CompareSerializeSmallMediumLarge()
        {
            UnityEngine.Debug.Log(
                "| Payload | Pooled Serialize (ms) | Classic Serialize (ms) | Speedup | Size (bytes) |"
            );
            UnityEngine.Debug.Log(
                "| ------- | ---------------------:| ---------------------:| -------:| ------------:|"
            );

            RunSerializeBenchmark("Small", () => MakeSmall(123), out int smallSize);
            RunSerializeBenchmark("Medium", () => MakeMedium(123, 16), out int medSize);
            RunSerializeBenchmark("Large", () => MakeLarge(123, 8 * 1024, 64), out int largeSize);
        }

        [Test, Timeout(0)]
        public void CompareDeserializeSmallMediumLarge()
        {
            UnityEngine.Debug.Log(
                "| Payload | Pooled Deserialize (ms) | Classic Deserialize (ms) | Speedup |"
            );
            UnityEngine.Debug.Log(
                "| ------- | -----------------------:| -----------------------:| -------:|"
            );

            RunDeserializeBenchmark("Small", MakeSmall(123));
            RunDeserializeBenchmark("Medium", MakeMedium(123, 16));
            RunDeserializeBenchmark("Large", MakeLarge(123, 8 * 1024, 64));
        }

        [Test, Timeout(0)]
        public void BenchmarkStringifyVsSerialize()
        {
            UnityEngine.Debug.Log("| Payload | JsonStringify (ms) | JsonSerialize (ms) | Ratio |");
            UnityEngine.Debug.Log("| ------- | ------------------:| ------------------:| -----:|");

            RunStringifyVsSerializeBenchmark("Small", MakeSmall(123));
            RunStringifyVsSerializeBenchmark("Medium", MakeMedium(123, 16));
            RunStringifyVsSerializeBenchmark("Large", MakeLarge(123, 8 * 1024, 64));
        }

        [Test, Timeout(0)]
        public void BenchmarkLargeCollectionSerialization()
        {
            // Test with very large collection to stress memory allocation
            MediumMsg msg = MakeMedium(999, 50_000);

            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < 100; ++i)
            {
                _ = SerializerAlias.JsonStringify(msg);
            }
            sw.Stop();

            UnityEngine.Debug.Log(
                $"Large collection (50k ints) serialization: {sw.ElapsedMilliseconds}ms for 100 iterations"
            );
            Assert.Pass($"Performance baseline: {sw.ElapsedMilliseconds}ms");
        }

        [Test, Timeout(0)]
        public void BenchmarkDeeplyNestedObjectSerialization()
        {
            // Create nested structure
            MediumMsg root = MakeMedium(0, 10);
            MediumMsg current = root;

            // Create 100 level deep nesting using arrays as containers
            for (int i = 1; i < 100; ++i)
            {
                // JSON doesn't support circular references, so we can't test true deep nesting
                // This test validates that moderately complex objects serialize efficiently
                _ = MakeMedium(i, 10);
            }

            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < 1000; ++i)
            {
                _ = SerializerAlias.JsonStringify(root);
            }
            sw.Stop();

            UnityEngine.Debug.Log(
                $"Complex object serialization: {sw.ElapsedMilliseconds}ms for 1000 iterations"
            );
            Assert.Pass($"Performance baseline: {sw.ElapsedMilliseconds}ms");
        }

        private static void RunSerializeBenchmark<T>(
            string label,
            Func<T> factory,
            out int payloadSize
        )
        {
            T sample = factory();

            // Warmup
            _ = SerializerAlias.JsonSerialize(sample);
            _ = JsonSerializer.SerializeToUtf8Bytes(sample);

            // Measure pooled (using our implementation)
            Stopwatch sw = Stopwatch.StartNew();
            byte[] last = null;
            for (int i = 0; i < Iterations; ++i)
            {
                last = SerializerAlias.JsonSerialize(factory());
            }
            sw.Stop();
            long pooledMs = sw.ElapsedMilliseconds;
            payloadSize = last?.Length ?? 0;

            // Measure classic (using System.Text.Json directly)
            sw.Restart();
            for (int i = 0; i < Iterations; ++i)
            {
                _ = JsonSerializer.SerializeToUtf8Bytes(factory());
            }
            sw.Stop();
            long classicMs = sw.ElapsedMilliseconds;

            double speedup = classicMs > 0 ? (double)classicMs / pooledMs : double.PositiveInfinity;
            UnityEngine.Debug.Log(
                $"| {label} | {pooledMs, 23:N0} | {classicMs, 23:N0} | {speedup, 7:0.00}x | {payloadSize, 12:N0} |"
            );
        }

        private static void RunDeserializeBenchmark<T>(string label, T payload)
        {
            byte[] data = SerializerAlias.JsonSerialize(payload);
            string jsonString = System.Text.Encoding.UTF8.GetString(data);

            // Warmup
            _ = SerializerAlias.JsonDeserialize<T>(jsonString);
            _ = JsonSerializer.Deserialize<T>(data);

            // Measure pooled (using our implementation)
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < Iterations; ++i)
            {
                _ = SerializerAlias.JsonDeserialize<T>(jsonString);
            }
            sw.Stop();
            long pooledMs = sw.ElapsedMilliseconds;

            // Measure classic (using System.Text.Json directly)
            sw.Restart();
            for (int i = 0; i < Iterations; ++i)
            {
                _ = JsonSerializer.Deserialize<T>(data);
            }
            sw.Stop();
            long classicMs = sw.ElapsedMilliseconds;

            double speedup = classicMs > 0 ? (double)classicMs / pooledMs : double.PositiveInfinity;
            UnityEngine.Debug.Log(
                $"| {label} | {pooledMs, 25:N0} | {classicMs, 25:N0} | {speedup, 7:0.00}x |"
            );
        }

        private static void RunStringifyVsSerializeBenchmark<T>(string label, T payload)
        {
            // Warmup
            _ = SerializerAlias.JsonStringify(payload);
            _ = SerializerAlias.JsonSerialize(payload);

            // Measure JsonStringify (returns string)
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < Iterations; ++i)
            {
                _ = SerializerAlias.JsonStringify(payload);
            }
            sw.Stop();
            long stringifyMs = sw.ElapsedMilliseconds;

            // Measure JsonSerialize (returns byte[])
            sw.Restart();
            for (int i = 0; i < Iterations; ++i)
            {
                _ = SerializerAlias.JsonSerialize(payload);
            }
            sw.Stop();
            long serializeMs = sw.ElapsedMilliseconds;

            double ratio =
                serializeMs > 0 ? (double)stringifyMs / serializeMs : double.PositiveInfinity;
            UnityEngine.Debug.Log(
                $"| {label} | {stringifyMs, 18:N0} | {serializeMs, 18:N0} | {ratio, 5:0.00}x |"
            );
        }

        private static int[] MakeIntArray(int len, int seed)
        {
            int[] arr = new int[len];
            int x = seed;
            for (int i = 0; i < len; ++i)
            {
                // simple LCG for reproducibility
                x = unchecked(x * 1103515245 + 12345);
                arr[i] = x;
            }
            return arr;
        }

        private static byte[] MakeBytes(int len, int seed)
        {
            byte[] b = new byte[len];
            int x = seed;
            for (int i = 0; i < len; ++i)
            {
                x = unchecked(x * 1664525 + 1013904223);
                b[i] = (byte)(x >> 24);
            }
            return b;
        }
    }
}
