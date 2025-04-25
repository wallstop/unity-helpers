namespace UnityHelpers.Tests.Extensions
{
    using System;
    using System.Collections.Generic;
    using Core.Extension;
    using NUnit.Framework;
    using UnityEngine;

    public sealed class LoggingExtensionTests
    {
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
            GameObject go = new(nameof(ColorLogging), typeof(SpriteRenderer));
            go.Log($"Hello {"world":40}");
        }

        [Test]
        public void DateTimeNormalFormatTests()
        {
            GameObject go = new(nameof(ColorLogging), typeof(SpriteRenderer));
            go.Log($"Hello {DateTime.UtcNow:O}");
        }

        [Test]
        public void LinkTests()
        {
            GameObject go = new(nameof(ColorLogging), typeof(SpriteRenderer));
            go.Log($"Hello {"world":link=UnityLogTagFormatter.cs}");
        }
    }
}
