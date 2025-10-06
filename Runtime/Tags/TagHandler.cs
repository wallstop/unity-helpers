namespace WallstopStudios.UnityHelpers.Tags
{
    using System;
    using System.Collections.Generic;
    using Core.Extension;
    using UnityEngine;

    /// <summary>
    /// Manages string-based tags for a GameObject, tracking tag counts and notifying listeners of changes.
    /// Tags are commonly used to represent active effect categories, status conditions, or gameplay states.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The TagHandler maintains a count for each tag, allowing the same tag to be applied multiple times.
    /// This is useful when multiple effects might apply the same tag (e.g., multiple "Stunned" effects).
    /// The tag is only considered removed when its count reaches zero.
    /// </para>
    /// <para>
    /// Example usage:
    /// <code>
    /// TagHandler tagHandler = gameObject.GetComponent&lt;TagHandler&gt;();
    ///
    /// // Check if a tag is active
    /// if (tagHandler.HasTag("Stunned"))
    /// {
    ///     // Character is stunned
    /// }
    ///
    /// // Add a tag
    /// tagHandler.ApplyTag("Poisoned");
    ///
    /// // Remove a tag
    /// tagHandler.RemoveTag("Poisoned", allInstances: false);
    ///
    /// // Listen for tag changes
    /// tagHandler.OnTagAdded += (tag) => Debug.Log($"Tag added: {tag}");
    /// </code>
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class TagHandler : MonoBehaviour
    {
        /// <summary>
        /// Invoked when a tag is first applied (count goes from 0 to 1).
        /// </summary>
        public event Action<string> OnTagAdded;

        /// <summary>
        /// Invoked when a tag is completely removed (count goes from 1 to 0).
        /// </summary>
        public event Action<string> OnTagRemoved;

        /// <summary>
        /// Invoked when a tag's count changes but remains above 0.
        /// Provides the tag name and the new count.
        /// </summary>
        public event Action<string, uint> OnTagCountChanged;

        /// <summary>
        /// Gets a read-only collection of all currently active tags (tags with count > 0).
        /// </summary>
        public IReadOnlyCollection<string> Tags => _tagCount.Keys;

        [SerializeField]
        private List<string> _initialEffectTags = new();

        private readonly Dictionary<string, uint> _tagCount = new(StringComparer.Ordinal);
        private readonly Dictionary<long, EffectHandle> _effectHandles = new();

        private void Awake()
        {
            if (_initialEffectTags is { Count: > 0 })
            {
                foreach (string effectTag in _initialEffectTags)
                {
                    InternalApplyTag(effectTag);
                }
            }
        }

        /// <summary>
        /// Checks whether the specified tag is currently active (has a count > 0).
        /// </summary>
        /// <param name="effectTag">The tag to check for.</param>
        /// <returns><c>true</c> if the tag is active; otherwise, <c>false</c>. Returns <c>false</c> for null or empty strings.</returns>
        public bool HasTag(string effectTag)
        {
            if (string.IsNullOrEmpty(effectTag))
            {
                return false;
            }

            return _tagCount.ContainsKey(effectTag);
        }

        /// <summary>
        /// Checks whether any of the specified tags are currently active.
        /// Optimized for different collection types with specialized implementations.
        /// </summary>
        /// <param name="effectTags">The collection of tags to check.</param>
        /// <returns><c>true</c> if any of the tags are active; otherwise, <c>false</c>.</returns>
        public bool HasAnyTag(IEnumerable<string> effectTags)
        {
            switch (effectTags)
            {
                case IReadOnlyList<string> list:
                {
                    return HasAnyTag(list);
                }
                case HashSet<string> hashSet:
                {
                    foreach (string effectTag in hashSet)
                    {
                        if (string.IsNullOrEmpty(effectTag))
                        {
                            continue;
                        }
                        if (_tagCount.ContainsKey(effectTag))
                        {
                            return true;
                        }
                    }

                    return false;
                }
                case SortedSet<string> sortedSet:
                {
                    foreach (string effectTag in sortedSet)
                    {
                        if (string.IsNullOrEmpty(effectTag))
                        {
                            continue;
                        }
                        if (_tagCount.ContainsKey(effectTag))
                        {
                            return true;
                        }
                    }

                    return false;
                }
                case Queue<string> queue:
                {
                    foreach (string effectTag in queue)
                    {
                        if (string.IsNullOrEmpty(effectTag))
                        {
                            continue;
                        }
                        if (_tagCount.ContainsKey(effectTag))
                        {
                            return true;
                        }
                    }

                    return false;
                }
                case Stack<string> stack:
                {
                    foreach (string effectTag in stack)
                    {
                        if (string.IsNullOrEmpty(effectTag))
                        {
                            continue;
                        }
                        if (_tagCount.ContainsKey(effectTag))
                        {
                            return true;
                        }
                    }

                    return false;
                }
                case LinkedList<string> linkedList:
                {
                    foreach (string effectTag in linkedList)
                    {
                        if (string.IsNullOrEmpty(effectTag))
                        {
                            continue;
                        }
                        if (_tagCount.ContainsKey(effectTag))
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }

            foreach (string effectTag in effectTags)
            {
                if (string.IsNullOrEmpty(effectTag))
                {
                    continue;
                }
                if (_tagCount.ContainsKey(effectTag))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks whether any of the specified tags are currently active.
        /// Optimized for IReadOnlyList with index-based iteration.
        /// </summary>
        /// <param name="effectTags">The list of tags to check.</param>
        /// <returns><c>true</c> if any of the tags are active; otherwise, <c>false</c>.</returns>
        public bool HasAnyTag(IReadOnlyList<string> effectTags)
        {
            for (int i = 0; i < effectTags.Count; ++i)
            {
                string effectTag = effectTags[i];
                if (string.IsNullOrEmpty(effectTag))
                {
                    continue;
                }

                if (_tagCount.ContainsKey(effectTag))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Applies a tag, incrementing its count. If the tag is new, raises <see cref="OnTagAdded"/>.
        /// Otherwise, raises <see cref="OnTagCountChanged"/>.
        /// </summary>
        /// <param name="effectTag">The tag to apply.</param>
        public void ApplyTag(string effectTag)
        {
            InternalApplyTag(effectTag);
        }

        /// <summary>
        /// Removes one or all instances of a tag.
        /// </summary>
        /// <param name="effectTag">The tag to remove.</param>
        /// <param name="allInstances">
        /// If true, completely removes the tag regardless of count.
        /// If false, decrements the count by 1, removing it only when count reaches 0.
        /// </param>
        public void RemoveTag(string effectTag, bool allInstances)
        {
            if (allInstances)
            {
                _tagCount.Remove(effectTag);
                return;
            }

            InternalRemoveTag(effectTag);
        }

        /// <summary>
        /// Applies all tags from an effect handle's effect.
        /// Tracks the handle to support later removal via <see cref="ForceRemoveTags"/>.
        /// </summary>
        /// <param name="handle">The effect handle containing tags to apply.</param>
        public void ForceApplyTags(EffectHandle handle)
        {
            long id = handle.id;
            if (!_effectHandles.TryAdd(id, handle))
            {
                return;
            }

            ForceApplyEffect(handle.effect);
        }

        /// <summary>
        /// Applies all tags from an effect without tracking a handle.
        /// Used for instant effects that don't need removal tracking.
        /// </summary>
        /// <param name="effect">The effect containing tags to apply.</param>
        public void ForceApplyEffect(AttributeEffect effect)
        {
            foreach (string effectTag in effect.effectTags)
            {
                InternalApplyTag(effectTag);
            }
        }

        /// <summary>
        /// Removes all tags that were applied by the specified effect handle.
        /// </summary>
        /// <param name="handle">The effect handle whose tags should be removed.</param>
        /// <returns><c>true</c> if the handle was found and tags were removed; otherwise, <c>false</c>.</returns>
        public bool ForceRemoveTags(EffectHandle handle)
        {
            long id = handle.id;
            if (!_effectHandles.Remove(id, out EffectHandle appliedHandle))
            {
                return false;
            }

            foreach (string effectTag in appliedHandle.effect.effectTags)
            {
                InternalRemoveTag(effectTag);
            }

            return true;
        }

        private void InternalApplyTag(string effectTag)
        {
            uint currentCount = _tagCount.AddOrUpdate(
                effectTag,
                _ => 1U,
                (_, existing) => existing + 1
            );
            if (currentCount == 1)
            {
                OnTagAdded?.Invoke(effectTag);
            }
            else
            {
                OnTagCountChanged?.Invoke(effectTag, currentCount);
            }
        }

        private void InternalRemoveTag(string effectTag)
        {
            if (!_tagCount.TryGetValue(effectTag, out uint count))
            {
                return;
            }

            if (count != 0)
            {
                --count;
            }

            if (count == 0)
            {
                _ = _tagCount.Remove(effectTag);
                OnTagRemoved?.Invoke(effectTag);
            }
            else
            {
                _tagCount[effectTag] = count;
                OnTagCountChanged?.Invoke(effectTag, count);
            }
        }
    }
}
