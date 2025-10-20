namespace WallstopStudios.UnityHelpers.Tags
{
    using System;
    using System.Collections.Generic;
    using Core.Extension;
    using UnityEngine;

    /// <summary>
    /// Tag system for gameplay state: applies, counts, and queries string-based tags on a GameObject.
    /// Used to represent transient states (stunned, poisoned) and effect categories without coupling to specific effects.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Why tags? Tags decouple “what is active” from “what applied it.” Systems can ask
    /// “is Stunned?” or “has any of X,Y?” without caring which effect created the state.
    /// This enables clean gating (e.g., block movement while Stunned) and cross‑system coordination.
    /// </para>
    /// <para>
    /// Counting semantics: TagHandler maintains a reference count per tag. Multiple effects can apply the
    /// same tag concurrently; the tag remains active until its count returns to 0. This solves common issues
    /// where removing one source would accidentally clear the state still required by another effect.
    /// </para>
    /// <para>
    /// Integration: The <see cref="EffectHandler"/> coordinates tag application/removal via
    /// <see cref="ForceApplyTags(EffectHandle)"/> and <see cref="ForceRemoveTags(EffectHandle)"/>.
    /// Instant effects can call <see cref="ForceApplyEffect(AttributeEffect)"/> since no handle exists.
    /// </para>
    /// <para>
    /// Benefits:
    /// - Decoupled state queries across systems (AI, input, animation)
    /// - Safe stacking via counts (no premature clears)
    /// - Lightweight string keys with event notifications for UI/FX
    /// - Optimized overloads for common collection types
    /// </para>
    /// <para>
    /// Usage examples:
    /// <code>
    /// TagHandler tags = gameObject.GetComponent&lt;TagHandler&gt;();
    ///
    /// // Querying
    /// if (tags.HasTag("Stunned")) { /* disable input */ }
    /// if (tags.HasAnyTag(new [] { "Frozen", "Stunned" })) { /* play break-free anim */ }
    ///
    /// // Manual application (advanced; normally applied via EffectHandler)
    /// tags.ApplyTag("Poisoned");
    /// tags.RemoveTag("Poisoned", allInstances: false);
    ///
    /// // Events for UI/telemetry
    /// tags.OnTagAdded += tag => Debug.Log($"+{tag}");
    /// tags.OnTagRemoved += tag => Debug.Log($"-{tag}");
    /// tags.OnTagCountChanged += (tag, count) => Debug.Log($"{tag}: {count}");
    /// </code>
    /// </para>
    /// <para>
    /// Tips:
    /// - Keep tag strings consistent (consider central constants to avoid typos).
    /// - Prefer using AttributeEffects to drive tags rather than calling ApplyTag/RemoveTag directly.
    /// - Use <see cref="HasAnyTag(System.Collections.Generic.IReadOnlyList{string})"/> for perf‑critical code.
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
        /// Checks whether all of the specified tags are currently active.
        /// Optimized for different collection types with specialized implementations.
        /// </summary>
        /// <param name="effectTags">The collection of tags to check.</param>
        /// <returns><c>true</c> if all tags are active; otherwise, <c>false</c>. Returns <c>false</c> when <paramref name="effectTags"/> is <c>null</c>.</returns>
        public bool HasAllTags(IEnumerable<string> effectTags)
        {
            if (effectTags == null)
            {
                return false;
            }

            switch (effectTags)
            {
                case IReadOnlyList<string> list:
                {
                    return HasAllTags(list);
                }
                case HashSet<string> hashSet:
                {
                    foreach (string effectTag in hashSet)
                    {
                        if (string.IsNullOrEmpty(effectTag))
                        {
                            continue;
                        }
                        if (!_tagCount.ContainsKey(effectTag))
                        {
                            return false;
                        }
                    }

                    return true;
                }
                case SortedSet<string> sortedSet:
                {
                    foreach (string effectTag in sortedSet)
                    {
                        if (string.IsNullOrEmpty(effectTag))
                        {
                            continue;
                        }
                        if (!_tagCount.ContainsKey(effectTag))
                        {
                            return false;
                        }
                    }

                    return true;
                }
                case Queue<string> queue:
                {
                    foreach (string effectTag in queue)
                    {
                        if (string.IsNullOrEmpty(effectTag))
                        {
                            continue;
                        }
                        if (!_tagCount.ContainsKey(effectTag))
                        {
                            return false;
                        }
                    }

                    return true;
                }
                case Stack<string> stack:
                {
                    foreach (string effectTag in stack)
                    {
                        if (string.IsNullOrEmpty(effectTag))
                        {
                            continue;
                        }
                        if (!_tagCount.ContainsKey(effectTag))
                        {
                            return false;
                        }
                    }

                    return true;
                }
                case LinkedList<string> linkedList:
                {
                    foreach (string effectTag in linkedList)
                    {
                        if (string.IsNullOrEmpty(effectTag))
                        {
                            continue;
                        }
                        if (!_tagCount.ContainsKey(effectTag))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            foreach (string effectTag in effectTags)
            {
                if (string.IsNullOrEmpty(effectTag))
                {
                    continue;
                }
                if (!_tagCount.ContainsKey(effectTag))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks whether all of the specified tags are active.
        /// Optimized for IReadOnlyList with index-based iteration.
        /// </summary>
        /// <param name="effectTags">The list of tags to check.</param>
        /// <returns><c>true</c> if all of the tags are active, or if the list is empty; otherwise, <c>false</c>.</returns>
        public bool HasAllTags(IReadOnlyList<string> effectTags)
        {
            if (effectTags == null)
            {
                return false;
            }

            for (int i = 0; i < effectTags.Count; ++i)
            {
                string effectTag = effectTags[i];
                if (string.IsNullOrEmpty(effectTag))
                {
                    continue;
                }

                if (!_tagCount.ContainsKey(effectTag))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether none of the specified tags are active.
        /// </summary>
        /// <param name="effectTags">The collection of tags to check.</param>
        /// <returns><c>true</c> if none of the tags are currently active; otherwise, <c>false</c>.</returns>
        public bool HasNoneOfTags(IEnumerable<string> effectTags)
        {
            if (effectTags == null)
            {
                return true;
            }

            return !HasAnyTag(effectTags);
        }

        /// <summary>
        /// Determines whether none of the specified tags are active.
        /// </summary>
        /// <param name="effectTags">The list of tags to check.</param>
        /// <returns><c>true</c> if none of the tags are currently active; otherwise, <c>false</c>.</returns>
        public bool HasNoneOfTags(IReadOnlyList<string> effectTags)
        {
            if (effectTags == null)
            {
                return true;
            }

            return !HasAnyTag(effectTags);
        }

        /// <summary>
        /// Attempts to retrieve the active instance count for the specified tag.
        /// </summary>
        /// <param name="effectTag">The tag whose count should be retrieved.</param>
        /// <param name="count">When this method returns, contains the active count for the tag if found; otherwise, zero.</param>
        /// <returns><c>true</c> if the tag is currently tracked; otherwise, <c>false</c>.</returns>
        public bool TryGetTagCount(string effectTag, out int count)
        {
            if (string.IsNullOrEmpty(effectTag))
            {
                count = default;
                return false;
            }

            if (_tagCount.TryGetValue(effectTag, out uint uintCount))
            {
                count = unchecked((int)uintCount);
                return true;
            }

            count = default;
            return false;
        }

        /// <summary>
        /// Copies the currently active tags into the provided buffer.
        /// </summary>
        /// <param name="buffer">The list to populate with active tags.</param>
        public List<string> GetActiveTags(List<string> buffer = null)
        {
            buffer ??= new List<string>();
            buffer.Clear();
            foreach (KeyValuePair<string, uint> entry in _tagCount)
            {
                if (entry.Value == 0)
                {
                    continue;
                }

                buffer.Add(entry.Key);
            }

            return buffer;
        }

        /// <summary>
        /// Retrieves all active effect handles that contributed the specified tag.
        /// </summary>
        /// <param name="effectTag">The tag to query.</param>
        /// <param name="buffer">The list to populate with effect handles.</param>
        /// <returns><c>true</c> if one or more handles were added to the buffer; otherwise, <c>false</c>.</returns>
        public List<EffectHandle> GetHandlesWithTag(
            string effectTag,
            List<EffectHandle> buffer = null
        )
        {
            buffer ??= new List<EffectHandle>();
            buffer.Clear();
            if (string.IsNullOrEmpty(effectTag))
            {
                return buffer;
            }

            foreach (EffectHandle handle in _effectHandles.Values)
            {
                if (
                    handle.effect.effectTags != null
                    && handle.effect.effectTags.Contains(effectTag)
                )
                {
                    buffer.Add(handle);
                }
            }

            return buffer;
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
        /// Removes all instances of the specified tag, returning the removed effect handles.
        /// </summary>
        /// <param name="effectTag">The tag to remove.</param>
        /// <param name="buffer">Buffer of removed effect handles.</param>
        public List<EffectHandle> RemoveTag(string effectTag, List<EffectHandle> buffer = null)
        {
            buffer ??= new List<EffectHandle>();
            buffer.Clear();
            if (string.IsNullOrEmpty(effectTag))
            {
                return buffer;
            }

            foreach (EffectHandle handle in _effectHandles.Values)
            {
                if (
                    handle.effect.effectTags != null
                    && handle.effect.effectTags.Contains(effectTag)
                )
                {
                    buffer.Add(handle);
                }
            }

            foreach (EffectHandle handle in buffer)
            {
                ForceRemoveTags(handle);
            }

            InternalRemoveTag(effectTag, allInstances: true);
            return buffer;
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

            ApplyEffectTags(handle.effect);
        }

        /// <summary>
        /// Applies all tags from an effect without tracking a handle.
        /// Used for instant effects that don't need removal tracking.
        /// </summary>
        /// <param name="effect">The effect containing tags to apply.</param>
        public void ForceApplyEffect(AttributeEffect effect)
        {
            ApplyEffectTags(effect);
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

            RemoveEffectTags(appliedHandle.effect);
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

        private void InternalRemoveTag(string effectTag, bool allInstances)
        {
            if (!_tagCount.TryGetValue(effectTag, out uint count))
            {
                return;
            }

            if (count != 0)
            {
                if (!allInstances)
                {
                    --count;
                }
                else
                {
                    count = 0;
                }
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

        private void ApplyEffectTags(AttributeEffect effect)
        {
            if (effect.effectTags == null)
            {
                return;
            }

            foreach (string effectTag in effect.effectTags)
            {
                InternalApplyTag(effectTag);
            }
        }

        private void RemoveEffectTags(AttributeEffect effect)
        {
            if (effect.effectTags == null)
            {
                return;
            }

            foreach (string effectTag in effect.effectTags)
            {
                InternalRemoveTag(effectTag, allInstances: false);
            }
        }
    }
}
