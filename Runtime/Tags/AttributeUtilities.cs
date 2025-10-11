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

    /// <summary>
    /// Provides utility methods and extension methods for working with the attribute/effect system.
    /// Includes methods for applying/removing effects, checking tags, and discovering attribute fields via reflection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Key features:
    /// - Extension methods for applying effects to any Unity Object
    /// - Tag checking utilities
    /// - Reflection-based attribute field discovery with caching
    /// - Integration with AttributeMetadataCache for performance
    /// </para>
    /// <para>
    /// Example usage:
    /// <code>
    /// // Extension method usage
    /// GameObject player = ...;
    /// AttributeEffect speedBoost = ...;
    ///
    /// // Apply an effect
    /// EffectHandle? handle = player.ApplyEffect(speedBoost);
    ///
    /// // Check tags
    /// if (player.HasTag("Stunned"))
    /// {
    ///     // Can't move
    /// }
    ///
    /// // Remove effect
    /// if (handle.HasValue)
    /// {
    ///     player.RemoveEffect(handle.Value);
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public static class AttributeUtilities
    {
        internal static string[] AllAttributeNames;
        internal static readonly Dictionary<Type, Dictionary<string, FieldInfo>> AttributeFields =
            new();

        private static readonly Dictionary<
            Type,
            Dictionary<string, Func<object, Attribute>>
        > OptimizedAttributeFields = new();

        /// <summary>
        /// Gets an array of all unique attribute field names across all AttributesComponent subclasses.
        /// Results are cached for performance. Uses AttributeMetadataCache if available, otherwise uses reflection.
        /// </summary>
        /// <returns>An array of all attribute names discovered in the project.</returns>
        public static string[] GetAllAttributeNames()
        {
            if (AllAttributeNames != null)
            {
                return AllAttributeNames;
            }

            // Try to load from cache first
            AttributeMetadataCache cache = AttributeMetadataCache.Instance;
            if (cache != null && cache.AllAttributeNames.Length > 0)
            {
                AllAttributeNames = cache.AllAttributeNames;
                return AllAttributeNames;
            }

            // Fallback to runtime reflection if cache is not available
            AllAttributeNames = ReflectionHelpers
                .GetAllLoadedTypes()
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

            return AllAttributeNames;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ClearCache()
        {
            AllAttributeNames = null;
            AttributeFields.Clear();
            OptimizedAttributeFields.Clear();
        }

        /// <summary>
        /// Extension method to check if a Unity Object has a specific tag.
        /// </summary>
        /// <param name="target">The Unity Object (GameObject or Component) to check.</param>
        /// <param name="effectTag">The tag to check for.</param>
        /// <returns><c>true</c> if the target has a TagHandler with the specified tag; otherwise, <c>false</c>.</returns>
        public static bool HasTag(this Object target, string effectTag)
        {
            if (target == null)
            {
                return false;
            }

            return target.TryGetComponent(out TagHandler tagHandler)
                && tagHandler.HasTag(effectTag);
        }

        /// <summary>
        /// Extension method to check if a Unity Object has any of the specified tags.
        /// </summary>
        /// <param name="target">The Unity Object (GameObject or Component) to check.</param>
        /// <param name="effectTags">The collection of tags to check for.</param>
        /// <returns><c>true</c> if the target has any of the specified tags; otherwise, <c>false</c>.</returns>
        public static bool HasAnyTag(this Object target, IEnumerable<string> effectTags)
        {
            if (target == null)
            {
                return false;
            }

            return target.TryGetComponent(out TagHandler tagHandler)
                && tagHandler.HasAnyTag(effectTags);
        }

        /// <summary>
        /// Extension method to check if a Unity Object has any of the specified tags (IReadOnlyList overload for performance).
        /// </summary>
        /// <param name="target">The Unity Object (GameObject or Component) to check.</param>
        /// <param name="effectTags">The list of tags to check for.</param>
        /// <returns><c>true</c> if the target has any of the specified tags; otherwise, <c>false</c>.</returns>
        public static bool HasAnyTag(this Object target, IReadOnlyList<string> effectTags)
        {
            if (target == null)
            {
                return false;
            }

            return target.TryGetComponent(out TagHandler tagHandler)
                && tagHandler.HasAnyTag(effectTags);
        }

        /// <summary>
        /// Extension method to apply an effect to a Unity Object.
        /// Automatically adds an EffectHandler component if one doesn't exist.
        /// </summary>
        /// <param name="target">The Unity Object (GameObject or Component) to apply the effect to.</param>
        /// <param name="attributeEffect">The effect to apply.</param>
        /// <returns>An EffectHandle for non-instant effects, or null for instant effects.</returns>
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
                    // Try to use cached field names first
                    AttributeMetadataCache cache = AttributeMetadataCache.Instance;
                    if (cache != null && cache.TryGetFieldNames(inputType, out string[] fieldNames))
                    {
                        // Build dictionary from cached field names
                        Dictionary<string, FieldInfo> result = new(
                            fieldNames.Length,
                            StringComparer.Ordinal
                        );
                        foreach (string fieldName in fieldNames)
                        {
                            FieldInfo field = inputType.GetField(
                                fieldName,
                                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                            );
                            if (field != null && field.FieldType == typeof(Attribute))
                            {
                                result[fieldName] = field;
                            }
                        }
                        return result;
                    }

                    // Fallback to runtime reflection
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
                    // Try to use cached field names first
                    AttributeMetadataCache cache = AttributeMetadataCache.Instance;
                    if (cache != null && cache.TryGetFieldNames(inputType, out string[] fieldNames))
                    {
                        // Build dictionary from cached field names
                        Dictionary<string, Func<object, Attribute>> result = new(
                            fieldNames.Length,
                            StringComparer.Ordinal
                        );
                        foreach (string fieldName in fieldNames)
                        {
                            FieldInfo field = inputType.GetField(
                                fieldName,
                                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                            );
                            if (field != null && field.FieldType == typeof(Attribute))
                            {
                                result[fieldName] = ReflectionHelpers.GetFieldGetter<
                                    object,
                                    Attribute
                                >(field);
                            }
                        }
                        return result;
                    }

                    // Fallback to runtime reflection
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
