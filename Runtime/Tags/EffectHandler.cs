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

        private readonly Dictionary<EffectStackKey, List<EffectHandle>> _handlesByStackKey = new();
        private readonly Dictionary<long, EffectStackKey> _stackKeyByHandleId = new();

        // Stores expiration time of duration effects (We store by Id because it's much cheaper to iterate Guids than it is EffectHandles
        private readonly Dictionary<long, float> _effectExpirations = new();
        private readonly Dictionary<long, EffectHandle> _effectHandlesById = new();

        // Used only to save allocations in Update()
        private readonly List<long> _expiredEffectIds = new();
        private readonly List<EffectHandle> _appliedEffects = new();
        private readonly Dictionary<long, List<PeriodicEffectRuntimeState>> _periodicEffectStates =
            new();
        private readonly Dictionary<long, List<EffectBehavior>> _behaviorsByHandleId = new();

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
            if (effect == null)
            {
                return null;
            }

            if (effect.durationType == ModifierDurationType.Instant)
            {
                if (RequiresHandle(effect))
                {
                    this.LogWarn(
                        $"Effect {effect:json} defines periodic or behaviour data but is Instant. These features require a Duration or Infinite effect."
                    );
                }

                InternalApplyEffect(effect);
                return null;
            }

            EffectStackKey stackKey = effect.GetStackKey();
            List<EffectHandle> existingHandles = TryGetStackHandles(stackKey);

            switch (effect.stackingMode)
            {
                case EffectStackingMode.Ignore:
                {
                    if (existingHandles is { Count: > 0 })
                    {
                        return existingHandles[0];
                    }

                    break;
                }
                case EffectStackingMode.Refresh:
                {
                    if (existingHandles is { Count: > 0 })
                    {
                        EffectHandle handle = existingHandles[0];
                        InternalApplyEffect(handle);
                        return handle;
                    }

                    break;
                }
                case EffectStackingMode.Replace:
                {
                    if (existingHandles is { Count: > 0 })
                    {
                        using PooledResource<List<EffectHandle>> handleBufferResource =
                            Buffers<EffectHandle>.List.Get(out List<EffectHandle> handleBuffer);
                        handleBuffer.AddRange(existingHandles);
                        for (int i = 0; i < handleBuffer.Count; ++i)
                        {
                            RemoveEffect(handleBuffer[i]);
                        }
                    }

                    break;
                }
                case EffectStackingMode.Stack:
                {
                    if (existingHandles is { Count: > 0 } && effect.maximumStacks > 0)
                    {
                        while (existingHandles.Count >= effect.maximumStacks)
                        {
                            EffectHandle oldestHandle = existingHandles[0];
                            RemoveEffect(oldestHandle);
                        }
                    }

                    break;
                }
            }

            EffectHandle newHandle = EffectHandle.CreateInstance(effect);
            RegisterStackHandle(stackKey, newHandle);
            InternalApplyEffect(newHandle);
            return newHandle;
        }

        private static bool RequiresHandle(AttributeEffect effect)
        {
            return (effect.periodicEffects is { Count: > 0 })
                || (effect.behaviors is { Count: > 0 });
        }

        private List<EffectHandle> TryGetStackHandles(EffectStackKey stackKey)
        {
            _ = _handlesByStackKey.TryGetValue(stackKey, out List<EffectHandle> handles);
            return handles;
        }

        private void RegisterStackHandle(EffectStackKey stackKey, EffectHandle handle)
        {
            long handleId = handle.id;
            _stackKeyByHandleId[handleId] = stackKey;

            if (!_handlesByStackKey.TryGetValue(stackKey, out List<EffectHandle> handles))
            {
                handles = new List<EffectHandle>();
                _handlesByStackKey.Add(stackKey, handles);
            }

            handles.Add(handle);
        }

        /// <summary>
        /// Removes a specific effect by its handle, cleaning up tags, cosmetic effects, and attribute modifications.
        /// </summary>
        /// <param name="handle">The handle of the effect to remove.</param>
        public void RemoveEffect(EffectHandle handle)
        {
            InternalRemoveEffect(handle);
            _ = _appliedEffects.Remove(handle);
            DeregisterHandle(handle);
        }

        public void RemoveAllEffects()
        {
            using PooledResource<List<EffectHandle>> handleBufferResource =
                Buffers<EffectHandle>.List.Get(out List<EffectHandle> handleBuffer);
            handleBuffer.AddRange(_appliedEffects);
            foreach (EffectHandle handle in handleBuffer)
            {
                RemoveEffect(handle);
            }
            _appliedEffects.Clear();
        }

        private void DeregisterHandle(EffectHandle handle)
        {
            long handleId = handle.id;
            if (_stackKeyByHandleId.TryGetValue(handleId, out EffectStackKey stackKey))
            {
                if (_handlesByStackKey.TryGetValue(stackKey, out List<EffectHandle> handles))
                {
                    handles.Remove(handle);
                    if (handles.Count == 0)
                    {
                        _handlesByStackKey.Remove(stackKey);
                    }
                }

                _stackKeyByHandleId.Remove(handleId);
            }

            _periodicEffectStates.Remove(handleId);

            if (_behaviorsByHandleId.Remove(handleId, out List<EffectBehavior> behaviorInstances))
            {
                EffectBehaviorContext context = new(this, handle, 0f);
                for (int i = 0; i < behaviorInstances.Count; ++i)
                {
                    EffectBehavior behavior = behaviorInstances[i];
                    if (behavior == null)
                    {
                        continue;
                    }

                    behavior.OnRemove(context);
                    Destroy(behavior);
                }
            }
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

            long handleId = handle.id;
            _effectHandlesById[handleId] = handle;

            AttributeEffect effect = handle.effect;
            if (effect.durationType == ModifierDurationType.Duration)
            {
                if (!exists || effect.resetDurationOnReapplication)
                {
                    _effectExpirations[handleId] = Time.time + effect.duration;
                }
            }

            if (!exists)
            {
                RegisterPeriodicRuntime(handle);
                RegisterBehaviors(handle);
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
            if (effect.durationType == ModifierDurationType.Instant && RequiresHandle(effect))
            {
                this.LogWarn(
                    $"Effect {effect:json} defines periodic or behaviour data but is Instant. These features require a Duration or Infinite effect."
                );
            }

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

        private void RegisterPeriodicRuntime(EffectHandle handle)
        {
            AttributeEffect effect = handle.effect;
            if (effect.periodicEffects is not { Count: > 0 })
            {
                return;
            }

            List<PeriodicEffectRuntimeState> runtimeStates = null;
            float startTime = Time.time;

            foreach (PeriodicEffectDefinition definition in effect.periodicEffects)
            {
                if (definition == null)
                {
                    continue;
                }

                (runtimeStates ??= new List<PeriodicEffectRuntimeState>()).Add(
                    new PeriodicEffectRuntimeState(definition, startTime)
                );
            }

            if (runtimeStates is { Count: > 0 })
            {
                _periodicEffectStates[handle.id] = runtimeStates;
            }
        }

        private void RegisterBehaviors(EffectHandle handle)
        {
            AttributeEffect effect = handle.effect;
            if (effect.behaviors is not { Count: > 0 })
            {
                return;
            }

            List<EffectBehavior> instances = null;
            foreach (EffectBehavior behavior in effect.behaviors)
            {
                if (behavior == null)
                {
                    continue;
                }

                EffectBehavior clone = Instantiate(behavior);
                (instances ??= new List<EffectBehavior>()).Add(clone);
            }

            if (instances is not { Count: > 0 })
            {
                return;
            }

            EffectBehaviorContext context = new(this, handle, 0f);
            for (int i = 0; i < instances.Count; ++i)
            {
                EffectBehavior instance = instances[i];
                if (instance == null)
                {
                    continue;
                }

                instance.OnApply(context);
            }

            _behaviorsByHandleId[handle.id] = instances;
        }

        private void ApplyPeriodicTick(
            EffectHandle handle,
            PeriodicEffectRuntimeState runtimeState,
            float currentTime,
            float deltaTime
        )
        {
            PeriodicEffectDefinition definition = runtimeState.definition;
            if (_attributes is { Count: > 0 } && definition.modifications is { Count: > 0 })
            {
                foreach (AttributesComponent attributesComponent in _attributes)
                {
                    attributesComponent.ApplyAttributeModifications(definition.modifications, null);
                }
            }

            if (
                _behaviorsByHandleId.TryGetValue(handle.id, out List<EffectBehavior> behaviors)
                && behaviors.Count > 0
            )
            {
                EffectBehaviorContext context = new(this, handle, deltaTime);
                PeriodicEffectTickContext tickContext = new(
                    definition,
                    runtimeState.ExecutedTicks,
                    currentTime
                );

                for (int i = 0; i < behaviors.Count; ++i)
                {
                    EffectBehavior behavior = behaviors[i];
                    if (behavior == null)
                    {
                        continue;
                    }

                    behavior.OnPeriodicTick(context, tickContext);
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
                if (cosmeticEffectData == null)
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
                cosmeticEffectData.GetComponents(cosmeticEffectsBuffer);
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
            ProcessEffectExpirations();
            ProcessBehaviorTicks();
            ProcessPeriodicEffects();
        }

        private void ProcessEffectExpirations()
        {
            if (_effectExpirations.Count <= 0)
            {
                return;
            }

            _expiredEffectIds.Clear();
            float currentTime = Time.time;
            foreach (KeyValuePair<long, float> entry in _effectExpirations)
            {
                if (entry.Value <= currentTime)
                {
                    _expiredEffectIds.Add(entry.Key);
                }
            }

            for (int i = 0; i < _expiredEffectIds.Count; ++i)
            {
                long expiredHandleId = _expiredEffectIds[i];
                if (_effectHandlesById.TryGetValue(expiredHandleId, out EffectHandle expiredHandle))
                {
                    RemoveEffect(expiredHandle);
                }
            }

            _expiredEffectIds.Clear();
        }

        private void ProcessBehaviorTicks()
        {
            if (_behaviorsByHandleId.Count <= 0)
            {
                return;
            }

            using PooledResource<List<long>> behaviorHandleIdsResource = Buffers<long>.List.Get(
                out List<long> behaviorHandleIdsBuffer
            );
            behaviorHandleIdsBuffer.AddRange(_behaviorsByHandleId.Keys);
            float deltaTime = Time.deltaTime;

            for (int i = 0; i < behaviorHandleIdsBuffer.Count; ++i)
            {
                long handleId = behaviorHandleIdsBuffer[i];
                if (!_effectHandlesById.TryGetValue(handleId, out EffectHandle handle))
                {
                    continue;
                }

                if (!_behaviorsByHandleId.TryGetValue(handleId, out List<EffectBehavior> behaviors))
                {
                    continue;
                }

                EffectBehaviorContext context = new(this, handle, deltaTime);
                for (int j = 0; j < behaviors.Count; ++j)
                {
                    EffectBehavior behavior = behaviors[j];
                    if (behavior == null)
                    {
                        continue;
                    }

                    behavior.OnTick(context);
                }
            }
        }

        private void ProcessPeriodicEffects()
        {
            if (_periodicEffectStates.Count <= 0)
            {
                return;
            }

            float currentTime = Time.time;
            float deltaTime = Time.deltaTime;
            using PooledResource<List<long>> periodicRemovalResource = Buffers<long>.List.Get(
                out List<long> periodicRemovalBuffer
            );
            using PooledResource<List<long>> periodHandleIdsResource = Buffers<long>.List.Get(
                out List<long> periodicHandleIdsBuffer
            );
            periodicHandleIdsBuffer.AddRange(_periodicEffectStates.Keys);

            for (int handleIndex = 0; handleIndex < periodicHandleIdsBuffer.Count; ++handleIndex)
            {
                long handleId = periodicHandleIdsBuffer[handleIndex];
                if (!_effectHandlesById.TryGetValue(handleId, out EffectHandle handle))
                {
                    periodicRemovalBuffer.Add(handleId);
                    continue;
                }

                if (
                    !_periodicEffectStates.TryGetValue(
                        handleId,
                        out List<PeriodicEffectRuntimeState> runtimes
                    )
                )
                {
                    continue;
                }

                bool hasActive = false;

                for (int i = 0; i < runtimes.Count; ++i)
                {
                    PeriodicEffectRuntimeState runtimeState = runtimes[i];
                    if (runtimeState == null)
                    {
                        continue;
                    }

                    while (runtimeState.TryConsumeTick(currentTime))
                    {
                        ApplyPeriodicTick(handle, runtimeState, currentTime, deltaTime);
                    }

                    if (!runtimeState.IsComplete)
                    {
                        hasActive = true;
                    }
                }

                if (!hasActive)
                {
                    periodicRemovalBuffer.Add(handleId);
                }
            }

            for (int i = 0; i < periodicRemovalBuffer.Count; ++i)
            {
                _periodicEffectStates.Remove(periodicRemovalBuffer[i]);
            }
        }
    }
}
