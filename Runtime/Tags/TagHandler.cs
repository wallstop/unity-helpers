namespace WallstopStudios.UnityHelpers.Tags
{
    using System;
    using System.Collections.Generic;
    using Core.Extension;
    using UnityEngine;

    [DisallowMultipleComponent]
    public sealed class TagHandler : MonoBehaviour
    {
        public event Action<string> OnTagAdded;
        public event Action<string> OnTagRemoved;
        public event Action<string, uint> OnTagCountChanged;

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

        public bool HasTag(string effectTag)
        {
            if (string.IsNullOrEmpty(effectTag))
            {
                return false;
            }

            return _tagCount.ContainsKey(effectTag);
        }

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

        public void ApplyTag(string effectTag)
        {
            InternalApplyTag(effectTag);
        }

        public void RemoveTag(string effectTag, bool allInstances)
        {
            if (allInstances)
            {
                _tagCount.Remove(effectTag);
                return;
            }

            InternalRemoveTag(effectTag);
        }

        public void ForceApplyTags(EffectHandle handle)
        {
            long id = handle.id;
            if (!_effectHandles.TryAdd(id, handle))
            {
                return;
            }

            ForceApplyEffect(handle.effect);
        }

        public void ForceApplyEffect(AttributeEffect effect)
        {
            foreach (string effectTag in effect.effectTags)
            {
                InternalApplyTag(effectTag);
            }
        }

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
