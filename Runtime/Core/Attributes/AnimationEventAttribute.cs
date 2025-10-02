namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Utils;

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class AnimationEventAttribute : Attribute
    {
        public bool ignoreDerived = true;
    }

    public static class AnimationEventHelpers
    {
        public static readonly IReadOnlyDictionary<Type, IReadOnlyList<MethodInfo>> TypesToMethods;

        static AnimationEventHelpers()
        {
            List<(Type, string)> ignoreDerived = new();
            Dictionary<Type, List<MethodInfo>> typesToMethods = ReflectionHelpers
                .GetAllLoadedTypes()
                .Where(type => type.IsClass)
                .Where(type => typeof(MonoBehaviour).IsAssignableFrom(type))
                .ToDictionary(
                    type => type,
                    type =>
                    {
                        List<MethodInfo> definedMethods = GetPossibleAnimatorEventsForType(type)
                            .Where(method =>
                            {
                                // Only include methods where the attribute is directly defined
                                if (
                                    !method.IsAttributeDefined<AnimationEventAttribute>(
                                        out _,
                                        inherit: false
                                    )
                                )
                                {
                                    return false;
                                }

                                // Only include methods that are declared on this type
                                return method.DeclaringType == type;
                            })
                            .ToList();

                        // Only include inherited methods if this type has its own handlers
                        if (definedMethods.Count > 0)
                        {
                            // Also include inherited methods that explicitly allow derived types
                            List<MethodInfo> inheritedMethods = GetPossibleAnimatorEventsForType(
                                    type
                                )
                                .Where(method =>
                                {
                                    // Skip if not attributed
                                    if (
                                        !method.IsAttributeDefined<AnimationEventAttribute>(
                                            out _,
                                            inherit: false
                                        )
                                    )
                                    {
                                        return false;
                                    }

                                    // Skip if declared on this type (already handled)
                                    if (method.DeclaringType == type)
                                    {
                                        return false;
                                    }

                                    // Include inherited methods that allow derived
                                    if (
                                        method.IsAttributeDefined(
                                            out AnimationEventAttribute attribute,
                                            inherit: false
                                        )
                                    )
                                    {
                                        return !attribute.ignoreDerived;
                                    }

                                    return false;
                                })
                                .ToList();

                            definedMethods.AddRange(inheritedMethods);
                        }
                        foreach (MethodInfo definedMethod in definedMethods)
                        {
                            // Only consider attributes on our specific method
                            if (
                                !definedMethod.IsAttributeDefined<AnimationEventAttribute>(
                                    out _,
                                    inherit: false
                                )
                            )
                            {
                                continue;
                            }

                            if (
                                definedMethod.IsAttributeDefined(
                                    out AnimationEventAttribute attribute,
                                    inherit: false
                                ) && attribute.ignoreDerived
                            )
                            {
                                ignoreDerived.Add((type, definedMethod.Name));
                            }
                        }

                        return definedMethods;
                    }
                );

            using PooledResource<List<KeyValuePair<Type, List<MethodInfo>>>> methodBufferResource =
                Buffers<KeyValuePair<Type, List<MethodInfo>>>.List.Get();
            List<KeyValuePair<Type, List<MethodInfo>>> methodBuffer = methodBufferResource.resource;
            foreach (KeyValuePair<Type, List<MethodInfo>> entry in typesToMethods)
            {
                methodBuffer.Add(entry);
            }
            foreach (KeyValuePair<Type, List<MethodInfo>> entry in methodBuffer)
            {
                if (entry.Value.Count <= 0)
                {
                    _ = typesToMethods.Remove(entry.Key);
                }

                Type key = entry.Key;
                foreach ((Type type, string methodName) in ignoreDerived)
                {
                    if (key == type)
                    {
                        continue;
                    }

                    if (!key.IsSubclassOf(type))
                    {
                        continue;
                    }

                    entry.Value.RemoveAll(method => method.Name == methodName);
                    if (entry.Value.Count <= 0)
                    {
                        _ = typesToMethods.Remove(entry.Key);
                        break;
                    }
                }
            }

            TypesToMethods = typesToMethods.ToDictionary(
                kvp => kvp.Key,
                kvp => (IReadOnlyList<MethodInfo>)kvp.Value
            );
        }

        public static List<MethodInfo> GetPossibleAnimatorEventsForType(this Type type)
        {
            return type.GetMethods(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                )
                .Where(p =>
                    p.ReturnType == typeof(void)
                    && (
                        p.GetParameters().Select(q => q.ParameterType).SequenceEqual(new Type[] { })
                        || p.GetParameters()
                            .Select(q => q.ParameterType)
                            .SequenceEqual(new Type[] { typeof(int) })
                        || p.GetParameters()
                            .Select(q => q.ParameterType.BaseType)
                            .SequenceEqual(new Type[] { typeof(Enum) })
                        || p.GetParameters()
                            .Select(q => q.ParameterType)
                            .SequenceEqual(new Type[] { typeof(float) })
                        || p.GetParameters()
                            .Select(q => q.ParameterType)
                            .SequenceEqual(new Type[] { typeof(string) })
                        || p.GetParameters()
                            .Select(q => q.ParameterType)
                            .SequenceEqual(new Type[] { typeof(UnityEngine.Object) })
                    )
                )
                .OrderBy(method => method.Name)
                .ToList();
        }
    }
}
