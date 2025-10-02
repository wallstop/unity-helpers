namespace WallstopStudios.UnityHelpers.Tags
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Core.Extension;
    using Core.Helper;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public static class AttributeUtilities
    {
        private static string[] AllAttributeNames;
        private static readonly Dictionary<Type, Dictionary<string, FieldInfo>> AttributeFields =
            new();

        private static readonly Dictionary<
            Type,
            Dictionary<string, Func<object, Attribute>>
        > OptimizedAttributeFields = new();

        // TODO: Use TypeCache + serialize
        public static string[] GetAllAttributeNames()
        {
            return AllAttributeNames ??= AppDomain
                .CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => !type.IsAbstract)
                .Where(type => type.IsSubclassOf(typeof(AttributesComponent)))
                .SelectMany(type =>
                    type.GetFields(
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                    )
                )
                .Where(fieldInfo => fieldInfo.FieldType == typeof(Attribute))
                .Select(fieldInfo => fieldInfo.Name)
                .Distinct()
                .Ordered()
                .ToArray();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ClearCache()
        {
            AllAttributeNames = null;
            AttributeFields.Clear();
            OptimizedAttributeFields.Clear();
        }

        public static bool HasTag(this Object target, string effectTag)
        {
            if (target == null)
            {
                return false;
            }

            return target.TryGetComponent(out TagHandler tagHandler)
                && tagHandler.HasTag(effectTag);
        }

        public static bool HasAnyTag(this Object target, IEnumerable<string> effectTags)
        {
            if (target == null)
            {
                return false;
            }

            return target.TryGetComponent(out TagHandler tagHandler)
                && tagHandler.HasAnyTag(effectTags);
        }

        public static bool HasAnyTag(this Object target, IReadOnlyList<string> effectTags)
        {
            if (target == null)
            {
                return false;
            }

            return target.TryGetComponent(out TagHandler tagHandler)
                && tagHandler.HasAnyTag(effectTags);
        }

        public static EffectHandle? ApplyEffect(this Object target, AttributeEffect attributeEffect)
        {
            if (target == null)
            {
                return null;
            }

            EffectHandler effectHandler = target.GetGameObject().GetOrAddComponent<EffectHandler>();
            return effectHandler.ApplyEffect(attributeEffect);
        }

        public static void ApplyEffectsNoAlloc(
            this Object target,
            List<AttributeEffect> attributeEffects
        )
        {
            if (attributeEffects is not { Count: > 0 })
            {
                return;
            }

            if (target == null)
            {
                return;
            }
            EffectHandler effectHandler = target.GetGameObject().GetOrAddComponent<EffectHandler>();
            foreach (AttributeEffect attributeEffect in attributeEffects)
            {
                _ = effectHandler.ApplyEffect(attributeEffect);
            }
        }

        public static void ApplyEffectsNoAlloc(
            this Object target,
            IEnumerable<AttributeEffect> attributeEffects
        )
        {
            if (target == null)
            {
                return;
            }

            EffectHandler effectHandler = target.GetGameObject().GetOrAddComponent<EffectHandler>();
            foreach (AttributeEffect attributeEffect in attributeEffects)
            {
                _ = effectHandler.ApplyEffect(attributeEffect);
            }
        }

        public static void ApplyEffectsNoAlloc(
            this Object target,
            List<AttributeEffect> attributeEffects,
            List<EffectHandle> effectHandles
        )
        {
            if (target == null)
            {
                return;
            }

            EffectHandler effectHandler = target.GetGameObject().GetOrAddComponent<EffectHandler>();
            foreach (AttributeEffect attributeEffect in attributeEffects)
            {
                EffectHandle? handle = effectHandler.ApplyEffect(attributeEffect);
                if (handle.HasValue)
                {
                    effectHandles.Add(handle.Value);
                }
            }
        }

        public static List<EffectHandle> ApplyEffects(
            this Object target,
            List<AttributeEffect> attributeEffects
        )
        {
            List<EffectHandle> handles = new(attributeEffects.Count);
            ApplyEffectsNoAlloc(target, attributeEffects, handles);
            return handles;
        }

        public static void RemoveEffect(this Object target, EffectHandle effectHandle)
        {
            if (target == null)
            {
                return;
            }

            if (target.TryGetComponent(out EffectHandler effectHandler))
            {
                effectHandler.RemoveEffect(effectHandle);
            }
        }

        public static void RemoveEffects(this Object target, List<EffectHandle> effectHandles)
        {
            if (target == null || effectHandles.Count <= 0)
            {
                return;
            }

            if (target.TryGetComponent(out EffectHandler effectHandler))
            {
                foreach (EffectHandle effectHandle in effectHandles)
                {
                    effectHandler.RemoveEffect(effectHandle);
                }
            }
        }

        public static void RemoveAllEffects(this Object target)
        {
            if (target == null)
            {
                return;
            }

            if (target.TryGetComponent(out EffectHandler effectHandler))
            {
                effectHandler.RemoveAllEffects();
            }
        }

        public static Dictionary<string, FieldInfo> GetAttributeFields(Type type)
        {
            return AttributeFields.GetOrAdd(
                type,
                inputType =>
                {
                    return inputType
                        .GetFields(
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                        )
                        .Where(field => field.FieldType == typeof(Attribute))
                        .ToDictionary(field => field.Name, StringComparer.Ordinal);
                }
            );
        }

        public static Dictionary<string, Func<object, Attribute>> GetOptimizedAttributeFields(
            Type type
        )
        {
            return OptimizedAttributeFields.GetOrAdd(
                type,
                inputType =>
                {
                    return inputType
                        .GetFields(
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                        )
                        .Where(field => field.FieldType == typeof(Attribute))
                        .ToDictionary(
                            field => field.Name,
                            field => ReflectionHelpers.GetFieldGetter<object, Attribute>(field),
                            StringComparer.Ordinal
                        );
                }
            );
        }
    }
}
