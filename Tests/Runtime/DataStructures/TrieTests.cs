namespace WallstopStudios.UnityHelpers.Tests.DataStructures
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.DataStructure;

    public sealed class TrieTests
    {
        [Test]
        public void ConstructorWithEmptyListCreatesValidTrie()
        {
            Trie trie = new(Array.Empty<string>());
            Assert.IsFalse(trie.Contains("test"));
        }

        [Test]
        public void ConstructorWithSingleWordCreatesValidTrie()
        {
            Trie trie = new(new[] { "test" });
            Assert.IsTrue(trie.Contains("test"));
        }

        [Test]
        public void ConstructorWithMultipleWordsCreatesValidTrie()
        {
            string[] words = { "apple", "banana", "cherry" };
            Trie trie = new(words);

            foreach (string word in words)
            {
                Assert.IsTrue(trie.Contains(word));
            }
        }

        [Test]
        public void ContainsReturnsFalseForNonExistentWord()
        {
            Trie trie = new(new[] { "test", "testing" });
            Assert.IsFalse(trie.Contains("nonexistent"));
        }

        [Test]
        public void ContainsReturnsFalseForPrefix()
        {
            Trie trie = new(new[] { "testing" });
            Assert.IsFalse(trie.Contains("test"));
        }

        [Test]
        public void ContainsReturnsTrueForExactWord()
        {
            Trie trie = new(new[] { "test", "testing" });
            Assert.IsTrue(trie.Contains("test"));
            Assert.IsTrue(trie.Contains("testing"));
        }

        [Test]
        public void ContainsReturnsFalseForEmptyString()
        {
            Trie trie = new(new[] { "test" });
            Assert.IsFalse(trie.Contains(""));
        }

        [Test]
        public void ContainsHandlesEmptyStringInWordList()
        {
            Trie trie = new(new[] { "", "test" });
            Assert.IsTrue(trie.Contains(""));
            Assert.IsTrue(trie.Contains("test"));
        }

        [Test]
        public void ContainsCaseSensitive()
        {
            Trie trie = new(new[] { "test" });
            Assert.IsTrue(trie.Contains("test"));
            Assert.IsFalse(trie.Contains("TEST"));
            Assert.IsFalse(trie.Contains("Test"));
        }

        [Test]
        public void GetWordsWithPrefixReturnsEmptyForNonMatchingPrefix()
        {
            Trie trie = new(new[] { "apple", "banana", "cherry" });
            List<string> results = new();
            int count = trie.GetWordsWithPrefix("xyz", results);

            Assert.AreEqual(0, count);
            Assert.AreEqual(0, results.Count);
        }

        [Test]
        public void GetWordsWithPrefixReturnsMatchingWords()
        {
            Trie trie = new(new[] { "apple", "application", "apply", "banana" });
            List<string> results = new();
            int count = trie.GetWordsWithPrefix("app", results);

            Assert.AreEqual(3, count);
            Assert.AreEqual(3, results.Count);
            CollectionAssert.AreEquivalent(new[] { "apple", "application", "apply" }, results);
        }

        [Test]
        public void GetWordsWithPrefixReturnsAllWordsForEmptyPrefix()
        {
            string[] words = { "apple", "banana", "cherry" };
            Trie trie = new(words);
            List<string> results = new();
            int count = trie.GetWordsWithPrefix("", results);

            Assert.AreEqual(3, count);
            CollectionAssert.AreEquivalent(words, results);
        }

        [Test]
        public void GetWordsWithPrefixRespectsMaxResults()
        {
            Trie trie = new(new[] { "apple", "application", "apply", "approach" });
            List<string> results = new();
            int count = trie.GetWordsWithPrefix("app", results, maxResults: 2);

            Assert.AreEqual(2, count);
            Assert.AreEqual(2, results.Count);
        }

        [Test]
        public void GetWordsWithPrefixClearsResultsList()
        {
            Trie trie = new(new[] { "apple", "banana" });
            List<string> results = new() { "old1", "old2" };
            trie.GetWordsWithPrefix("app", results);

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("apple", results[0]);
        }

        [Test]
        public void GetWordsWithPrefixHandlesExactMatch()
        {
            Trie trie = new(new[] { "test", "testing", "tester" });
            List<string> results = new();
            trie.GetWordsWithPrefix("test", results);

            Assert.AreEqual(3, results.Count);
            CollectionAssert.Contains(results, "test");
        }

        [Test]
        public void GetWordsWithPrefixHandlesLongPrefix()
        {
            Trie trie = new(new[] { "testing" });
            List<string> results = new();
            int count = trie.GetWordsWithPrefix("testing", results);

            Assert.AreEqual(1, count);
            Assert.AreEqual("testing", results[0]);
        }

        [Test]
        public void GetWordsWithPrefixHandlesPrefixLongerThanAnyWord()
        {
            Trie trie = new(new[] { "test" });
            List<string> results = new();
            int count = trie.GetWordsWithPrefix("testingextra", results);

            Assert.AreEqual(0, count);
            Assert.AreEqual(0, results.Count);
        }

        [Test]
        public void TrieHandlesWordsWithCommonPrefixes()
        {
            string[] words = { "car", "card", "care", "careful", "carefully" };
            Trie trie = new(words);

            foreach (string word in words)
            {
                Assert.IsTrue(trie.Contains(word), $"Word '{word}' should be in trie");
            }

            List<string> results = new();
            trie.GetWordsWithPrefix("car", results);
            Assert.AreEqual(5, results.Count);
            CollectionAssert.AreEquivalent(words, results);
        }

        [Test]
        public void TrieHandlesSingleCharacterWords()
        {
            Trie trie = new(new[] { "a", "b", "c" });
            Assert.IsTrue(trie.Contains("a"));
            Assert.IsTrue(trie.Contains("b"));
            Assert.IsTrue(trie.Contains("c"));
            Assert.IsFalse(trie.Contains("d"));
        }

        [Test]
        public void TrieHandlesUnicodeCharacters()
        {
            string[] words = { "世界", "世", "世界和平" };
            Trie trie = new(words);

            Assert.IsTrue(trie.Contains("世界"));
            Assert.IsTrue(trie.Contains("世"));
            Assert.IsTrue(trie.Contains("世界和平"));
        }

        [Test]
        public void TrieHandlesSpecialCharacters()
        {
            string[] words = { "hello-world", "hello_world", "hello.world" };
            Trie trie = new(words);

            foreach (string word in words)
            {
                Assert.IsTrue(trie.Contains(word));
            }
        }

        [Test]
        public void TrieHandlesDuplicateWords()
        {
            Trie trie = new(new[] { "test", "test", "test" });
            Assert.IsTrue(trie.Contains("test"));

            List<string> results = new();
            trie.GetWordsWithPrefix("test", results);
            Assert.AreEqual(1, results.Count);
        }

        [Test]
        public void TrieHandlesVeryLongWords()
        {
            string longWord = new string('a', 1000);
            Trie trie = new(new[] { longWord });
            Assert.IsTrue(trie.Contains(longWord));
        }

        [Test]
        public void TrieHandlesManyWords()
        {
            string[] words = Enumerable.Range(0, 1000).Select(i => $"word{i}").ToArray();
            Trie trie = new(words);

            foreach (string word in words)
            {
                Assert.IsTrue(trie.Contains(word));
            }
        }
    }

    public sealed class GenericTrieTests
    {
        [Test]
        public void ConstructorWithEmptyDictionaryCreatesValidTrie()
        {
            Trie<int> trie = new(new Dictionary<string, int>());
            Assert.IsFalse(trie.TryGetValue("test", out _));
        }

        [Test]
        public void ConstructorWithNullDictionaryThrows()
        {
            Assert.Throws<ArgumentNullException>(() => new Trie<int>(null));
        }

        [Test]
        public void ConstructorWithSingleEntryCreatesValidTrie()
        {
            Dictionary<string, int> dict = new() { { "test", 42 } };
            Trie<int> trie = new(dict);

            Assert.IsTrue(trie.TryGetValue("test", out int value));
            Assert.AreEqual(42, value);
        }

        [Test]
        public void ConstructorWithMultipleEntriesCreatesValidTrie()
        {
            Dictionary<string, int> dict = new()
            {
                { "apple", 1 },
                { "banana", 2 },
                { "cherry", 3 },
            };
            Trie<int> trie = new(dict);

            Assert.IsTrue(trie.TryGetValue("apple", out int value1));
            Assert.AreEqual(1, value1);
            Assert.IsTrue(trie.TryGetValue("banana", out int value2));
            Assert.AreEqual(2, value2);
            Assert.IsTrue(trie.TryGetValue("cherry", out int value3));
            Assert.AreEqual(3, value3);
        }

        [Test]
        public void TryGetValueReturnsFalseForNonExistentKey()
        {
            Dictionary<string, string> dict = new() { { "test", "value" } };
            Trie<string> trie = new(dict);

            Assert.IsFalse(trie.TryGetValue("nonexistent", out string value));
            Assert.IsNull(value);
        }

        [Test]
        public void TryGetValueReturnsFalseForPrefix()
        {
            Dictionary<string, int> dict = new() { { "testing", 100 } };
            Trie<int> trie = new(dict);

            Assert.IsFalse(trie.TryGetValue("test", out _));
        }

        [Test]
        public void TryGetValueReturnsTrueForExactKey()
        {
            Dictionary<string, string> dict = new()
            {
                { "test", "value1" },
                { "testing", "value2" },
            };
            Trie<string> trie = new(dict);

            Assert.IsTrue(trie.TryGetValue("test", out string value1));
            Assert.AreEqual("value1", value1);
            Assert.IsTrue(trie.TryGetValue("testing", out string value2));
            Assert.AreEqual("value2", value2);
        }

        [Test]
        public void TryGetValueReturnsFalseForEmptyString()
        {
            Dictionary<string, int> dict = new() { { "test", 42 } };
            Trie<int> trie = new(dict);

            Assert.IsFalse(trie.TryGetValue("", out _));
        }

        [Test]
        public void TryGetValueHandlesEmptyStringInDictionary()
        {
            Dictionary<string, int> dict = new() { { "", 0 }, { "test", 42 } };
            Trie<int> trie = new(dict);

            Assert.IsTrue(trie.TryGetValue("", out int value1));
            Assert.AreEqual(0, value1);
            Assert.IsTrue(trie.TryGetValue("test", out int value2));
            Assert.AreEqual(42, value2);
        }

        [Test]
        public void TryGetValueCaseSensitive()
        {
            Dictionary<string, int> dict = new() { { "test", 42 } };
            Trie<int> trie = new(dict);

            Assert.IsTrue(trie.TryGetValue("test", out int value));
            Assert.AreEqual(42, value);
            Assert.IsFalse(trie.TryGetValue("TEST", out _));
            Assert.IsFalse(trie.TryGetValue("Test", out _));
        }

        [Test]
        public void GetValuesWithPrefixReturnsEmptyForNonMatchingPrefix()
        {
            Dictionary<string, int> dict = new()
            {
                { "apple", 1 },
                { "banana", 2 },
                { "cherry", 3 },
            };
            Trie<int> trie = new(dict);
            List<int> results = new();

            int count = trie.GetValuesWithPrefix("xyz", results);

            Assert.AreEqual(0, count);
            Assert.AreEqual(0, results.Count);
        }

        [Test]
        public void GetValuesWithPrefixReturnsMatchingValues()
        {
            Dictionary<string, int> dict = new()
            {
                { "apple", 1 },
                { "application", 2 },
                { "apply", 3 },
                { "banana", 4 },
            };
            Trie<int> trie = new(dict);
            List<int> results = new();

            int count = trie.GetValuesWithPrefix("app", results);

            Assert.AreEqual(3, count);
            Assert.AreEqual(3, results.Count);
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, results);
        }

        [Test]
        public void GetValuesWithPrefixReturnsAllValuesForEmptyPrefix()
        {
            Dictionary<string, int> dict = new()
            {
                { "apple", 1 },
                { "banana", 2 },
                { "cherry", 3 },
            };
            Trie<int> trie = new(dict);
            List<int> results = new();

            int count = trie.GetValuesWithPrefix("", results);

            Assert.AreEqual(3, count);
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, results);
        }

        [Test]
        public void GetValuesWithPrefixRespectsMaxResults()
        {
            Dictionary<string, int> dict = new()
            {
                { "apple", 1 },
                { "application", 2 },
                { "apply", 3 },
                { "approach", 4 },
            };
            Trie<int> trie = new(dict);
            List<int> results = new();

            int count = trie.GetValuesWithPrefix("app", results, maxResults: 2);

            Assert.AreEqual(2, count);
            Assert.AreEqual(2, results.Count);
        }

        [Test]
        public void GetValuesWithPrefixClearsResultsList()
        {
            Dictionary<string, int> dict = new() { { "apple", 1 }, { "banana", 2 } };
            Trie<int> trie = new(dict);
            List<int> results = new() { 100, 200 };

            trie.GetValuesWithPrefix("app", results);

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(1, results[0]);
        }

        [Test]
        public void GetValuesWithPrefixHandlesExactMatch()
        {
            Dictionary<string, int> dict = new()
            {
                { "test", 1 },
                { "testing", 2 },
                { "tester", 3 },
            };
            Trie<int> trie = new(dict);
            List<int> results = new();

            trie.GetValuesWithPrefix("test", results);

            Assert.AreEqual(3, results.Count);
            CollectionAssert.Contains(results, 1);
        }

        [Test]
        public void GenericTrieHandlesComplexTypes()
        {
            Dictionary<string, List<int>> dict = new()
            {
                {
                    "key1",
                    new List<int> { 1, 2, 3 }
                },
                {
                    "key2",
                    new List<int> { 4, 5, 6 }
                },
            };
            Trie<List<int>> trie = new(dict);

            Assert.IsTrue(trie.TryGetValue("key1", out List<int> value1));
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, value1);
            Assert.IsTrue(trie.TryGetValue("key2", out List<int> value2));
            CollectionAssert.AreEqual(new[] { 4, 5, 6 }, value2);
        }

        [Test]
        public void GenericTrieHandlesNullableValues()
        {
            Dictionary<string, int?> dict = new() { { "null", null }, { "value", 42 } };
            Trie<int?> trie = new(dict);

            Assert.IsTrue(trie.TryGetValue("null", out int? value1));
            Assert.IsNull(value1);
            Assert.IsTrue(trie.TryGetValue("value", out int? value2));
            Assert.AreEqual(42, value2);
        }

        [Test]
        public void GenericTrieHandlesReferenceTypes()
        {
            Dictionary<string, string> dict = new()
            {
                { "key1", "value1" },
                { "key2", "value2" },
                { "key3", null },
            };
            Trie<string> trie = new(dict);

            Assert.IsTrue(trie.TryGetValue("key1", out string value1));
            Assert.AreEqual("value1", value1);
            Assert.IsTrue(trie.TryGetValue("key3", out string value3));
            Assert.IsNull(value3);
        }

        [Test]
        public void GenericTrieHandlesWordsWithCommonPrefixes()
        {
            Dictionary<string, int> dict = new()
            {
                { "car", 1 },
                { "card", 2 },
                { "care", 3 },
                { "careful", 4 },
                { "carefully", 5 },
            };
            Trie<int> trie = new(dict);

            foreach (var kvp in dict)
            {
                Assert.IsTrue(trie.TryGetValue(kvp.Key, out int value));
                Assert.AreEqual(kvp.Value, value);
            }

            List<int> results = new();
            trie.GetValuesWithPrefix("car", results);
            Assert.AreEqual(5, results.Count);
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5 }, results);
        }

        [Test]
        public void GenericTrieHandlesSingleCharacterKeys()
        {
            Dictionary<string, int> dict = new()
            {
                { "a", 1 },
                { "b", 2 },
                { "c", 3 },
            };
            Trie<int> trie = new(dict);

            Assert.IsTrue(trie.TryGetValue("a", out int value1));
            Assert.AreEqual(1, value1);
            Assert.IsTrue(trie.TryGetValue("b", out int value2));
            Assert.AreEqual(2, value2);
            Assert.IsTrue(trie.TryGetValue("c", out int value3));
            Assert.AreEqual(3, value3);
            Assert.IsFalse(trie.TryGetValue("d", out _));
        }

        [Test]
        public void GenericTrieHandlesUnicodeCharacters()
        {
            Dictionary<string, string> dict = new()
            {
                { "世界", "world" },
                { "世", "generation" },
                { "世界和平", "world peace" },
            };
            Trie<string> trie = new(dict);

            Assert.IsTrue(trie.TryGetValue("世界", out string value1));
            Assert.AreEqual("world", value1);
            Assert.IsTrue(trie.TryGetValue("世", out string value2));
            Assert.AreEqual("generation", value2);
        }

        [Test]
        public void GenericTrieHandlesVeryLongKeys()
        {
            string longKey = new string('a', 1000);
            Dictionary<string, int> dict = new() { { longKey, 42 } };
            Trie<int> trie = new(dict);

            Assert.IsTrue(trie.TryGetValue(longKey, out int value));
            Assert.AreEqual(42, value);
        }

        [Test]
        public void GenericTrieHandlesManyEntries()
        {
            Dictionary<string, int> dict = Enumerable
                .Range(0, 1000)
                .ToDictionary(i => $"key{i}", i => i);
            Trie<int> trie = new(dict);

            foreach (var kvp in dict)
            {
                Assert.IsTrue(trie.TryGetValue(kvp.Key, out int value));
                Assert.AreEqual(kvp.Value, value);
            }
        }
    }
}
