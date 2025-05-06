namespace WallstopStudios.UnityHelpers.Tests.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Extension;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Core.Helper.Logging;

    public sealed class LoggingExtensionTests
    {
        [Test]
        public void Registration()
        {
            UnityLogTagFormatter formatter = new(createDefaultDecorators: false);
            Assert.AreEqual(
                0,
                formatter.Decorations.Count(),
                $"Found an unexpected number of registered decorations {formatter.Decorations.ToJson()}"
            );

            bool added = formatter.AddDecoration("b", value => $"<b>{value}</b>", "Bold");
            Assert.IsTrue(added);
            string formatted = formatter.Log($"{"Hello":b}", pretty: false);
            Assert.AreEqual("<b>Hello</b>", formatted);
            Assert.That(Enumerables.Of("Bold"), Is.EqualTo(formatter.Decorations));

            added = formatter.AddDecoration("b", value => $"<c>{value}</c>", "Bold");
            Assert.IsFalse(added);
            formatted = formatter.Log($"{"Hello":b}", pretty: false);
            Assert.AreEqual("<b>Hello</b>", formatted);
            Assert.That(Enumerables.Of("Bold"), Is.EqualTo(formatter.Decorations));

            added = formatter.AddDecoration("c", value => $"<c>{value}</c>", "Bold");
            Assert.IsFalse(added);
            formatted = formatter.Log($"{"Hello":b}", pretty: false);
            Assert.AreEqual("<b>Hello</b>", formatted);
            Assert.That(Enumerables.Of("Bold"), Is.EqualTo(formatter.Decorations));

            added = formatter.AddDecoration("c", value => $"<c>{value}</c>", "Bold1");
            Assert.IsTrue(added);
            formatted = formatter.Log($"{"Hello":b}", pretty: false);
            Assert.AreEqual("<b>Hello</b>", formatted);
            Assert.That(Enumerables.Of("Bold", "Bold1"), Is.EqualTo(formatter.Decorations));
            formatted = formatter.Log($"{"Hello":c}", pretty: false);
            Assert.AreEqual("<c>Hello</c>", formatted);
            Assert.That(Enumerables.Of("Bold", "Bold1"), Is.EqualTo(formatter.Decorations));

            added = formatter.AddDecoration("b", value => $"<c>{value}</c>", "Bold", force: true);
            Assert.IsTrue(added);
            Assert.That(Enumerables.Of("Bold", "Bold1"), Is.EqualTo(formatter.Decorations));
            formatted = formatter.Log($"{"Hello":b}", pretty: false);
            Assert.AreEqual("<c>Hello</c>", formatted);

            bool removed = formatter.RemoveDecoration("Bold", out _);
            Assert.IsTrue(removed);
            Assert.That(Enumerables.Of("Bold1"), Is.EqualTo(formatter.Decorations));
            formatted = formatter.Log($"{"Hello":b}", pretty: false);
            Assert.AreEqual("Hello", formatted);
            formatted = formatter.Log($"{"Hello":c}", pretty: false);
            Assert.AreEqual("<c>Hello</c>", formatted);
        }

        [Test]
        public void SimpleLogging()
        {
            GameObject go = new(nameof(SimpleLogging), typeof(SpriteRenderer));

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
                            Assert.IsTrue(message.Contains(nameof(SimpleLogging)), message);
                            Assert.IsTrue(message.Contains(nameof(GameObject)), message);
                            Assert.IsTrue(message.Contains("Hello, world!"), message);
                        }
                        else
                        {
                            Assert.AreEqual("Hello, world!", message);
                        }
                    };

                    go.Log($"Hello, world!", pretty: pretty);
                    Assert.AreEqual(++expectedLogCount, logCount);
                    Assert.IsNull(exception, exception?.ToString());

                    SpriteRenderer sr = go.GetComponent<SpriteRenderer>();

                    assertion = message =>
                    {
                        if (pretty)
                        {
                            Assert.IsTrue(message.Contains(nameof(SimpleLogging)), message);
                            Assert.IsTrue(message.Contains(nameof(SpriteRenderer)), message);
                            Assert.IsTrue(message.Contains("Hello, world!"), message);
                        }
                        else
                        {
                            Assert.AreEqual("Hello, world!", message);
                        }
                    };

                    sr.Log($"Hello, world!", pretty: pretty);

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
        public void ColorLogging()
        {
            GameObject go = new(nameof(ColorLogging), typeof(SpriteRenderer));

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
                            Assert.IsTrue(message.Contains(nameof(ColorLogging)), message);
                            Assert.IsTrue(message.Contains(nameof(GameObject)), message);
                            Assert.IsTrue(
                                message.Contains("Hello <color=#FF0000FF>world</color>"),
                                message
                            );
                        }
                        else
                        {
                            Assert.AreEqual("Hello <color=#FF0000FF>world</color>", message);
                        }
                    };
                    go.Log($"Hello {"world":#red}", pretty: pretty);
                    Assert.AreEqual(++expectedLogCount, logCount);
                    Assert.IsNull(exception, exception?.ToString());

                    assertion = message =>
                    {
                        if (pretty)
                        {
                            Assert.IsTrue(message.Contains(nameof(ColorLogging)), message);
                            Assert.IsTrue(message.Contains(nameof(GameObject)), message);
                            Assert.IsTrue(
                                message.Contains("Hello <color=#00FF00FF>world</color>"),
                                message
                            );
                        }
                        else
                        {
                            Assert.AreEqual("Hello <color=#00FF00FF>world</color>", message);
                        }
                    };
                    go.Log($"Hello {"world":#green}", pretty: pretty);
                    Assert.AreEqual(++expectedLogCount, logCount);
                    Assert.IsNull(exception, exception?.ToString());

                    assertion = message =>
                    {
                        if (pretty)
                        {
                            Assert.IsTrue(message.Contains(nameof(ColorLogging)), message);
                            Assert.IsTrue(message.Contains(nameof(GameObject)), message);
                            Assert.IsTrue(
                                message.Contains("Hello <color=#FFAABB>world</color>"),
                                message
                            );
                        }
                        else
                        {
                            Assert.AreEqual("Hello <color=#FFAABB>world</color>", message);
                        }
                    };
                    go.Log($"Hello {"world":#FFAABB}", pretty: pretty);
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
        public void BoldLogging()
        {
            GameObject go = new(nameof(BoldLogging), typeof(SpriteRenderer));

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
                            Assert.IsTrue(message.Contains(nameof(BoldLogging)), message);
                            Assert.IsTrue(message.Contains(nameof(GameObject)), message);
                            Assert.IsTrue(message.Contains("Hello <b>world</b>"), message);
                        }
                        else
                        {
                            Assert.AreEqual("Hello <b>world</b>", message);
                        }
                    };
                    go.Log($"Hello {"world":b}", pretty: pretty);
                    Assert.AreEqual(++expectedLogCount, logCount);
                    Assert.IsNull(exception, exception?.ToString());

                    go.Log($"Hello {"world":bold}", pretty: pretty);
                    Assert.AreEqual(++expectedLogCount, logCount);
                    Assert.IsNull(exception, exception?.ToString());

                    go.Log($"Hello {"world":!}", pretty: pretty);
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
        public void JsonLogging()
        {
            GameObject go = new(nameof(JsonLogging), typeof(SpriteRenderer));

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
                            Assert.IsTrue(message.Contains(nameof(JsonLogging)), message);
                            Assert.IsTrue(message.Contains(nameof(GameObject)), message);
                            Assert.IsTrue(message.Contains("Hello [\"a\",\"b\",\"c\"]"), message);
                        }
                        else
                        {
                            Assert.AreEqual("Hello [\"a\",\"b\",\"c\"]", message);
                        }
                    };

                    go.Log($"Hello {new List<string> { "a", "b", "c" }:json}", pretty: pretty);
                    Assert.AreEqual(++expectedLogCount, logCount);
                    Assert.IsNull(exception, exception?.ToString());

                    assertion = message =>
                    {
                        if (pretty)
                        {
                            Assert.IsTrue(message.Contains(nameof(JsonLogging)), message);
                            Assert.IsTrue(message.Contains(nameof(GameObject)), message);
                            Assert.IsTrue(message.Contains("Hello {}"), message);
                        }
                        else
                        {
                            Assert.AreEqual("Hello {}", message);
                        }
                    };
                    go.Log($"Hello {null:json}", pretty: pretty);
                    Assert.AreEqual(++expectedLogCount, logCount);
                    Assert.IsNull(exception, exception?.ToString());

                    assertion = message =>
                    {
                        if (pretty)
                        {
                            Assert.IsTrue(message.Contains(nameof(JsonLogging)), message);
                            Assert.IsTrue(message.Contains(nameof(GameObject)), message);
                            Assert.IsTrue(message.Contains("Hello [1,2,3,4]"), message);
                        }
                        else
                        {
                            Assert.AreEqual("Hello [1,2,3,4]", message);
                        }
                    };
                    go.Log($"Hello {new[] { 1, 2, 3, 4 }:json}", pretty: pretty);
                    Assert.AreEqual(++expectedLogCount, logCount);
                    Assert.IsNull(exception, exception?.ToString());

                    assertion = message =>
                    {
                        if (pretty)
                        {
                            Assert.IsTrue(message.Contains(nameof(JsonLogging)), message);
                            Assert.IsTrue(message.Contains(nameof(GameObject)), message);
                            Assert.IsTrue(message.Contains("Hello {\"key\":\"value\"}"), message);
                        }
                        else
                        {
                            Assert.AreEqual("Hello {\"key\":\"value\"}", message);
                        }
                    };
                    go.Log(
                        $"Hello {new Dictionary<string, string> { ["key"] = "value" }:json}",
                        pretty: pretty
                    );
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
        public void SizeLogging()
        {
            GameObject go = new(nameof(SizeLogging), typeof(SpriteRenderer));

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
                            Assert.IsTrue(message.Contains(nameof(SizeLogging)), message);
                            Assert.IsTrue(message.Contains(nameof(GameObject)), message);
                            Assert.IsTrue(message.Contains("Hello <size=40>world</size>"), message);
                        }
                        else
                        {
                            Assert.AreEqual("Hello <size=40>world</size>", message);
                        }
                    };
                    go.Log($"Hello {"world":40}", pretty: pretty);
                    Assert.AreEqual(++expectedLogCount, logCount);
                    Assert.IsNull(exception, exception?.ToString());

                    go.Log($"Hello {"world":size=40}", pretty: pretty);
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
        public void DateTimeNormalFormatTests()
        {
            GameObject go = new(nameof(DateTimeNormalFormatTests), typeof(SpriteRenderer));
            int logCount = 0;
            Exception exception = null;
            Action<string> assertion = null;
            Application.logMessageReceived += HandleMessageReceived;
            try
            {
                int expectedLogCount = 0;
                DateTime now = DateTime.UtcNow;
                foreach (bool pretty in new[] { true, false })
                {
                    assertion = message =>
                    {
                        if (pretty)
                        {
                            Assert.IsTrue(
                                message.Contains(nameof(DateTimeNormalFormatTests)),
                                message
                            );
                            Assert.IsTrue(message.Contains(nameof(GameObject)), message);
                            Assert.IsTrue(message.Contains($"Hello {now:O}"), message);
                        }
                        else
                        {
                            Assert.AreEqual($"Hello {now:O}", message);
                        }
                    };

                    go.Log($"Hello {now:O}", pretty: pretty);
                    Assert.AreEqual(++expectedLogCount, logCount);
                    Assert.IsNull(exception, exception?.ToString());

                    assertion = message =>
                    {
                        if (pretty)
                        {
                            Assert.IsTrue(
                                message.Contains(nameof(DateTimeNormalFormatTests)),
                                message
                            );
                            Assert.IsTrue(message.Contains(nameof(GameObject)), message);
                            Assert.IsTrue(
                                message.Contains($"Hello <size=40>{now}</size>"),
                                message
                            );
                        }
                        else
                        {
                            Assert.AreEqual($"Hello <size=40>{now}</size>", message);
                        }
                    };

                    go.Log($"Hello {now:40}", pretty: pretty);
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
        public void StackedTags()
        {
            GameObject go = new(nameof(StackedTags), typeof(SpriteRenderer));
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
                            Assert.IsTrue(message.Contains(nameof(StackedTags)), message);
                            Assert.IsTrue(message.Contains(nameof(GameObject)), message);
                            Assert.IsTrue(message.Contains("Hello <b>[1,2,3]</b>"), message);
                        }
                        else
                        {
                            Assert.AreEqual("Hello <b>[1,2,3]</b>", message);
                        }
                    };

                    go.Log($"Hello {new List<int> { 1, 2, 3 }:json,b}", pretty: pretty);
                    Assert.AreEqual(++expectedLogCount, logCount);
                    Assert.IsNull(exception, exception?.ToString());

                    assertion = message =>
                    {
                        if (pretty)
                        {
                            Assert.IsTrue(message.Contains(nameof(StackedTags)), message);
                            Assert.IsTrue(message.Contains(nameof(GameObject)), message);
                            Assert.IsTrue(
                                message.Contains("Hello <color=#FF0000FF><b>[1,2,3]</b></color>"),
                                message
                            );
                        }
                        else
                        {
                            Assert.AreEqual(
                                "Hello <color=#FF0000FF><b>[1,2,3]</b></color>",
                                message
                            );
                        }
                    };

                    go.Log($"Hello {new List<int> { 1, 2, 3 }:json,b,color=red}", pretty: pretty);
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
        public void TagsDeduplicate()
        {
            GameObject go = new(nameof(TagsDeduplicate), typeof(SpriteRenderer));
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
                            Assert.IsTrue(message.Contains(nameof(TagsDeduplicate)), message);
                            Assert.IsTrue(message.Contains(nameof(GameObject)), message);
                            Assert.IsTrue(message.Contains("Hello <b>[1,2,3]</b>"), message);
                        }
                        else
                        {
                            Assert.AreEqual("Hello <b>[1,2,3]</b>", message);
                        }
                    };

                    go.Log(
                        $"Hello {new List<int> { 1, 2, 3 }:json,b,bold,!,bold,b,!,b,bold}",
                        pretty: pretty
                    );
                    Assert.AreEqual(++expectedLogCount, logCount);
                    Assert.IsNull(exception, exception?.ToString());

                    assertion = message =>
                    {
                        if (pretty)
                        {
                            Assert.IsTrue(message.Contains(nameof(TagsDeduplicate)), message);
                            Assert.IsTrue(message.Contains(nameof(GameObject)), message);
                            Assert.IsTrue(
                                message.Contains("Hello <color=#FF0000FF><b>[1,2,3]</b></color>"),
                                message
                            );
                        }
                        else
                        {
                            Assert.AreEqual(
                                "Hello <color=#FF0000FF><b>[1,2,3]</b></color>",
                                message
                            );
                        }
                    };

                    go.Log(
                        $"Hello {new List<int> { 1, 2, 3 }:json,b,!,color=red,b,b,b,b,b,b,b}",
                        pretty: pretty
                    );
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
    }
}
