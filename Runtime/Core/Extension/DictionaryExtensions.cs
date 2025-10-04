namespace WallstopStudios.UnityHelpers.Core.Extension
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    public static class DictionaryExtensions
    {
        public static V GetOrAdd<K, V>(
            this IDictionary<K, V> dictionary,
            K key,
            Func<V> valueProducer
        )
        {
            if (dictionary is ConcurrentDictionary<K, V> concurrentDictionary)
            {
                return concurrentDictionary.GetOrAdd(
                    key,
                    static (_, existing) => existing(),
                    valueProducer
                );
            }

            if (dictionary.TryGetValue(key, out V result))
            {
                return result;
            }

            return dictionary[key] = valueProducer();
        }

        public static V GetOrAdd<K, V>(
            this IDictionary<K, V> dictionary,
            K key,
            Func<K, V> valueProducer
        )
        {
            if (dictionary is ConcurrentDictionary<K, V> concurrentDictionary)
            {
                return concurrentDictionary.GetOrAdd(key, valueProducer);
            }

            if (dictionary.TryGetValue(key, out V result))
            {
                return result;
            }

            return dictionary[key] = valueProducer(key);
        }

        public static V GetOrElse<K, V>(
            this IReadOnlyDictionary<K, V> dictionary,
            K key,
            Func<V> valueProducer
        )
        {
            if (dictionary.TryGetValue(key, out V value))
            {
                return value;
            }

            return valueProducer.Invoke();
        }

        public static V GetOrElse<K, V>(
            this IReadOnlyDictionary<K, V> dictionary,
            K key,
            Func<K, V> valueProducer
        )
        {
            if (dictionary.TryGetValue(key, out V value))
            {
                return value;
            }

            return valueProducer.Invoke(key);
        }

        public static V GetOrAdd<K, V>(this IDictionary<K, V> dictionary, K key)
            where V : new()
        {
            if (dictionary is ConcurrentDictionary<K, V> concurrentDictionary)
            {
                return concurrentDictionary.AddOrUpdate(
                    key,
                    _ => new V(),
                    (_, existing) => existing
                );
            }

            if (dictionary.TryGetValue(key, out V result))
            {
                return result;
            }

            return dictionary[key] = new V();
        }

        public static V GetOrElse<K, V>(this IReadOnlyDictionary<K, V> dictionary, K key, V value)
        {
            return dictionary.GetValueOrDefault(key, value);
        }

        public static V AddOrUpdate<K, V>(
            this IDictionary<K, V> dictionary,
            K key,
            Func<K, V> creator,
            Func<K, V, V> updater
        )
        {
            if (dictionary is ConcurrentDictionary<K, V> concurrentDictionary)
            {
                return concurrentDictionary.AddOrUpdate(key, creator, updater);
            }

            V latest = dictionary.TryGetValue(key, out V value)
                ? updater(key, value)
                : creator(key);
            dictionary[key] = latest;
            return latest;
        }

        public static V TryAdd<K, V>(this IDictionary<K, V> dictionary, K key, Func<K, V> creator)
        {
            if (dictionary is ConcurrentDictionary<K, V> concurrentDictionary)
            {
                return concurrentDictionary.AddOrUpdate(
                    key,
                    creator,
                    (_, existingValue) => existingValue
                );
            }

            if (dictionary.TryGetValue(key, out V existing))
            {
                return existing;
            }

            V value = creator(key);
            dictionary[key] = value;
            return value;
        }

        public static Dictionary<K, V> Merge<K, V>(
            this IReadOnlyDictionary<K, V> lhs,
            IReadOnlyDictionary<K, V> rhs,
            Func<Dictionary<K, V>> creator = null
        )
        {
            Dictionary<K, V> result = creator?.Invoke() ?? new Dictionary<K, V>();
            if (0 < lhs.Count)
            {
                foreach (KeyValuePair<K, V> kvp in lhs)
                {
                    result[kvp.Key] = kvp.Value;
                }
            }

            if (0 < rhs.Count)
            {
                foreach (KeyValuePair<K, V> kvp in rhs)
                {
                    result[kvp.Key] = kvp.Value;
                }
            }

            return result;
        }

        public static bool TryRemove<K, V>(this IDictionary<K, V> dictionary, K key, out V value)
        {
            if (dictionary is ConcurrentDictionary<K, V> concurrentDictionary)
            {
                return concurrentDictionary.TryRemove(key, out value);
            }

            return dictionary.Remove(key, out value);
        }

        /// <summary>
        ///  </summary>
        /// <typeparam name="K">Key type.</typeparam>
        /// <typeparam name="V">Value type.</typeparam>
        /// <param name="lhs">Basis dictionary.</param>
        /// <param name="rhs">Changed dictionary.</param>
        /// <returns>All elements of rhs that either don't exist in or are different from lhs</returns>
        public static Dictionary<K, V> Difference<K, V>(
            this IReadOnlyDictionary<K, V> lhs,
            IReadOnlyDictionary<K, V> rhs,
            Func<Dictionary<K, V>> creator = null
        )
        {
            Dictionary<K, V> result = creator?.Invoke() ?? new Dictionary<K, V>(rhs.Count);
            foreach (KeyValuePair<K, V> kvp in rhs)
            {
                K key = kvp.Key;
                if (lhs.TryGetValue(key, out V existing) && Equals(existing, kvp.Value))
                {
                    continue;
                }

                result[key] = kvp.Value;
            }

            return result;
        }

        public static Dictionary<V, K> Reverse<K, V>(
            this IReadOnlyDictionary<K, V> dictionary,
            Func<Dictionary<V, K>> creator = null
        )
        {
            Dictionary<V, K> output = creator?.Invoke() ?? new Dictionary<V, K>(dictionary.Count);
            foreach (KeyValuePair<K, V> entry in dictionary)
            {
                output[entry.Value] = entry.Key;
            }

            return output;
        }

        public static Dictionary<K, V> ToDictionary<K, V>(this IReadOnlyDictionary<K, V> dictionary)
        {
            return new Dictionary<K, V>(dictionary);
        }

        public static Dictionary<K, V> ToDictionary<K, V>(
            this IReadOnlyDictionary<K, V> dictionary,
            IEqualityComparer<K> comparer
        )
        {
            return new Dictionary<K, V>(dictionary, comparer);
        }

        public static Dictionary<K, V> ToDictionary<K, V>(
            this IEnumerable<KeyValuePair<K, V>> prettyMuchADictionary
        )
        {
            return prettyMuchADictionary.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public static Dictionary<K, V> ToDictionary<K, V>(
            this IEnumerable<KeyValuePair<K, V>> prettyMuchADictionary,
            IEqualityComparer<K> comparer
        )
        {
            return prettyMuchADictionary.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, comparer);
        }

        public static Dictionary<K, V> ToDictionary<K, V>(
            this IEnumerable<(K, V)> prettyMuchADictionary
        )
        {
            return prettyMuchADictionary.ToDictionary(kvp => kvp.Item1, kvp => kvp.Item2);
        }

        public static Dictionary<K, V> ToDictionary<K, V>(
            this IEnumerable<(K, V)> prettyMuchADictionary,
            IEqualityComparer<K> comparer
        )
        {
            return prettyMuchADictionary.ToDictionary(kvp => kvp.Item1, kvp => kvp.Item2, comparer);
        }

        public static bool ContentEquals<K, V>(
            this IReadOnlyDictionary<K, V> dictionary,
            IReadOnlyDictionary<K, V> other
        )
            where V : IEquatable<V>
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

            if (dictionary.Count == 0)
            {
                return true;
            }

            foreach (KeyValuePair<K, V> entry in dictionary)
            {
                if (!other.TryGetValue(entry.Key, out V value) || !entry.Value.Equals(value))
                {
                    return false;
                }
            }

            return true;
        }

        public static void Deconstruct<K, V>(this KeyValuePair<K, V> kvp, out K key, out V value)
        {
            key = kvp.Key;
            value = kvp.Value;
        }
    }
}
