namespace WallstopStudios.UnityHelpers.Core.DataStructure
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Utils;

    /// <summary>
    /// A highly optimized, array-backed Trie implementation for fast prefix search and exact word lookup.
    /// Preallocates storage based on total characters in the input set and uses integer indices for traversal,
    /// minimizing memory allocations and indirections. Provides allocation-free prefix search method (aside from
    /// returned string allocations).
    /// </summary>
    public sealed class Trie : IEnumerable<string>
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
            if (words == null)
            {
                throw new ArgumentNullException(nameof(words));
            }

            IReadOnlyList<string> wordList = words as IReadOnlyList<string> ?? words.ToArray();
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
        public List<string> GetWordsWithPrefix(
            string prefix,
            List<string> results,
            int maxResults = int.MaxValue
        )
        {
            results.Clear();
            if (maxResults <= 0)
            {
                return results;
            }

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
                    return results;
                }

                node = child;
            }
            _stringBuilder.Clear();
            _stringBuilder.Append(prefix);
            Collect(node, results, maxResults);
            return results;
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

        /// <summary>
        /// Returns a value-based enumerator for efficient iteration without heap allocations.
        /// </summary>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            return new EnumeratorObject(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new EnumeratorObject(this);
        }

        /// <summary>
        /// Value-based enumerator for efficient foreach iteration without heap allocations.
        /// Implements IDisposable to return pooled StringBuilders.
        /// </summary>
        public struct Enumerator : IDisposable
        {
            private readonly Trie _trie;
            private readonly Stack<(
                int node,
                PooledResource<StringBuilder> sbResource,
                int sbLength
            )> _stack;
            private readonly List<PooledResource<StringBuilder>> _pooledResources;
            private string _current;

            internal Enumerator(Trie trie)
            {
                _trie = trie;
                _stack = new Stack<(int, PooledResource<StringBuilder>, int)>();
                _pooledResources = new List<PooledResource<StringBuilder>>();
                _current = null;

                // Initialize with root node
                PooledResource<StringBuilder> sbResource = Buffers.StringBuilder.Get();
                _pooledResources.Add(sbResource);
                _stack.Push((0, sbResource, 0));
            }

            public string Current => _current;

            public bool MoveNext()
            {
                while (
                    _stack.TryPop(
                        out (int node, PooledResource<StringBuilder> sbResource, int sbLength) item
                    )
                )
                {
                    (int node, PooledResource<StringBuilder> sbResource, int sbLength) = item;
                    StringBuilder sb = sbResource.resource;
                    sb.Length = sbLength;

                    // Check if this node represents a word
                    if (_trie._isWord[node])
                    {
                        _current = sb.ToString();

                        // Push siblings and children for next iteration
                        PushChildrenAndContinue(node, sbResource, sbLength);
                        return true;
                    }

                    // Push all children onto the stack
                    for (
                        int child = _trie._firstChild[node];
                        child != Poison;
                        child = _trie._nextSibling[child]
                    )
                    {
                        PooledResource<StringBuilder> childResource = Buffers.StringBuilder.Get();
                        _pooledResources.Add(childResource);
                        StringBuilder childSb = childResource.resource;
                        childSb.Append(sb);
                        childSb.Append(_trie._chars[child]);
                        _stack.Push((child, childResource, childSb.Length));
                    }
                }

                return false;
            }

            private void PushChildrenAndContinue(
                int node,
                PooledResource<StringBuilder> sbResource,
                int sbLength
            )
            {
                StringBuilder sb = sbResource.resource;
                // Push all children onto the stack for future iterations
                for (
                    int child = _trie._firstChild[node];
                    child != Poison;
                    child = _trie._nextSibling[child]
                )
                {
                    PooledResource<StringBuilder> childResource = Buffers.StringBuilder.Get();
                    _pooledResources.Add(childResource);
                    StringBuilder childSb = childResource.resource;
                    childSb.Append(sb);
                    childSb.Append(_trie._chars[child]);
                    _stack.Push((child, childResource, childSb.Length));
                }
            }

            public void Dispose()
            {
                if (_pooledResources == null)
                {
                    return;
                }

                // Return all pooled StringBuilders to the pool
                foreach (PooledResource<StringBuilder> resource in _pooledResources)
                {
                    resource.Dispose();
                }
                _pooledResources.Clear();
            }
        }

        /// <summary>
        /// Reference-based enumerator for IEnumerable interface compatibility.
        /// </summary>
        private sealed class EnumeratorObject : IEnumerator<string>
        {
            private Enumerator _enumerator;

            internal EnumeratorObject(Trie trie)
            {
                _enumerator = new Enumerator(trie);
            }

            public string Current => _enumerator.Current;

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }

            public void Dispose()
            {
                _enumerator.Dispose();
            }
        }
    }

    /// <summary>
    /// A highly optimized, array-backed generic Trie for mapping string keys to values of type T.
    /// Preallocates storage based on total characters in the key set and uses integer indices for traversal,
    /// minimizing memory allocations and indirections. Provides allocation-free prefix search (aside from
    /// the output list allocations themselves).
    /// </summary>
    public sealed class Trie<T> : IEnumerable<T>
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
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

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
        public List<T> GetValuesWithPrefix(
            string prefix,
            List<T> results,
            int maxResults = int.MaxValue
        )
        {
            results.Clear();
            if (maxResults <= 0)
            {
                return results;
            }

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
                    return results;
                }

                node = child;
            }
            Collect(node, results, maxResults);
            return results;
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

        /// <summary>
        /// Returns a value-based enumerator for efficient iteration without heap allocations.
        /// </summary>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new EnumeratorObject(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new EnumeratorObject(this);
        }

        /// <summary>
        /// Value-based enumerator for efficient foreach iteration without heap allocations.
        /// </summary>
        public struct Enumerator
        {
            private readonly Trie<T> _trie;
            private readonly Stack<int> _stack;
            private T _current;

            internal Enumerator(Trie<T> trie)
            {
                _trie = trie;
                _stack = new Stack<int>();
                _current = default;

                // Initialize with root node
                if (_trie._nodeCount > 1)
                {
                    _stack.Push(0);
                }
            }

            public T Current => _current;

            public bool MoveNext()
            {
                while (_stack.TryPop(out int node))
                {
                    // Check if this node has a value
                    if (node != 0 && _trie._hasValue[node])
                    {
                        _current = _trie._values[node];

                        // Push children for next iteration
                        PushChildren(node);
                        return true;
                    }

                    // Push all children onto the stack
                    for (
                        int child = _trie._firstChild[node];
                        child != Poison;
                        child = _trie._nextSibling[child]
                    )
                    {
                        _stack.Push(child);
                    }
                }

                return false;
            }

            private void PushChildren(int node)
            {
                // Push all children onto the stack for future iterations
                for (
                    int child = _trie._firstChild[node];
                    child != Poison;
                    child = _trie._nextSibling[child]
                )
                {
                    _stack.Push(child);
                }
            }
        }

        /// <summary>
        /// Reference-based enumerator for IEnumerable interface compatibility.
        /// </summary>
        private sealed class EnumeratorObject : IEnumerator<T>
        {
            private Enumerator _enumerator;

            internal EnumeratorObject(Trie<T> trie)
            {
                _enumerator = new Enumerator(trie);
            }

            public T Current => _enumerator.Current;

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }

            public void Dispose() { }
        }
    }
}
