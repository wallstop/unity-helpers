namespace WallstopStudios.UnityHelpers.Tags
{
    using System;
    using System.Collections.Generic;
    using Core.Attributes;
    using Core.Extension;
    using Core.Helper;
    using UnityEngine;
    using Utils;

    /// <summary>
    /// Manages the application and removal of AttributeEffects on a GameObject.
    /// Handles effect duration tracking, tag application, cosmetic effects, and attribute modifications.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The EffectHandler is the central component for the effect system. It:
    /// - Applies effects and creates handles for tracking
    /// - Manages effect durations and automatic expiration
    /// - Coordinates with TagHandler for tag-based effects
    /// - Manages cosmetic effect instantiation and cleanup
    /// - Distributes attribute modifications to AttributesComponents
    /// </para>
    /// <para>
    /// Example usage:
    /// <code>
    /// EffectHandler effectHandler = gameObject.GetComponent&lt;EffectHandler&gt;();
    ///
    /// // Apply an effect
    /// AttributeEffect poisonEffect = ...;
    /// EffectHandle? handle = effectHandler.ApplyEffect(poisonEffect);
    ///
    /// // Remove a specific effect
    /// if (handle.HasValue)
    /// {
    ///     effectHandler.RemoveEffect(handle.Value);
    /// }
    ///
    /// // Remove all effects
    /// effectHandler.RemoveAllEffects();
    /// </code>
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TagHandler))]
    public sealed class EffectHandler : MonoBehaviour
    {
        /// <summary>
        /// Invoked when an effect is successfully applied.
        /// </summary>
        public event Action<EffectHandle> OnEffectApplied;

        /// <summary>
        /// Invoked when an effect is removed (either manually or through expiration).
        /// </summary>
        public event Action<EffectHandle> OnEffectRemoved;

        [SiblingComponent]
        private TagHandler _tagHandler;

        [SiblingComponent(Optional = true)]
        private HashSet<AttributesComponent> _attributes;

        // Stores instanced cosmetic effect data for associated effects.
        private readonly Dictionary<
            EffectHandle,
            List<CosmeticEffectData>
        > _instancedCosmeticEffects = new();

        // Stores expiration time of duration effects (We store by Id because it's much cheaper to iterate Guids than it is EffectHandles
        private readonly Dictionary<long, float> _effectExpirations = new();
        private readonly Dictionary<long, EffectHandle> _effectHandlesById = new();

        // Used only to save allocations in Update()
        private readonly List<long> _expiredEffectIds = new();
        private readonly List<EffectHandle> _appliedEffects = new();

        private bool _initialized;

        private void Awake()
        {
            this.AssignRelationalComponents();
            _initialized = true;
        }

        /// <summary>
        /// Registers an AttributesComponent to receive effect modifications.
        /// Called automatically by AttributesComponent during Awake.
        /// </summary>
        /// <param name="attributesComponent">The component to register.</param>
        internal void Register(AttributesComponent attributesComponent)
        {
            _attributes ??= new HashSet<AttributesComponent>();
            _ = _attributes.Add(attributesComponent);
        }

        /// <summary>
        /// Unregisters an AttributesComponent from receiving effect modifications.
        /// Called automatically by AttributesComponent during OnDestroy.
        /// </summary>
        /// <param name="attributesComponent">The component to unregister.</param>
        internal void Remove(AttributesComponent attributesComponent)
        {
            _attributes?.Remove(attributesComponent);
        }

        /// <summary>
        /// Applies an AttributeEffect to this GameObject, handling tags, cosmetic effects, and attribute modifications.
        /// </summary>
        /// <param name="effect">The effect to apply.</param>
        /// <returns>
        /// An EffectHandle if the effect is non-instant (Duration or Infinite), allowing later removal.
        /// Null for instant effects that permanently modify base values.
        /// </returns>
        /// <remarks>
        /// For Duration effects with the same name, reapplying can either reset the timer (if resetDurationOnReapplication is true)
        /// or be ignored if already active.
        /// </remarks>
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
                        if (string.Equals(effect.name, serializableName, StringComparison.Ordinal))
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
                    long handleId = handle.id;
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

        /// <summary>
        /// Removes a specific effect by its handle, cleaning up tags, cosmetic effects, and attribute modifications.
        /// </summary>
        /// <param name="handle">The handle of the effect to remove.</param>
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

        /// <summary>
        /// Determines whether the specified effect is currently active on this handler.
        /// </summary>
        /// <param name="effect">The effect to check.</param>
        /// <returns><c>true</c> if at least one handle for the effect is active; otherwise, <c>false</c>.</returns>
        public bool IsEffectActive(AttributeEffect effect)
        {
            if (effect == null)
            {
                return false;
            }

            for (int i = 0; i < _appliedEffects.Count; ++i)
            {
                EffectHandle handle = _appliedEffects[i];
                if (handle.effect == effect)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the number of active handles for the specified effect.
        /// </summary>
        /// <param name="effect">The effect to count.</param>
        /// <returns>The number of active handles associated with <paramref name="effect"/>.</returns>
        public int GetEffectStackCount(AttributeEffect effect)
        {
            if (effect == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < _appliedEffects.Count; ++i)
            {
                EffectHandle handle = _appliedEffects[i];
                if (handle.effect == effect)
                {
                    ++count;
                }
            }

            return count;
        }

        /// <summary>
        /// Copies all active effect handles into the provided buffer.
        /// </summary>
        /// <param name="buffer">
        /// Optional list to populate. When <c>null</c>, a new list is created. The buffer is cleared before population.
        /// </param>
        /// <returns>The populated buffer containing all currently active effect handles.</returns>
        public List<EffectHandle> GetActiveEffects(List<EffectHandle> buffer = null)
        {
            buffer ??= new List<EffectHandle>();
            buffer.Clear();
            buffer.AddRange(_appliedEffects);
            return buffer;
        }

        /// <summary>
        /// Attempts to retrieve the remaining duration for the specified effect handle.
        /// </summary>
        /// <param name="handle">The handle to inspect.</param>
        /// <param name="remainingDuration">When this method returns, contains the remaining time in seconds, or zero if unavailable.</param>
        /// <returns><c>true</c> if the handle has a tracked duration; otherwise, <c>false</c>.</returns>
        public bool TryGetRemainingDuration(EffectHandle handle, out float remainingDuration)
        {
            long handleId = handle.id;
            if (!_effectExpirations.TryGetValue(handleId, out float expiration))
            {
                remainingDuration = 0f;
                return false;
            }

            float timeRemaining = expiration - Time.time;
            if (timeRemaining < 0f)
            {
                timeRemaining = 0f;
            }

            remainingDuration = timeRemaining;
            return true;
        }

        /// <summary>
        /// Ensures an effect handle exists for the specified effect, optionally refreshing its duration if already active.
        /// </summary>
        /// <param name="effect">The effect to apply or refresh.</param>
        /// <returns>An active handle for the effect, or <c>null</c> for instant effects.</returns>
        public EffectHandle? EnsureHandle(AttributeEffect effect)
        {
            return EnsureHandle(effect, refreshDuration: true);
        }

        /// <summary>
        /// Ensures an effect handle exists for the specified effect, optionally refreshing its duration if already active.
        /// </summary>
        /// <param name="effect">The effect to apply or refresh.</param>
        /// <param name="refreshDuration">
        /// When <c>true</c>, attempts to refresh the effect's duration when it is already active and supports reapplication.
        /// </param>
        /// <returns>An active handle for the effect, or <c>null</c> for instant effects.</returns>
        public EffectHandle? EnsureHandle(AttributeEffect effect, bool refreshDuration)
        {
            if (effect == null)
            {
                return null;
            }

            for (int i = 0; i < _appliedEffects.Count; ++i)
            {
                EffectHandle handle = _appliedEffects[i];
                if (handle.effect == effect)
                {
                    if (refreshDuration)
                    {
                        _ = RefreshEffect(handle);
                    }

                    return handle;
                }
            }

            return ApplyEffect(effect);
        }

        /// <summary>
        /// Attempts to refresh the duration of the specified effect handle.
        /// </summary>
        /// <param name="handle">The handle to refresh.</param>
        /// <returns><c>true</c> if the duration was refreshed; otherwise, <c>false</c>.</returns>
        public bool RefreshEffect(EffectHandle handle)
        {
            return RefreshEffect(handle, ignoreReapplicationPolicy: false);
        }

        /// <summary>
        /// Attempts to refresh the duration of the specified effect handle.
        /// </summary>
        /// <param name="handle">The handle to refresh.</param>
        /// <param name="ignoreReapplicationPolicy">
        /// When <c>true</c>, refreshes the duration even if <see cref="AttributeEffect.resetDurationOnReapplication"/> is <c>false</c>.
        /// </param>
        /// <returns><c>true</c> if the duration was refreshed; otherwise, <c>false</c>.</returns>
        public bool RefreshEffect(EffectHandle handle, bool ignoreReapplicationPolicy)
        {
            AttributeEffect effect = handle.effect;
            if (effect == null)
            {
                return false;
            }

            if (effect.durationType != ModifierDurationType.Duration)
            {
                return false;
            }

            if (!ignoreReapplicationPolicy && !effect.resetDurationOnReapplication)
            {
                return false;
            }

            long handleId = handle.id;
            if (!_effectExpirations.ContainsKey(handleId))
            {
                return false;
            }

            float newExpiration = Time.time + effect.duration;
            _effectExpirations[handleId] = newExpiration;
            _effectHandlesById[handleId] = handle;
            return true;
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

            long handleId = handle.id;
            _ = _effectExpirations.Remove(handleId);
            _ = _effectHandlesById.Remove(handleId);
            InternalRemoveCosmeticEffects(handle);
            OnEffectRemoved?.Invoke(handle);
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
                    long handleId = handle.id;
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

            OnEffectApplied?.Invoke(handle);
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

                using PooledResource<List<CosmeticEffectComponent>> cosmeticEffectsResource =
                    Buffers<CosmeticEffectComponent>.List.Get();
                List<CosmeticEffectComponent> cosmeticEffectsBuffer =
                    cosmeticEffectsResource.resource;
                cosmeticEffect.GetComponents(cosmeticEffectsBuffer);
                foreach (CosmeticEffectComponent cosmeticComponent in cosmeticEffectsBuffer)
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

                using PooledResource<List<CosmeticEffectComponent>> cosmeticEffectsResource =
                    Buffers<CosmeticEffectComponent>.List.Get();
                List<CosmeticEffectComponent> cosmeticEffectsBuffer =
                    cosmeticEffectsResource.resource;
                cosmeticEffect.GetComponents(cosmeticEffectsBuffer);
                foreach (CosmeticEffectComponent cosmeticComponent in cosmeticEffectsBuffer)
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
                if (handle.effect?.cosmeticEffects == null)
                {
                    return;
                }

                // If we don't have instanced cosmetic effects, then they were applied directly to the cosmetic data
                foreach (CosmeticEffectData cosmeticEffectData in handle.effect.cosmeticEffects)
                {
                    if (cosmeticEffectData.RequiresInstancing)
                    {
                        this.LogWarn(
                            $"Double-deregistration detected for handle {handle:json}. Existing handles: [{string.Join(",", _instancedCosmeticEffects.Keys)}]."
                        );
                        continue;
                    }

                    using PooledResource<List<CosmeticEffectComponent>> cosmeticEffectsResource =
                        Buffers<CosmeticEffectComponent>.List.Get();
                    List<CosmeticEffectComponent> cosmeticEffectsBuffer =
                        cosmeticEffectsResource.resource;
                    cosmeticEffectData.GetComponents(cosmeticEffectsBuffer);
                    foreach (CosmeticEffectComponent cosmeticComponent in cosmeticEffectsBuffer)
                    {
                        cosmeticComponent.OnRemoveEffect(gameObject);
                    }
                }

                return;
            }

            foreach (CosmeticEffectData cosmeticData in cosmeticDatas)
            {
                using PooledResource<List<CosmeticEffectComponent>> cosmeticEffectsResource =
                    Buffers<CosmeticEffectComponent>.List.Get();
                List<CosmeticEffectComponent> cosmeticEffectsBuffer =
                    cosmeticEffectsResource.resource;
                cosmeticData.GetComponents(cosmeticEffectsBuffer);
                foreach (CosmeticEffectComponent cosmeticComponent in cosmeticEffectsBuffer)
                {
                    cosmeticComponent.OnRemoveEffect(gameObject);
                }
            }

            foreach (CosmeticEffectData data in cosmeticDatas)
            {
                bool shouldDestroyGameObject = true;
                using PooledResource<List<CosmeticEffectComponent>> cosmeticEffectsResource =
                    Buffers<CosmeticEffectComponent>.List.Get();
                List<CosmeticEffectComponent> cosmeticEffectsBuffer =
                    cosmeticEffectsResource.resource;
                data.GetComponents(cosmeticEffectsBuffer);
                foreach (CosmeticEffectComponent cosmeticEffect in cosmeticEffectsBuffer)
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
            foreach (KeyValuePair<long, float> entry in _effectExpirations)
            {
                if (entry.Value < currentTime)
                {
                    _expiredEffectIds.Add(entry.Key);
                }
            }

            foreach (long expiredHandleId in _expiredEffectIds)
            {
                if (_effectHandlesById.TryGetValue(expiredHandleId, out EffectHandle expiredHandle))
                {
                    RemoveEffect(expiredHandle);
                }
            }
        }
    }
}
