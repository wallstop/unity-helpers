// MIT License - Copyright (c) 2026 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

// TryXxx pattern - defensive programming for failable operations
// Never throw exceptions from public APIs

namespace WallstopStudios.UnityHelpers.Examples
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public sealed class DefensiveTryPatternExamples
    {
        private readonly Dictionary<string, object> _dictionary = new Dictionary<string, object>();
        private readonly List<Item> _items = new List<Item>();

        // Basic TryGetValue pattern
        public bool TryGetValue(string key, out object value)
        {
            value = default;

            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            if (!_dictionary.TryGetValue(key, out value))
            {
                return false;
            }

            return true;
        }

        // Complex TryParse with error info
        public bool TryParse(string json, out MyData result, out string error)
        {
            result = default;
            error = null;

            if (string.IsNullOrEmpty(json))
            {
                error = "JSON string is null or empty";
                return false;
            }

            try
            {
                result = JsonUtility.FromJson<MyData>(json);
                return result != null;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        // Safe indexing - returns default for invalid index
        public Item Get(int index)
        {
            if (index < 0 || index >= _items.Count)
            {
                return default;
            }
            return _items[index];
        }

        // TryGet pattern for callers who need to know success/failure
        public bool TryGet(int index, out Item value)
        {
            if (index < 0 || index >= _items.Count)
            {
                value = default;
                return false;
            }
            value = _items[index];
            return true;
        }

        // GetOrDefault pattern
        public object GetOrDefault(string key, object defaultValue = default)
        {
            if (key == null)
            {
                return defaultValue;
            }

            if (_dictionary.TryGetValue(key, out object value))
            {
                return value;
            }

            return defaultValue;
        }

        // Safe removal
        public bool TryRemove(string key)
        {
            if (key == null)
            {
                return false;
            }

            return _dictionary.Remove(key);
        }

        // Dummy classes for example
        [Serializable]
        public sealed class MyData { }

        public sealed class Item { }
    }
}
