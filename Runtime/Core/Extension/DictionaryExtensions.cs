namespace UnityHelpers.Core.Extension
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class DictionaryExtensions
    {
        public static V GetOrAdd<K, V>(this IDictionary<K, V> dictionary, K key, Func<V> valueProducer)
        {
            if (dictionary.TryGetValue(key, out V result))
            {
                return result;
            }

            return dictionary[key] = valueProducer();
        }
        public static V GetOrAdd<K, V>(this IDictionary<K, V> dictionary, K key, Func<K, V> valueProducer)
        {
            if (dictionary.TryGetValue(key, out V result))
            {
                return result;
            }

            return dictionary[key] = valueProducer(key);
        }

        public static V GetOrElse<K, V>(this IDictionary<K, V> dictionary, K key, Func<V> valueProducer)
        {
            if (dictionary.TryGetValue(key, out V value))
            {
                return value;
            }
            return valueProducer.Invoke();
        }

        public static V GetOrElse<K, V>(this IDictionary<K, V> dictionary, K key, Func<K, V> valueProducer)
        {
            if (dictionary.TryGetValue(key, out V value))
            {
                return value;
            }

            return valueProducer.Invoke(key);
        }

        public static V GetOrAdd<K, V>(this IDictionary<K, V> dictionary, K key) where V : new()
        {
            if (dictionary.TryGetValue(key, out V result))
            {
                return result;
            }

            return dictionary[key] = new V();
        }

        public static V GetOrElse<K, V>(this IDictionary<K, V> dictionary, K key, V value)
        {
            return GetOrElse(dictionary, key, () => value);
        }

        public static bool Remove<K, V>(this IDictionary<K, V> dictionary, K key, out V value)
        {
            if (dictionary.TryGetValue(key, out value))
            {
                return dictionary.Remove(key);
            }
            return false;
        }

        public static V AddOrUpdate<K, V>(this IDictionary<K, V> dictionary, K key, Func<K, V> creator, Func<K, V, V> updater)
        {
            V latest = dictionary.TryGetValue(key, out V value) ? updater(key, value) : creator(key);
            dictionary[key] = latest;
            return latest;
        }

        public static Dictionary<K, V> Merge<K, V>(this IDictionary<K, V> lhs, IDictionary<K, V> rhs)
        {
            Dictionary<K, V> result = new Dictionary<K, V>(lhs.Count);
            foreach (KeyValuePair<K, V> kvp in lhs)
            {
                if (rhs.ContainsKey(kvp.Key))
                {
                    continue;
                }

                result[kvp.Key] = kvp.Value;
            }

            foreach (KeyValuePair<K, V> kvp in rhs)
            {
                result[kvp.Key] = kvp.Value;
            }

            return result;
        }

        /// <summary>
        ///  </summary>
        /// <typeparam name="K">Key type.</typeparam>
        /// <typeparam name="V">Value type.</typeparam>
        /// <param name="lhs">Basis dictionary.</param>
        /// <param name="rhs">Changed dictionary.</param>
        /// <returns>All elements of rhs that either don't exist in or are different from lhs</returns>
        public static Dictionary<K, V> Difference<K, V>(this IDictionary<K, V> lhs, IDictionary<K, V> rhs)
        {
            Dictionary<K, V> result = new Dictionary<K, V>(rhs.Count);
            foreach (KeyValuePair<K, V> kvp in rhs)
            {
                K key = kvp.Key;
                V existing;
                if (lhs.TryGetValue(key, out existing))
                {
                    if (Equals(existing, kvp.Value))
                    {
                        continue;
                    }

                    result[key] = kvp.Value;
                }
            }

            return result;
        }

        public static Dictionary<V, K> Reverse<K, V>(this IDictionary<K, V> dictionary)
        {
            Dictionary<V, K> output = new Dictionary<V, K>(dictionary.Count);
            foreach (KeyValuePair<K, V> entry in dictionary)
            {
                output[entry.Value] = entry.Key;
            }
            return output;
        }

        public static Dictionary<K, V> ToDictionary<K, V>(this IDictionary<K, V> dictionary)
        {
            return new Dictionary<K, V>(dictionary);
        }

        public static Dictionary<K, V> ToDictionary<K, V>(this IEnumerable<KeyValuePair<K, V>> prettyMuchADictionary)
        {
            return prettyMuchADictionary.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public static bool ContentEquals<K, V>(this IDictionary<K, V> dictionary, IDictionary<K, V> other)
        {
            if (ReferenceEquals(dictionary, other))
            {
                return true;
            }

            if (ReferenceEquals(dictionary, null) || ReferenceEquals(other, null))
            {
                return false;
            }

            if (dictionary.Count != other.Count)
            {
                return false;
            }

            return !dictionary.Except(other).Any();
        }

        public static void Deconstruct<K, V>(this KeyValuePair<K, V> kvp, out K key, out V value)
        {
            key = kvp.Key;
            value = kvp.Value;
        }
    }
}
