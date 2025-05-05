namespace WallstopStudios.UnityHelpers.Tags
{
    using System.Collections.Generic;
    using Core.Extension;
    using UnityEngine;

    [RequireComponent(typeof(CosmeticEffectData))]
    public abstract class CosmeticEffectComponent : MonoBehaviour
    {
        public virtual bool RequiresInstance => false;

        public virtual bool CleansUpSelf => false;

        protected readonly List<GameObject> _appliedTargets = new();

        protected virtual void OnDestroy()
        {
            if (_appliedTargets.Count <= 0)
            {
                return;
            }

            foreach (GameObject appliedTarget in _appliedTargets.ToArray())
            {
                if (appliedTarget == null)
                {
                    continue;
                }

                OnRemoveEffect(appliedTarget);
            }
        }

        // Executed when the associated effect is applied.
        public virtual void OnApplyEffect(GameObject target)
        {
            _appliedTargets.Add(target);
        }

        // Executed when the associated effect is non-instant and removed.
        public virtual void OnRemoveEffect(GameObject target)
        {
            int appliedIndex = _appliedTargets.IndexOf(target);
            if (0 <= appliedIndex)
            {
                _appliedTargets.RemoveAtSwapBack(appliedIndex);
            }
        }
    }
}
