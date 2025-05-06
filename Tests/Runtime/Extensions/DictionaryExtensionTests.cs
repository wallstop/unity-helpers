namespace WallstopStudios.UnityHelpers.Tests.Extensions
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Extension;
    using NUnit.Framework;

    public sealed class DictionaryExtensionTests
    {
        [Test]
        public void GetOrAddValueProducer()
        {
            Dictionary<string, int> dictionary = new();
            int value = dictionary.GetOrAdd("test", () => 100);
            Assert.AreEqual(100, value);
            Assert.AreEqual(value, dictionary["test"]);

            int newValue = dictionary.GetOrAdd(
                "test",
                () =>
                {
                    Assert.Fail("Value Producer should not have been called!");
                    return 200;
                }
            );
            Assert.AreEqual(100, newValue);

            newValue = dictionary.GetOrAdd("test2", () => 300);
            Assert.AreEqual(300, newValue);
            Assert.AreEqual(100, dictionary["test"]);
        }

        [Test]
        public void GetOrAddKeyValueProducer()
        {
            Dictionary<string, int> dictionary = new();
            int value = dictionary.GetOrAdd(
                "test",
                key =>
                {
                    Assert.AreEqual("test", key);
                    return 100;
                }
            );
            Assert.AreEqual(100, value);
            Assert.AreEqual(value, dictionary["test"]);

            int newValue = dictionary.GetOrAdd(
                "test",
                key =>
                {
                    Assert.Fail("Value Producer should not have been called!");
                    return 200;
                }
            );
            Assert.AreEqual(100, newValue);

            newValue = dictionary.GetOrAdd(
                "test2",
                key =>
                {
                    Assert.AreEqual("test2", key);
                    return 300;
                }
            );
            Assert.AreEqual(300, newValue);
            Assert.AreEqual(100, dictionary["test"]);
        }

        [Test]
        public void GetOrElseValue()
        {
            Dictionary<string, int> dictionary = new();
            int value = dictionary.GetOrElse("test", 100);
            Assert.AreEqual(100, value);
            Assert.IsFalse(dictionary.ContainsKey("test"));
            dictionary["test"] = 150;
            value = dictionary.GetOrElse("test", 100);
            Assert.AreEqual(150, value);
            Assert.AreEqual(150, dictionary["test"]);
        }

        [Test]
        public void GetOrElseValueProducer()
        {
            Dictionary<string, int> dictionary = new();
            int value = dictionary.GetOrElse("test", () => 100);
            Assert.AreEqual(100, value);
            Assert.IsFalse(dictionary.ContainsKey("test"));
            dictionary["test"] = 150;
            value = dictionary.GetOrElse(
                "test",
                () =>
                {
                    Assert.Fail("Producer should not be called.");
                    return 100;
                }
            );
            Assert.AreEqual(150, value);
            Assert.AreEqual(150, dictionary["test"]);
        }

        [Test]
        public void GetOrElseKeyValueProducer()
        {
            Dictionary<string, int> dictionary = new();
            int value = dictionary.GetOrElse(
                "test",
                key =>
                {
                    Assert.AreEqual("test", key);
                    return 100;
                }
            );
            Assert.AreEqual(100, value);
            Assert.IsFalse(dictionary.ContainsKey("test"));
            dictionary["test"] = 150;
            value = dictionary.GetOrElse(
                "test",
                () =>
                {
                    Assert.Fail("Producer should not be called.");
                    return 100;
                }
            );
            Assert.AreEqual(150, value);
            Assert.AreEqual(150, dictionary["test"]);
        }

        [Test]
        public void GetOrAddNew()
        {
            Dictionary<string, List<int>> dictionary = new();
            List<int> value = dictionary.GetOrAdd("test");
            Assert.IsNotNull(value);
            Assert.AreEqual(value, dictionary["test"]);
            value.Add(1);

            List<int> newValue = dictionary.GetOrAdd("test");
            Assert.AreEqual(value, newValue);
        }

        [Test]
        public void GetOrElse()
        {
            IReadOnlyDictionary<string, int> dictionary = new Dictionary<string, int>();
            int value = dictionary.GetOrElse("test", 100);
            Assert.AreEqual(100, value);
            Assert.AreEqual(0, dictionary.Count);
        }

        [Test]
        public void AddOrUpdate()
        {
            Dictionary<string, int> dictionary = new();
            int value = dictionary.AddOrUpdate("test", key => 100, (key, existing) => existing + 1);
            Assert.AreEqual(100, value);
            Assert.AreEqual(100, dictionary["test"]);
            int expected = value;
            for (int i = 0; i < 100; ++i)
            {
                value = dictionary.AddOrUpdate("test", key => 100, (key, existing) => existing + 1);
                Assert.AreEqual(++expected, value);
                Assert.AreEqual(value, dictionary["test"]);
            }

            value = dictionary.AddOrUpdate("test2", key => 150, (key, existing) => existing + 1);
            Assert.AreEqual(150, value);
            Assert.AreEqual(150, dictionary["test2"]);
            Assert.AreEqual(expected, dictionary["test"]);
        }

        [Test]
        public void TryAdd()
        {
            Dictionary<string, int> dictionary = new();
            int value = dictionary.TryAdd(
                "test",
                key =>
                {
                    Assert.AreEqual("test", key);
                    return 150;
                }
            );
            Assert.AreEqual(150, value);
            Assert.AreEqual(value, dictionary["test"]);

            value = dictionary.TryAdd(
                "test",
                key =>
                {
                    Assert.Fail("Creator should not have been called.");
                    return 200;
                }
            );
            Assert.AreEqual(150, value);
            Assert.AreEqual(value, dictionary["test"]);

            value = dictionary.TryAdd(
                "test2",
                key =>
                {
                    Assert.AreEqual("test2", key);
                    return 350;
                }
            );
            Assert.AreEqual(350, value);
            Assert.AreEqual(value, dictionary["test2"]);
            Assert.AreEqual(150, dictionary["test"]);
        }

        [Test]
        public void Merge()
        {
            IReadOnlyDictionary<string, int> left = new Dictionary<string, int>()
            {
                ["only-on-left"] = 1,
                ["both"] = 1,
            };

            IReadOnlyDictionary<string, int> right = new Dictionary<string, int>()
            {
                ["only-on-right"] = 3,
                ["both"] = 2,
            };

            Dictionary<string, int> merged = left.Merge(right);
            Assert.AreEqual(3, merged.Count);
            Assert.AreEqual(1, merged["only-on-left"]);
            Assert.AreEqual(2, merged["both"]);
            Assert.AreEqual(3, merged["only-on-right"]);

            merged = right.Merge(left);
            Assert.AreEqual(3, merged.Count);
            Assert.AreEqual(1, merged["only-on-left"]);
            Assert.AreEqual(1, merged["both"]);
            Assert.AreEqual(3, merged["only-on-right"]);

            merged = left.Merge(left);
            Assert.AreEqual(2, merged.Count);
            Assert.AreEqual(1, merged["only-on-left"]);
            Assert.AreEqual(1, merged["both"]);

            merged = right.Merge(right);
            Assert.AreEqual(2, merged.Count);
            Assert.AreEqual(3, merged["only-on-right"]);
            Assert.AreEqual(2, merged["both"]);
        }

        [Test]
        public void Difference()
        {
            IReadOnlyDictionary<string, int> left = new Dictionary<string, int>()
            {
                ["only-on-left"] = 1,
                ["both"] = 1,
            };

            IReadOnlyDictionary<string, int> right = new Dictionary<string, int>()
            {
                ["only-on-right"] = 3,
                ["both"] = 2,
            };

            Dictionary<string, int> difference = left.Difference(right);
            Assert.AreEqual(2, difference.Count);
            Assert.AreEqual(2, difference["both"]);
            Assert.AreEqual(3, difference["only-on-right"]);

            difference = right.Difference(left);
            Assert.AreEqual(2, difference.Count);
            Assert.AreEqual(1, difference["both"]);
            Assert.AreEqual(1, difference["only-on-left"]);

            difference = left.Difference(left);
            Assert.AreEqual(0, difference.Count);

            difference = right.Difference(right);
            Assert.AreEqual(0, difference.Count);
        }

        [Test]
        public void Reverse()
        {
            IReadOnlyDictionary<string, int> initial = new Dictionary<string, int>()
            {
                ["one"] = 1,
                ["one-duplicate"] = 1,
                ["two"] = 2,
                ["three"] = 3,
            };

            Dictionary<int, string> reversed = initial.Reverse();
            Assert.AreEqual(3, reversed.Count);
            Assert.AreEqual("one-duplicate", reversed[1]);
            Assert.AreEqual("two", reversed[2]);
            Assert.AreEqual("three", reversed[3]);
        }

        [Test]
        public void ToDictionaryFromDictionary()
        {
            IReadOnlyDictionary<string, int> initial = new Dictionary<string, int>()
            {
                ["test"] = 1,
                ["test2"] = 2,
            };

            Dictionary<string, int> copy = initial.ToDictionary();
            Assert.AreEqual(2, copy.Count);
            Assert.AreEqual(1, copy["test"]);
            Assert.AreEqual(2, copy["test2"]);
        }

        [Test]
        public void ToDictionaryFromEnumerableKeyValuePair()
        {
            IEnumerable<KeyValuePair<string, int>> initial = new Dictionary<string, int>()
            {
                ["test"] = 1,
                ["test2"] = 2,
            };

            Dictionary<string, int> copy = initial.ToDictionary();
            Assert.AreEqual(2, copy.Count);
            Assert.AreEqual(1, copy["test"]);
            Assert.AreEqual(2, copy["test2"]);
        }

        [Test]
        public void ToDictionaryFromEnumerableValueTuple()
        {
            IEnumerable<(string, int)> initial = new Dictionary<string, int>()
            {
                ["test"] = 1,
                ["test2"] = 2,
            }.Select(kvp => (kvp.Key, kvp.Value));

            Dictionary<string, int> copy = initial.ToDictionary();
            Assert.AreEqual(2, copy.Count);
            Assert.AreEqual(1, copy["test"]);
            Assert.AreEqual(2, copy["test2"]);
        }

        [Test]
        public void ContentEqualsSame()
        {
            IReadOnlyDictionary<string, int> left = new Dictionary<string, int>()
            {
                ["one"] = 1,
                ["two"] = 2,
            };

            IReadOnlyDictionary<string, int> right = new Dictionary<string, int>()
            {
                ["one"] = 1,
                ["two"] = 2,
            };

            Assert.IsTrue(left.ContentEquals(right));
            Assert.IsTrue(left.ContentEquals(left));
            Assert.IsTrue(right.ContentEquals(left));
            Assert.IsTrue(right.ContentEquals(right));
        }

        [Test]
        public void ContentEqualsDifferentCount()
        {
            IReadOnlyDictionary<string, int> left = new Dictionary<string, int>()
            {
                ["one"] = 1,
                ["two"] = 2,
            };

            IReadOnlyDictionary<string, int> right = new Dictionary<string, int>()
            {
                ["one"] = 1,
                ["two"] = 2,
                ["three"] = 3,
            };

            Assert.IsFalse(left.ContentEquals(right));
            Assert.IsTrue(left.ContentEquals(left));
            Assert.IsFalse(right.ContentEquals(left));
            Assert.IsTrue(right.ContentEquals(right));
        }

        [Test]
        public void ContentEqualsDifferentValuesSameSize()
        {
            IReadOnlyDictionary<string, int> left = new Dictionary<string, int>()
            {
                ["one"] = 1,
                ["two"] = 2,
            };

            IReadOnlyDictionary<string, int> right = new Dictionary<string, int>()
            {
                ["one"] = 1,
                ["two"] = 20_000,
            };

            Assert.IsFalse(left.ContentEquals(right));
            Assert.IsTrue(left.ContentEquals(left));
            Assert.IsFalse(right.ContentEquals(left));
            Assert.IsTrue(right.ContentEquals(right));
        }

        [Test]
        public void ContentEqualsDifferentKeysSameSize()
        {
            IReadOnlyDictionary<string, int> left = new Dictionary<string, int>()
            {
                ["one"] = 1,
                ["two"] = 2,
            };

            IReadOnlyDictionary<string, int> right = new Dictionary<string, int>()
            {
                ["one"] = 1,
                ["three"] = 2,
            };

            Assert.IsFalse(left.ContentEquals(right));
            Assert.IsTrue(left.ContentEquals(left));
            Assert.IsFalse(right.ContentEquals(left));
            Assert.IsTrue(right.ContentEquals(right));
        }

        [Test]
        public void Deconstruct()
        {
            new KeyValuePair<string, int>("one", 1).Deconstruct(out string key, out int value);
            Assert.AreEqual("one", key);
            Assert.AreEqual(1, value);
        }
    }
}
