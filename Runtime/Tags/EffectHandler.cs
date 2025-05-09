namespace WallstopStudios.UnityHelpers.Tags
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Attributes;
    using Core.DataStructure.Adapters;
    using Core.Extension;
    using Core.Helper;
    using UnityEngine;
    using Utils;

    [DisallowMultipleComponent]
    [RequireComponent(typeof(TagHandler))]
    public sealed class EffectHandler : MonoBehaviour
    {
        [SiblingComponent()]
        private TagHandler _tagHandler;

        [SiblingComponent(optional = true)]
        private AttributesComponent[] _attributes;

        // Stores instanced cosmetic effect data for associated effects.
        private readonly Dictionary<
            EffectHandle,
            List<CosmeticEffectData>
        > _instancedCosmeticEffects = new();

        // Stores expiration time of duration effects (We store by Id because it's much cheaper to iterate Guids than it is EffectHandles
        private readonly Dictionary<KGuid, float> _effectExpirations = new();
        private readonly Dictionary<KGuid, EffectHandle> _effectHandlesById = new();

        // Used only to save allocations in Update()
        private readonly List<KGuid> _expiredEffectIds = new();
        private readonly List<EffectHandle> _appliedEffects = new();

        private bool _initialized;

        private void Awake()
        {
            this.AssignRelationalComponents();
            _initialized = true;
        }

        public EffectHandle? ApplyEffect(AttributeEffect effect)
        {
            EffectHandle? maybeHandle = null;

            if (effect.durationType != ModifierDurationType.Instant)
            {
                if (effect.durationType == ModifierDurationType.Duration)
                {
                    foreach (EffectHandle appliedEffect in _appliedEffects)
                    {
                        if (appliedEffect.effect == null)
                        {
                            continue;
                        }

                        string serializableName = appliedEffect.effect.name;
                        if (string.Equals(effect.name, serializableName))
                        {
                            maybeHandle = appliedEffect;
                            break;
                        }
                    }
                }

                maybeHandle ??= EffectHandle.CreateInstance(effect);
            }

            if (maybeHandle.HasValue)
            {
                EffectHandle handle = maybeHandle.Value;
                InternalApplyEffect(handle);
                if (
                    effect.durationType == ModifierDurationType.Duration
                    && (effect.resetDurationOnReapplication || !_appliedEffects.Contains(handle))
                )
                {
                    KGuid handleId = handle.id;
                    _effectExpirations[handleId] = Time.time + effect.duration;
                    _effectHandlesById[handleId] = handle;
                }
            }
            else
            {
                InternalApplyEffect(effect);
            }

            return maybeHandle;
        }

        public void RemoveEffect(EffectHandle handle)
        {
            InternalRemoveEffect(handle);
            _ = _appliedEffects.Remove(handle);
        }

        public void RemoveAllEffects()
        {
            foreach (EffectHandle handle in _appliedEffects.ToArray())
            {
                InternalRemoveEffect(handle);
            }
            _appliedEffects.Clear();
        }

        private void InternalRemoveEffect(EffectHandle handle)
        {
            foreach (AttributesComponent attributesComponent in _attributes)
            {
                attributesComponent.ForceRemoveAttributeModifications(handle);
            }

            if (!_initialized && _tagHandler == null)
            {
                this.AssignRelationalComponents();
            }

            // Then, tags are removed (so cosmetic components can look up if any tags are still applied)
            if (_tagHandler != null)
            {
                _ = _tagHandler.ForceRemoveTags(handle);
            }

            KGuid handleId = handle.id;
            _ = _effectExpirations.Remove(handleId);
            _ = _effectHandlesById.Remove(handleId);
            InternalRemoveCosmeticEffects(handle);
        }

        private void InternalApplyEffect(EffectHandle handle)
        {
            bool exists = _appliedEffects.Contains(handle);
            if (!exists)
            {
                _appliedEffects.Add(handle);
            }

            AttributeEffect effect = handle.effect;
            if (effect.durationType == ModifierDurationType.Duration)
            {
                if (effect.resetDurationOnReapplication || !exists)
                {
                    KGuid handleId = handle.id;
                    _effectExpirations[handleId] = Time.time + effect.duration;
                    _effectHandlesById[handleId] = handle;
                }
            }

            if (!_initialized && _tagHandler == null)
            {
                this.AssignRelationalComponents();
            }

            if (_tagHandler != null && effect.effectTags is { Count: > 0 })
            {
                _tagHandler.ForceApplyTags(handle);
            }

            if (effect.cosmeticEffects is { Count: > 0 })
            {
                InternalApplyCosmeticEffects(handle);
            }

            if (effect.modifications is { Count: > 0 })
            {
                foreach (AttributesComponent attributesComponent in _attributes)
                {
                    attributesComponent.ForceApplyAttributeModifications(handle);
                }
            }
        }

        private void InternalApplyEffect(AttributeEffect effect)
        {
            if (!_initialized && _tagHandler == null)
            {
                this.AssignRelationalComponents();
            }

            if (_tagHandler != null && effect.effectTags is { Count: > 0 })
            {
                _tagHandler.ForceApplyEffect(effect);
            }

            if (effect.cosmeticEffects is { Count: > 0 })
            {
                InternalApplyCosmeticEffects(effect);
            }

            if (effect.modifications is { Count: > 0 })
            {
                foreach (AttributesComponent attributesComponent in _attributes)
                {
                    attributesComponent.ForceApplyAttributeModifications(effect);
                }
            }
        }

        private void InternalApplyCosmeticEffects(EffectHandle handle)
        {
            if (_instancedCosmeticEffects.ContainsKey(handle))
            {
                return;
            }

            List<CosmeticEffectData> instancedCosmeticData = null;

            AttributeEffect effect = handle.effect;

            foreach (CosmeticEffectData cosmeticEffectData in effect.cosmeticEffects)
            {
                CosmeticEffectData cosmeticEffect = cosmeticEffectData;
                if (cosmeticEffect == null)
                {
                    this.LogError(
                        $"CosmeticEffectData is null for effect {effect:json}, cannot determine instancing scheme."
                    );
                    continue;
                }

                if (cosmeticEffectData.RequiresInstancing)
                {
                    cosmeticEffect = Instantiate(
                        cosmeticEffectData,
                        transform.position,
                        Quaternion.identity
                    );
                    cosmeticEffect.transform.SetParent(transform, true);
                    (instancedCosmeticData ??= new List<CosmeticEffectData>()).Add(cosmeticEffect);
                }

                Buffers<CosmeticEffectComponent>.List.Clear();
                cosmeticEffect.GetComponents(Buffers<CosmeticEffectComponent>.List);
                foreach (
                    CosmeticEffectComponent cosmeticComponent in Buffers<CosmeticEffectComponent>.List
                )
                {
                    cosmeticComponent.OnApplyEffect(gameObject);
                }
            }

            if (instancedCosmeticData != null)
            {
                _instancedCosmeticEffects.Add(handle, instancedCosmeticData);
            }
        }

        private void InternalApplyCosmeticEffects(AttributeEffect attributeEffect)
        {
            foreach (CosmeticEffectData cosmeticEffectData in attributeEffect.cosmeticEffects)
            {
                CosmeticEffectData cosmeticEffect = cosmeticEffectData;
                if (cosmeticEffect == null)
                {
                    this.LogError(
                        $"CosmeticEffectData is null for effect {attributeEffect:json}, cannot determine instancing scheme."
                    );
                    continue;
                }

                if (cosmeticEffectData.RequiresInstancing)
                {
                    this.LogError(
                        $"CosmeticEffectData requires instancing, but can't instance (no handle)."
                    );
                    continue;
                }

                Buffers<CosmeticEffectComponent>.List.Clear();
                cosmeticEffect.GetComponents(Buffers<CosmeticEffectComponent>.List);
                foreach (
                    CosmeticEffectComponent cosmeticComponent in Buffers<CosmeticEffectComponent>.List
                )
                {
                    cosmeticComponent.OnApplyEffect(gameObject);
                }
            }
        }

        private void InternalRemoveCosmeticEffects(EffectHandle handle)
        {
            if (
                !_instancedCosmeticEffects.TryGetValue(
                    handle,
                    out List<CosmeticEffectData> cosmeticDatas
                )
            )
            {
                // If we don't have instanced cosmetic effects, then they were applied directly to the cosmetic data
                foreach (
                    CosmeticEffectData cosmeticEffectData in handle.effect.cosmeticEffects
                        ?? Enumerable.Empty<CosmeticEffectData>()
                )
                {
                    if (cosmeticEffectData.RequiresInstancing)
                    {
                        this.LogWarn(
                            $"Double-deregistration detected for handle {handle:json}. Existing handles: [{string.Join(",", _instancedCosmeticEffects.Keys)}]."
                        );
                        continue;
                    }

                    Buffers<CosmeticEffectComponent>.List.Clear();
                    cosmeticEffectData.GetComponents(Buffers<CosmeticEffectComponent>.List);
                    foreach (
                        CosmeticEffectComponent cosmeticComponent in Buffers<CosmeticEffectComponent>.List
                    )
                    {
                        cosmeticComponent.OnRemoveEffect(gameObject);
                    }
                }

                return;
            }

            foreach (
                CosmeticEffectComponent cosmeticComponent in cosmeticDatas.SelectMany(
                    cosmeticData => cosmeticData.GetComponents<CosmeticEffectComponent>()
                )
            )
            {
                cosmeticComponent.OnRemoveEffect(gameObject);
            }

            foreach (CosmeticEffectData data in cosmeticDatas)
            {
                bool shouldDestroyGameObject = true;
                Buffers<CosmeticEffectComponent>.List.Clear();
                data.GetComponents(Buffers<CosmeticEffectComponent>.List);
                foreach (
                    CosmeticEffectComponent cosmeticEffect in Buffers<CosmeticEffectComponent>.List
                )
                {
                    if (cosmeticEffect.CleansUpSelf)
                    {
                        shouldDestroyGameObject = false;
                        continue;
                    }

                    cosmeticEffect.Destroy();
                }

                if (shouldDestroyGameObject)
                {
                    data.gameObject.Destroy();
                }
            }

            _ = _instancedCosmeticEffects.Remove(handle);
        }

        private void Update()
        {
            if (_effectExpirations.Count <= 0)
            {
                return;
            }

            _expiredEffectIds.Clear();
            float currentTime = Time.time;
            foreach (KeyValuePair<KGuid, float> entry in _effectExpirations)
            {
                if (entry.Value < currentTime)
                {
                    _expiredEffectIds.Add(entry.Key);
                }
            }

            foreach (KGuid expiredHandleId in _expiredEffectIds)
            {
                if (_effectHandlesById.TryGetValue(expiredHandleId, out EffectHandle expiredHandle))
                {
                    RemoveEffect(expiredHandle);
                }
            }
        }
    }
}
