namespace WallstopStudios.UnityHelpers.Core.DataStructure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using UnityEngine;

    /// <summary>
    /// A highly optimized, array-backed Trie implementation for fast prefix search and exact word lookup.
    /// Preallocates storage based on total characters in the input set and uses integer indices for traversal,
    /// minimizing memory allocations and indirections. Provides allocation-free prefix search method (aside from
    /// returned string allocations).
    /// </summary>
    public sealed class Trie
    {
        private const int Poison = -1;

        private readonly char[] _chars;
        private readonly int[] _firstChild;
        private readonly int[] _nextSibling;
        private readonly bool[] _isWord;
        private readonly StringBuilder _stringBuilder;
        private int _nodeCount;

        /// <summary>
        /// Constructs the Trie from the provided collection of words.
        /// </summary>
        /// <param name="words">All possible words to insert into the Trie.</param>
        public Trie(IEnumerable<string> words)
        {
            IReadOnlyList<string> wordList = words as IReadOnlyList<string> ?? words.ToList();
            int maxWordLength;
            if (wordList.Count > 0)
            {
                maxWordLength = wordList[0].Length;
                for (int i = 1; i < wordList.Count; ++i)
                {
                    maxWordLength = Mathf.Max(maxWordLength, wordList[i].Length);
                }
            }
            else
            {
                maxWordLength = 0;
            }

            int capacity = 1;
            for (int i = 0; i < wordList.Count; ++i)
            {
                capacity += wordList[i].Length;
            }

            _chars = new char[capacity];
            _firstChild = new int[capacity];
            _nextSibling = new int[capacity];
            _isWord = new bool[capacity];

            Array.Fill(_firstChild, Poison);
            Array.Fill(_nextSibling, Poison);

            _stringBuilder = new StringBuilder(maxWordLength);

            _nodeCount = 1; // root node index
            for (int i = 0; i < wordList.Count; ++i)
            {
                string word = wordList[i];
                Insert(word);
            }
        }

        // Inserts a single word into the Trie
        private void Insert(string word)
        {
            int node = 0;
            foreach (char c in word)
            {
                int prev = Poison;
                int child = _firstChild[node];
                while (child != Poison && _chars[child] != c)
                {
                    prev = child;
                    child = _nextSibling[child];
                }
                if (child == Poison)
                {
                    child = _nodeCount++;
                    _chars[child] = c;
                    _firstChild[child] = Poison;
                    _nextSibling[child] = Poison;
                    if (prev == Poison)
                    {
                        _firstChild[node] = child;
                    }
                    else
                    {
                        _nextSibling[prev] = child;
                    }
                }
                node = child;
            }
            _isWord[node] = true;
        }

        /// <summary>
        /// Determines whether the exact word exists in the Trie.
        /// </summary>
        public bool Contains(string word)
        {
            int node = 0;
            foreach (char c in word)
            {
                int child = _firstChild[node];
                while (child != Poison && _chars[child] != c)
                {
                    child = _nextSibling[child];
                }

                if (child == Poison)
                {
                    return false;
                }

                node = child;
            }
            return _isWord[node];
        }

        /// <summary>
        /// Collects up to maxResults words that start with the given prefix.
        /// Results are added into the provided list (which is cleared at the start).
        /// Returns the number of results added.
        /// </summary>
        public int GetWordsWithPrefix(
            string prefix,
            List<string> results,
            int maxResults = int.MaxValue
        )
        {
            results.Clear();
            int node = 0;
            foreach (char c in prefix)
            {
                int child = _firstChild[node];
                while (child != Poison && _chars[child] != c)
                {
                    child = _nextSibling[child];
                }

                if (child == Poison)
                {
                    return 0;
                }

                node = child;
            }
            _stringBuilder.Clear();
            _stringBuilder.Append(prefix);
            Collect(node, results, maxResults);
            return results.Count;
        }

        // Recursive collection without allocations
        private void Collect(int node, List<string> results, int maxResults)
        {
            if (results.Count >= maxResults)
            {
                return;
            }

            if (_isWord[node])
            {
                results.Add(_stringBuilder.ToString());
                if (results.Count >= maxResults)
                {
                    return;
                }
            }
            for (int child = _firstChild[node]; child != Poison; child = _nextSibling[child])
            {
                _stringBuilder.Append(_chars[child]);
                Collect(child, results, maxResults);
                _stringBuilder.Length--;
                if (results.Count >= maxResults)
                {
                    return;
                }
            }
        }
    }

    /// <summary>
    /// A highly optimized, array-backed generic Trie for mapping string keys to values of type T.
    /// Preallocates storage based on total characters in the key set and uses integer indices for traversal,
    /// minimizing memory allocations and indirections. Provides allocation-free prefix search (aside from
    /// the output list allocations themselves).
    /// </summary>
    public sealed class Trie<T>
    {
        private const int Poison = -1;

        private readonly char[] _chars;
        private readonly int[] _firstChild;
        private readonly int[] _nextSibling;
        private readonly bool[] _hasValue;
        private readonly T[] _values;
        private int _nodeCount;

        /// <summary>
        /// Constructs the Trie from the provided dictionary of keys to values.
        /// </summary>
        /// <param name="items">Mapping from unique string keys to values of type T.</param>
        public Trie(IReadOnlyDictionary<string, T> items)
        {
            KeyValuePair<string, T>[] array = items.ToArray();
            int capacity = 1;
            foreach (KeyValuePair<string, T> entry in array)
            {
                capacity += entry.Key.Length;
            }

            _chars = new char[capacity];
            _firstChild = new int[capacity];
            _nextSibling = new int[capacity];
            _hasValue = new bool[capacity];
            _values = new T[capacity];

            Array.Fill(_firstChild, Poison);
            Array.Fill(_nextSibling, Poison);

            _nodeCount = 1;
            foreach (KeyValuePair<string, T> kv in array)
            {
                Insert(kv.Key, kv.Value);
            }
        }

        // Inserts a single key-value pair into the Trie
        private void Insert(string key, T value)
        {
            int node = 0;
            foreach (char c in key)
            {
                int prev = Poison;
                int child = _firstChild[node];
                while (child != Poison && _chars[child] != c)
                {
                    prev = child;
                    child = _nextSibling[child];
                }
                if (child == Poison)
                {
                    child = _nodeCount++;
                    _chars[child] = c;
                    _firstChild[child] = Poison;
                    _nextSibling[child] = Poison;
                    if (prev == Poison)
                    {
                        _firstChild[node] = child;
                    }
                    else
                    {
                        _nextSibling[prev] = child;
                    }
                }
                node = child;
            }
            _hasValue[node] = true;
            _values[node] = value;
        }

        /// <summary>
        /// Attempts to retrieve the value associated with the exact key.
        /// </summary>
        public bool TryGetValue(string key, out T value)
        {
            int node = 0;
            foreach (char c in key)
            {
                int child = _firstChild[node];
                while (child != Poison && _chars[child] != c)
                {
                    child = _nextSibling[child];
                }

                if (child == Poison)
                {
                    value = default;
                    return false;
                }
                node = child;
            }
            if (_hasValue[node])
            {
                value = _values[node];
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>
        /// Collects up to maxResults values whose keys start with the given prefix.
        /// Results are added into the provided list (which is cleared at the start).
        /// Returns the number of results added.
        /// </summary>
        public int GetValuesWithPrefix(
            string prefix,
            List<T> results,
            int maxResults = int.MaxValue
        )
        {
            results.Clear();
            int node = 0;
            foreach (char c in prefix)
            {
                int child = _firstChild[node];
                while (child != Poison && _chars[child] != c)
                {
                    child = _nextSibling[child];
                }

                if (child == Poison)
                {
                    return 0;
                }

                node = child;
            }
            Collect(node, results, maxResults);
            return results.Count;
        }

        // Recursive collection without extra allocations
        private void Collect(int node, List<T> results, int maxResults)
        {
            if (results.Count >= maxResults)
            {
                return;
            }

            if (_hasValue[node])
            {
                results.Add(_values[node]);
                if (results.Count >= maxResults)
                {
                    return;
                }
            }
            for (int child = _firstChild[node]; child != Poison; child = _nextSibling[child])
            {
                Collect(child, results, maxResults);
                if (results.Count >= maxResults)
                {
                    return;
                }
            }
        }
    }
}
