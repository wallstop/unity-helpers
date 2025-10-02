namespace WallstopStudios.UnityHelpers.Tags
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Core.Attributes;
    using UnityEngine;

    [RequireComponent(typeof(TagHandler))]
    [RequireComponent(typeof(EffectHandler))]
    public abstract class AttributesComponent : MonoBehaviour
    {
        public event Action<string, float, float> OnAttributeModified;

        private readonly Dictionary<string, FieldInfo> _attributeFields;
        private readonly HashSet<EffectHandle> _effectHandles;

        [SiblingComponent]
        protected TagHandler _tagHandler;

        [SiblingComponent]
        protected EffectHandler _effectHandler;

        protected AttributesComponent()
        {
            _attributeFields = AttributeUtilities.GetAttributeFields(GetType());
            _effectHandles = new HashSet<EffectHandle>();
        }

        protected virtual void Awake()
        {
            this.AssignSiblingComponents();
            _effectHandler.Register(this);
        }

        protected virtual void OnDestroy()
        {
            if (_effectHandler != null)
            {
                _effectHandler.Remove(this);
            }
        }

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

        private bool TryGetAttribute(string attributeName, out Attribute attribute)
        {
            if (!_attributeFields.TryGetValue(attributeName, out FieldInfo fieldInfo))
            {
                attribute = default;
                return false;
            }

            object fieldValue = fieldInfo.GetValue(this);
            if (fieldValue is Attribute fieldAttribute)
            {
                attribute = fieldAttribute;
                return true;
            }

            attribute = default;
            return false;
        }
    }
}
