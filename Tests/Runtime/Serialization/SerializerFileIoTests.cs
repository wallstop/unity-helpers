// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Serialization
{
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Serialization;
    using WallstopStudios.UnityHelpers.Tests.Core;

    [TestFixture]
    [NUnit.Framework.Category("Slow")]
    [NUnit.Framework.Category("Integration")]
    public sealed class SerializerFileIoTests : CommonTestBase
    {
        private string _dir;

        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            _dir = Path.Combine(Application.persistentDataPath, "SerializerFileIoTests");
            if (Directory.Exists(_dir))
            {
                Directory.Delete(_dir, recursive: true);
            }
            Directory.CreateDirectory(_dir);
        }

        [TearDown]
        public override void TearDown()
        {
            try
            {
                if (Directory.Exists(_dir))
                {
                    Directory.Delete(_dir, recursive: true);
                }
            }
            catch { }
            base.TearDown();
        }

        private sealed class Sample
        {
            public int a;
            public string b;
        }

        [Test]
        public void TryWriteAndTryReadRoundTrip()
        {
            string path = Path.Combine(_dir, "sample.json");
            Sample s = new() { a = 7, b = "test" };

            bool wrote = Serializer.TryWriteToJsonFile(s, path, pretty: true);
            Assert.IsTrue(wrote, "Expected TryWriteToJsonFile to succeed.");
            Assert.IsTrue(File.Exists(path), "Expected file to be created.");

            bool read = Serializer.TryReadFromJsonFile(path, out Sample loaded);
            Assert.IsTrue(read, "Expected TryReadFromJsonFile to succeed.");
            Assert.NotNull(loaded);
            Assert.AreEqual(7, loaded.a);
            Assert.AreEqual("test", loaded.b);
        }

        [Test]
        public void TryReadReturnsFalseWhenMissing()
        {
            string path = Path.Combine(_dir, "does_not_exist.json");
            bool read = Serializer.TryReadFromJsonFile(path, out Sample loaded);
            Assert.IsFalse(read);
            Assert.IsTrue(loaded == null, "Loaded object should be null when file is missing");
        }

        [Test]
        public void ReadAsyncHonorsCancellation()
        {
            string path = Path.Combine(_dir, "big.json");
            // Create a moderately large file
            File.WriteAllText(path, new string('x', 200_000));

            using CancellationTokenSource cts = new();
            cts.Cancel();
            Assert.Throws<TaskCanceledException>(() =>
                Serializer
                    .ReadFromJsonFileAsync<Sample>(path, cts.Token)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult()
            );
        }

        [Test]
        public void WriteAsyncHonorsCancellation()
        {
            string path = Path.Combine(_dir, "out.json");
            using CancellationTokenSource cts = new();
            cts.Cancel();
            Assert.Throws<TaskCanceledException>(() =>
                Serializer
                    .WriteToJsonFileAsync(new Sample { a = 1, b = "x" }, path, true, cts.Token)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult()
            );
        }
    }
}
