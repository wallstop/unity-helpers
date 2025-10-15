namespace WallstopStudios.UnityHelpers.Tests.Extensions
{
    using System;
    using System.Text.RegularExpressions;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Extension;
    using WallstopStudios.UnityHelpers.Core.Helper.Logging;
    using WallstopStudios.UnityHelpers.Tests.TestUtils;

    public sealed class UnityLogTagFormatterEdgeTests : CommonTestBase
    {
        [Test]
        public void UnknownTagFallsBack()
        {
            GameObject go = Track(
                new GameObject(nameof(UnknownTagFallsBack), typeof(SpriteRenderer))
            );

            int logCount = 0;
            Exception exception = null;
            Action<string> assertion = null;
            Application.logMessageReceived += HandleMessageReceived;

            try
            {
                int expectedLogCount = 0;
                foreach (bool pretty in new[] { true, false })
                {
                    assertion = message =>
                    {
                        if (pretty)
                        {
                            Assert.IsTrue(message.Contains(nameof(UnknownTagFallsBack)), message);
                            Assert.IsTrue(message.Contains(nameof(GameObject)), message);
                            Assert.IsTrue(message.Contains("Hello world"), message);
                        }
                        else
                        {
                            Assert.AreEqual("Hello world", message);
                        }
                    };

                    go.Log($"Hello {"world":does_not_exist}", pretty: pretty);
                    Assert.AreEqual(++expectedLogCount, logCount);
                    Assert.IsNull(exception, exception?.ToString());
                }
            }
            finally
            {
                Application.logMessageReceived -= HandleMessageReceived;
            }

            return;

            void HandleMessageReceived(string message, string stackTrace, LogType type)
            {
                ++logCount;
                try
                {
                    assertion?.Invoke(message);
                }
                catch (Exception e)
                {
                    exception = e;
                    throw;
                }
            }
        }

        [Test]
        public void RepeatedSeparatorsAreIgnored()
        {
            GameObject go = Track(
                new GameObject(nameof(RepeatedSeparatorsAreIgnored), typeof(SpriteRenderer))
            );

            int logCount = 0;
            Exception exception = null;
            Action<string> assertion = null;
            Application.logMessageReceived += HandleMessageReceived;

            try
            {
                int expectedLogCount = 0;
                foreach (bool pretty in new[] { true, false })
                {
                    assertion = message =>
                    {
                        if (pretty)
                        {
                            Assert.IsTrue(
                                message.Contains(nameof(RepeatedSeparatorsAreIgnored)),
                                message
                            );
                            Assert.IsTrue(message.Contains(nameof(GameObject)), message);
                            Assert.IsTrue(message.Contains("<b>value</b>"), message);
                        }
                        else
                        {
                            Assert.AreEqual("<b>value</b>", message);
                        }
                    };

                    go.Log($"{"value":b,,,,,}", pretty: pretty);
                    Assert.AreEqual(++expectedLogCount, logCount);
                    Assert.IsNull(exception, exception?.ToString());
                }
            }
            finally
            {
                Application.logMessageReceived -= HandleMessageReceived;
            }

            return;

            void HandleMessageReceived(string message, string stackTrace, LogType type)
            {
                ++logCount;
                try
                {
                    assertion?.Invoke(message);
                }
                catch (Exception e)
                {
                    exception = e;
                    throw;
                }
            }
        }

        [Test]
        public void ExceptionLoggingFormatsOutput()
        {
            UnityLogTagFormatter formatter = new();
            Exception testException = new("Boom");

            int logCount = 0;
            Exception exception = null;
            Action<string, LogType> assertion = null;
            Application.logMessageReceived += HandleMessageReceived;

            try
            {
                int expectedLogCount = 0;

                assertion = (message, type) =>
                {
                    Assert.AreEqual(LogType.Log, type);
                    Assert.IsTrue(message.Contains("NO_NAME[NO_TYPE]"), message);
                    Assert.IsTrue(message.Contains("Hello"), message);
                    Assert.IsTrue(message.Contains("Boom"), message);
                };
                formatter.Log($"Hello", context: null, e: testException, pretty: true);
                Assert.AreEqual(++expectedLogCount, logCount);
                Assert.IsNull(exception, exception?.ToString());

                assertion = (message, type) =>
                {
                    Assert.AreEqual(LogType.Warning, type);
                    Assert.IsTrue(message.Contains("Hello"), message);
                    Assert.IsTrue(message.Contains("Boom"), message);
                };
                // Mark the upcoming warning log as expected so the test runner doesn't flag it.
                LogAssert.Expect(LogType.Warning, new Regex("Hello[\n\r]+.*Boom"));
                formatter.LogWarn($"Hello", context: null, e: testException, pretty: false);
                Assert.AreEqual(++expectedLogCount, logCount);
                Assert.IsNull(exception, exception?.ToString());

                assertion = (message, type) =>
                {
                    Assert.AreEqual(LogType.Error, type);
                    Assert.IsTrue(message.Contains("Hello"), message);
                    Assert.IsTrue(message.Contains("Boom"), message);
                };
                // Mark the upcoming error log as expected so the test runner doesn't flag it.
                LogAssert.Expect(LogType.Error, new Regex("Hello[\n\r]+.*Boom"));
                formatter.LogError($"Hello", context: null, e: testException, pretty: false);
                Assert.AreEqual(++expectedLogCount, logCount);
                Assert.IsNull(exception, exception?.ToString());
            }
            finally
            {
                Application.logMessageReceived -= HandleMessageReceived;
            }

            return;

            void HandleMessageReceived(string message, string stackTrace, LogType type)
            {
                ++logCount;
                try
                {
                    assertion?.Invoke(message, type);
                }
                catch (Exception e)
                {
                    exception = e;
                    throw;
                }
            }
        }

        [Test]
        public void CustomDecorationPriorityControlsOrder()
        {
            UnityLogTagFormatter formatter = new(createDefaultDecorators: false);

            formatter.AddDecoration(
                predicate: x => string.Equals(x, "x", StringComparison.OrdinalIgnoreCase),
                format: (_, v) => $"<A>{v}</A>",
                tag: "A",
                priority: 10,
                editorOnly: false,
                force: true
            );

            formatter.AddDecoration(
                predicate: x => string.Equals(x, "x", StringComparison.OrdinalIgnoreCase),
                format: (_, v) => $"<B>{v}</B>",
                tag: "B",
                priority: -10,
                editorOnly: false,
                force: true
            );

            string formatted = formatter.Log($"{"value":x}", pretty: false);
            Assert.AreEqual("<A><B>value</B></A>", formatted);
        }

        [Test]
        public void ForceOverrideMovesPriority()
        {
            UnityLogTagFormatter formatter = new(createDefaultDecorators: false);

            formatter.AddDecoration(
                match: "demo",
                format: v => $"<P5>{v}</P5>",
                tag: "Demo",
                priority: 5,
                editorOnly: false,
                force: true
            );
            string formatted = formatter.Log($"{"value":demo}", pretty: false);
            Assert.AreEqual("<P5>value</P5>", formatted);

            formatter.AddDecoration(
                match: "demo",
                format: v => $"<P1>{v}</P1>",
                tag: "Demo",
                priority: 1,
                editorOnly: false,
                force: true
            );
            formatted = formatter.Log($"{"value":demo}", pretty: false);
            Assert.AreEqual("<P1>value</P1>", formatted);
        }

        [Test]
        public void ForceOverrideAtSamePriorityReplacesFormatter()
        {
            UnityLogTagFormatter formatter = new(createDefaultDecorators: false);

            formatter.AddDecoration(
                match: "demo",
                format: value => $"<Initial>{value}</Initial>",
                tag: "Demo",
                priority: 3,
                editorOnly: false,
                force: true
            );
            formatter.AddDecoration(
                match: "demo",
                format: value => $"<Updated>{value}</Updated>",
                tag: "Demo",
                priority: 3,
                editorOnly: false,
                force: true
            );

            string formatted = formatter.Log($"{"value":demo}", pretty: false);
            Assert.AreEqual("<Updated>value</Updated>", formatted);
        }

        [Test]
        public void DuplicateTagWithoutForceReturnsFalse()
        {
            UnityLogTagFormatter formatter = new(createDefaultDecorators: false);

            bool firstResult = formatter.AddDecoration(
                match: "demo",
                format: value => $"<Initial>{value}</Initial>",
                tag: "Demo",
                priority: 0,
                editorOnly: false,
                force: false
            );
            Assert.IsTrue(firstResult);

            bool secondResult = formatter.AddDecoration(
                match: "demo",
                format: value => $"<Ignored>{value}</Ignored>",
                tag: "Demo",
                priority: 10,
                editorOnly: false,
                force: false
            );
            Assert.IsFalse(secondResult);

            string formatted = formatter.Log($"{"value":demo}", pretty: false);
            Assert.AreEqual("<Initial>value</Initial>", formatted);
        }

        [Test]
        public void RemoveDecorationRemovesFormatter()
        {
            UnityLogTagFormatter formatter = new(createDefaultDecorators: false);

            formatter.AddDecoration(
                match: "demo",
                format: value => $"<Removed>{value}</Removed>",
                tag: "Demo",
                priority: 0,
                editorOnly: false,
                force: true
            );

            (
                string tag,
                bool editorOnly,
                Func<string, bool> predicate,
                Func<string, object, string> formatterDelegate
            ) removedDecoration;
            bool removed = formatter.RemoveDecoration("Demo", out removedDecoration);

            Assert.IsTrue(removed);
            Assert.AreEqual("Demo", removedDecoration.tag);

            string formatted = formatter.Log($"{"value":demo}", pretty: false);
            Assert.AreEqual("value", formatted);
        }
    }
}
