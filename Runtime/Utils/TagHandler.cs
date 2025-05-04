namespace WallstopStudios.UnityHelpers.Utils
{
    using System.Collections.Generic;
    using Core.Extension;
    using UnityEngine;

    [DisallowMultipleComponent]
    public class TagHandler : MonoBehaviour
    {
        [SerializeField]
        protected List<string> _initialEffectTags = new();

        protected readonly Dictionary<string, uint> _tagCount = new();

        protected virtual void Awake()
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
            return _tagCount.ContainsKey(effectTag);
        }

        public bool HasAnyTag(IEnumerable<string> effectTags)
        {
            foreach (string effectTag in effectTags)
            {
                bool hasTag = _tagCount.ContainsKey(effectTag);
                if (hasTag)
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
                bool hasTag = _tagCount.ContainsKey(effectTag);
                if (hasTag)
                {
                    return true;
                }
            }

            return false;
        }

        private void InternalApplyTag(string effectTag)
        {
            _ = _tagCount.AddOrUpdate(effectTag, _ => 1U, (_, existing) => existing + 1);
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
            }
            else
            {
                _tagCount[effectTag] = count;
            }
        }
    }
}
