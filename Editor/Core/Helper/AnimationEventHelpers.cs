namespace WallstopStudios.UnityHelpers.Editor.Core.Helper
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Utils;

    public static class AnimationEventHelpers
    {
        public static readonly IReadOnlyDictionary<Type, IReadOnlyList<MethodInfo>> TypesToMethods;

        static AnimationEventHelpers()
        {
            List<(Type, string)> ignoreDerived = new();
            Dictionary<Type, List<MethodInfo>> typesToMethods = new();

            TypeCache.TypeCollection monoTypes = TypeCache.GetTypesDerivedFrom<MonoBehaviour>();
            for (int i = 0; i < monoTypes.Count; i++)
            {
                Type type = monoTypes[i];
                if (type == null || !type.IsClass || type.IsAbstract)
                {
                    continue;
                }

                List<MethodInfo> definedMethods = GetPossibleAnimatorEventsForType(type);
                // Filter: only methods directly declared on this type and attributed
                for (int m = definedMethods.Count - 1; m >= 0; m--)
                {
                    MethodInfo method = definedMethods[m];
                    if (method.DeclaringType != type)
                    {
                        definedMethods.RemoveAt(m);
                        continue;
                    }
                    if (!method.IsAttributeDefined<AnimationEventAttribute>(out _, inherit: false))
                    {
                        definedMethods.RemoveAt(m);
                    }
                }

                if (definedMethods.Count > 0)
                {
                    // Include inherited methods that explicitly allow derived
                    List<MethodInfo> allPossible = GetPossibleAnimatorEventsForType(type);
                    for (int m = 0; m < allPossible.Count; m++)
                    {
                        MethodInfo candidate = allPossible[m];
                        if (candidate.DeclaringType == type)
                        {
                            continue;
                        }
                        if (
                            !candidate.IsAttributeDefined<AnimationEventAttribute>(
                                out AnimationEventAttribute attribute,
                                inherit: false
                            )
                        )
                        {
                            continue;
                        }
                        if (attribute.ignoreDerived)
                        {
                            continue;
                        }

                        // Re-resolve method on its declaring type with exact parameter types
                        ParameterInfo[] parameters = candidate.GetParameters();
                        Type[] paramTypes;
                        if (parameters is { Length: > 0 })
                        {
                            paramTypes = new Type[parameters.Length];
                            for (int pi = 0; pi < parameters.Length; pi++)
                            {
                                paramTypes[pi] = parameters[pi].ParameterType;
                            }
                        }
                        else
                        {
                            paramTypes = Array.Empty<Type>();
                        }

                        MethodInfo resolved = candidate.DeclaringType.GetMethod(
                            candidate.Name,
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                            null,
                            paramTypes,
                            null
                        );
                        if (resolved != null)
                        {
                            definedMethods.Add(resolved);
                        }
                    }
                }

                for (int m = 0; m < definedMethods.Count; m++)
                {
                    MethodInfo definedMethod = definedMethods[m];
                    if (
                        definedMethod.IsAttributeDefined(
                            out AnimationEventAttribute attr,
                            inherit: false
                        ) && attr.ignoreDerived
                    )
                    {
                        ignoreDerived.Add((type, definedMethod.Name));
                    }
                }

                if (definedMethods.Count > 0)
                {
                    typesToMethods[type] = definedMethods;
                }
            }

            using (
                PooledResource<List<KeyValuePair<Type, List<MethodInfo>>>> methodBufferResource =
                    Buffers<KeyValuePair<Type, List<MethodInfo>>>.List.Get()
            )
            {
                List<KeyValuePair<Type, List<MethodInfo>>> methodBuffer =
                    methodBufferResource.resource;
                foreach (KeyValuePair<Type, List<MethodInfo>> entry in typesToMethods)
                {
                    methodBuffer.Add(entry);
                }

                foreach (KeyValuePair<Type, List<MethodInfo>> entry in methodBuffer)
                {
                    if (entry.Value.Count <= 0)
                    {
                        _ = typesToMethods.Remove(entry.Key);
                        continue;
                    }

                    Type key = entry.Key;
                    for (int i = 0; i < ignoreDerived.Count; i++)
                    {
                        (Type baseType, string methodName) = ignoreDerived[i];
                        if (key == baseType)
                        {
                            continue;
                        }
                        if (!key.IsSubclassOf(baseType))
                        {
                            continue;
                        }

                        // Remove inherited methods with this name
                        for (int midx = entry.Value.Count - 1; midx >= 0; midx--)
                        {
                            if (entry.Value[midx].Name == methodName)
                            {
                                entry.Value.RemoveAt(midx);
                            }
                        }
                        if (entry.Value.Count <= 0)
                        {
                            _ = typesToMethods.Remove(entry.Key);
                            break;
                        }
                    }
                }
            }

            // Project to IReadOnlyList without LINQ
            Dictionary<Type, IReadOnlyList<MethodInfo>> ro = new();
            foreach (KeyValuePair<Type, List<MethodInfo>> kvp in typesToMethods)
            {
                ro[kvp.Key] = kvp.Value;
            }
            TypesToMethods = ro;
        }

        public static List<MethodInfo> GetPossibleAnimatorEventsForType(Type type)
        {
            MethodInfo[] methods = type.GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );
            using (Buffers<MethodInfo>.List.Get(out List<MethodInfo> result))
            {
                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo m = methods[i];
                    if (m.ReturnType != typeof(void))
                    {
                        continue;
                    }

                    ParameterInfo[] ps = m.GetParameters();
                    bool ok;
                    if (ps == null || ps.Length == 0)
                    {
                        ok = true;
                    }
                    else if (ps.Length == 1)
                    {
                        Type pt = ps[0].ParameterType;
                        ok =
                            pt == typeof(int)
                            || pt == typeof(float)
                            || pt == typeof(string)
                            || pt == typeof(UnityEngine.Object)
                            || (pt.BaseType == typeof(Enum));
                    }
                    else
                    {
                        ok = false;
                    }

                    if (ok)
                    {
                        result.Add(m);
                    }
                }

                result.Sort(
                    static (a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal)
                );
                // Return a new list to avoid exposing pooled instance
                return new List<MethodInfo>(result);
            }
        }
    }
}
