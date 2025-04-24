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
            go.Log($"Hello {"world":#red}");
            go.Log($"Hello {"world":#red}, with exception!", new Exception("Test"));
        }

        [Test]
        public void BoldLogging()
        {
            GameObject go = new(nameof(ColorLogging), typeof(SpriteRenderer));
            go.Log($"Hello {"world":b}");
        }

        [Test]
        public void JsonLogging()
        {
            GameObject go = new(nameof(ColorLogging), typeof(SpriteRenderer));
            go.Log($"Hello {new List<string> { "a", "b", "c" }:json}");
        }

        [Test]
        public void SizeLogging()
        {
            GameObject go = new(nameof(ColorLogging), typeof(SpriteRenderer));
            go.Log($"Hello {"world":40}");
        }
    }
}
