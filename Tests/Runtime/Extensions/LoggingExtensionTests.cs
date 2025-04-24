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
            go.Log($"Hello, world!");
            go.Log($"Hello, world, with exception!", new Exception("Test"));

            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            sr.Log($"Hello, world!");
            sr.Log($"Hello, world, with exception!", new Exception("Test"));
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

                assertion = message =>
                    Assert.IsTrue(message.Contains("Hello <color=#FF0000FF>world</color>"));
                go.Log($"Hello {"world":#red}");
                Assert.AreEqual(++expectedLogCount, logCount);
                Assert.IsNull(exception, exception?.ToString());

                assertion = message =>
                    Assert.IsTrue(message.Contains("Hello <color=#00FF00FF>world</color>"));
                go.Log($"Hello {"world":#green}");
                Assert.AreEqual(++expectedLogCount, logCount);
                Assert.IsNull(exception, exception?.ToString());

                assertion = message =>
                    Assert.IsTrue(message.Contains("Hello <color=#FFAABB>world</color>"));
                go.Log($"Hello {"world":#FFAABB}");
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
            GameObject go = new(nameof(ColorLogging), typeof(SpriteRenderer));

            int logCount = 0;
            Exception exception = null;
            Action<string> assertion = null;
            Application.logMessageReceived += HandleMessageReceived;

            try
            {
                int expectedLogCount = 0;

                assertion = message => Assert.IsTrue(message.Contains("Hello <b>world</b>"));
                go.Log($"Hello {"world":b}");
                Assert.AreEqual(++expectedLogCount, logCount);
                Assert.IsNull(exception, exception?.ToString());

                go.Log($"Hello {"world":bold}");
                Assert.AreEqual(++expectedLogCount, logCount);
                Assert.IsNull(exception, exception?.ToString());

                go.Log($"Hello {"world":!}");
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
            GameObject go = new(nameof(ColorLogging), typeof(SpriteRenderer));

            int logCount = 0;
            Exception exception = null;
            Action<string> assertion = null;
            Application.logMessageReceived += HandleMessageReceived;

            try
            {
                int expectedLogCount = 0;
                assertion = message => Assert.IsTrue(message.Contains("Hello [\"a\",\"b\",\"c\"]"));
                go.Log($"Hello {new List<string> { "a", "b", "c" }:json}");
                Assert.AreEqual(++expectedLogCount, logCount);
                Assert.IsNull(exception, exception?.ToString());

                assertion = message => Assert.IsTrue(message.Contains("Hello {}"));
                go.Log($"Hello {null:json}");
                Assert.AreEqual(++expectedLogCount, logCount);
                Assert.IsNull(exception, exception?.ToString());

                assertion = message => Assert.IsTrue(message.Contains("Hello [1,2,3,4]"));
                go.Log($"Hello {new[] { 1, 2, 3, 4 }:json}");
                Assert.AreEqual(++expectedLogCount, logCount);
                Assert.IsNull(exception, exception?.ToString());

                assertion = message => Assert.IsTrue(message.Contains("Hello {\"key\":\"value\"}"));
                go.Log($"Hello {new Dictionary<string, string> { ["key"] = "value" }:json}");
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
