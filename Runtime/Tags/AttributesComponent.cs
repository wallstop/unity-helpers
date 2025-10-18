namespace WallstopStudios.UnityHelpers.Tags
{
    using System;
    using System.Collections.Generic;
    using Core.Attributes;
    using UnityEngine;

    /// <summary>
    /// Abstract base class for components that contain Attribute fields to be modified by effects.
    /// Subclasses should define public or private Attribute fields that can be dynamically modified.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This component automatically:
    /// - Discovers all Attribute fields via reflection (optimized with caching)
    /// - Registers with the EffectHandler to receive attribute modifications
    /// - Applies and removes modifications based on effect handles
    /// - Notifies listeners when attributes change
    /// </para>
    /// <para>
    /// Example usage:
    /// <code>
    /// public class CharacterStats : AttributesComponent
    /// {
    ///     public Attribute Health = new Attribute(100f);
    ///     public Attribute Speed = new Attribute(5f);
    ///     public Attribute Damage = new Attribute(10f);
    ///
    ///     protected override void Awake()
    ///     {
    ///         base.Awake();
    ///         OnAttributeModified += (attrName, oldVal, newVal) =>
    ///         {
    ///             Debug.Log($"{attrName} changed from {oldVal} to {newVal}");
    ///         };
    ///     }
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(TagHandler))]
    [RequireComponent(typeof(EffectHandler))]
    public abstract class AttributesComponent : MonoBehaviour
    {
        /// <summary>
        /// Invoked when an attribute's value changes due to an effect being applied or removed.
        /// Provides the attribute name, old value, and new value.
        /// </summary>
        public event Action<string, float, float> OnAttributeModified;

        private Dictionary<string, Func<object, Attribute>> _attributeFieldGetters;
        private readonly HashSet<EffectHandle> _effectHandles;

        [SiblingComponent]
        protected TagHandler _tagHandler;

        [SiblingComponent]
        protected EffectHandler _effectHandler;

        /// <summary>
        /// Initializes the AttributesComponent by discovering all Attribute fields in the derived class.
        /// </summary>
        protected AttributesComponent()
        {
            _effectHandles = new HashSet<EffectHandle>();
        }

        /// <summary>
        /// Initializes sibling components and registers with the EffectHandler.
        /// Override this method in derived classes, but always call base.Awake().
        /// </summary>
        protected virtual void Awake()
        {
            EnsureAttributeFieldGettersInitialized();
            this.AssignSiblingComponents();
            _effectHandler.Register(this);
        }

        /// <summary>
        /// Unregisters from the EffectHandler when destroyed.
        /// Override this method in derived classes, but always call base.OnDestroy().
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (_effectHandler != null)
            {
                _effectHandler.Remove(this);
            }
        }

        /// <summary>
        /// Applies a collection of attribute modifications, either instantly or with an effect handle.
        /// </summary>
        /// <param name="attributeModifications">The modifications to apply.</param>
        /// <param name="handle">Optional effect handle for tracking. If null, modifications are permanent.</param>
        public void ApplyAttributeModifications(
            IEnumerable<AttributeModification> attributeModifications,
            EffectHandle? handle
        )
        {
            if (handle.HasValue)
            {
                ForceApplyAttributeModifications(handle.Value);
                return;
            }

            InternalApplyAttributeModifications(attributeModifications);
        }

        /// <summary>
        /// Removes all attribute modifications associated with the specified effect handle.
        /// Called automatically by the EffectHandler when an effect is removed.
        /// </summary>
        /// <param name="handle">The effect handle whose modifications should be removed.</param>
        public void ForceRemoveAttributeModifications(EffectHandle handle)
        {
            InternalRemoveAttributeModifications(handle);
        }

        private void InternalApplyAttributeModifications(
            IEnumerable<AttributeModification> attributeModifications
        )
        {
            foreach (AttributeModification modification in attributeModifications)
            {
                if (!TryGetAttribute(modification.attribute, out Attribute attribute))
                {
                    continue;
                }

                float oldValue = attribute;
                attribute.ApplyAttributeModification(modification);
                float currentValue = attribute;

                OnAttributeModified?.Invoke(modification.attribute, oldValue, currentValue);
            }
        }

        /// <summary>
        /// Applies all attribute modifications from an effect handle.
        /// Called automatically by the EffectHandler when an effect is applied.
        /// </summary>
        /// <param name="handle">The effect handle containing modifications to apply.</param>
        public void ForceApplyAttributeModifications(EffectHandle handle)
        {
            AttributeEffect effect = handle.effect;
            if (effect.modifications is not { Count: > 0 })
            {
                return;
            }

            bool isNewEffect = false;
            foreach (AttributeModification modification in effect.modifications)
            {
                if (!TryGetAttribute(modification.attribute, out Attribute attribute))
                {
                    continue;
                }

                isNewEffect |= _effectHandles.Add(handle);
                if (isNewEffect)
                {
                    float oldValue = attribute;
                    attribute.ApplyAttributeModification(modification, handle);
                    float currentValue = attribute;
                    OnAttributeModified?.Invoke(modification.attribute, oldValue, currentValue);
                }
            }
        }

        /// <summary>
        /// Applies all attribute modifications from an effect without tracking a handle.
        /// Used for instant effects.
        /// </summary>
        /// <param name="effect">The effect containing modifications to apply.</param>
        public void ForceApplyAttributeModifications(AttributeEffect effect)
        {
            if (effect.modifications is not { Count: > 0 })
            {
                return;
            }

            foreach (AttributeModification modification in effect.modifications)
            {
                if (!TryGetAttribute(modification.attribute, out Attribute attribute))
                {
                    continue;
                }

                float oldValue = attribute;
                attribute.ApplyAttributeModification(modification);
                float currentValue = attribute;
                OnAttributeModified?.Invoke(modification.attribute, oldValue, currentValue);
            }
        }

        private void InternalRemoveAttributeModifications(EffectHandle handle)
        {
            AttributeEffect effect = handle.effect;
            if (effect.modifications is not { Count: > 0 })
            {
                return;
            }

            foreach (AttributeModification modification in effect.modifications)
            {
                if (!TryGetAttribute(modification.attribute, out Attribute attribute))
                {
                    continue;
                }

                float oldValue = attribute;
                _ = attribute.RemoveAttributeModification(handle);
                float currentValue = attribute;
                _ = _effectHandles.Remove(handle);
                OnAttributeModified?.Invoke(modification.attribute, oldValue, currentValue);
            }
        }

        protected bool TryGetAttribute(string attributeName, out Attribute attribute)
        {
            EnsureAttributeFieldGettersInitialized();
            if (
                !_attributeFieldGetters.TryGetValue(
                    attributeName,
                    out Func<object, Attribute> getter
                )
            )
            {
                attribute = default;
                return false;
            }

            attribute = getter(this);
            return true;
        }

        protected void EnsureAttributeFieldGettersInitialized()
        {
            if (_attributeFieldGetters != null)
            {
                return;
            }

            _attributeFieldGetters = AttributeUtilities.GetOptimizedAttributeFields(GetType());
        }
    }
}
