namespace Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using UnityEngine;

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
            Dictionary<Type, List<MethodInfo>> typesToMethods = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsClass)
                .Where(type => typeof(MonoBehaviour).IsAssignableFrom(type))
                .ToDictionary(
                    type => type,
                    type =>
                    {
                        List<MethodInfo> definedMethods = GetPossibleAnimatorEventsForType(type)
                            .Where(method => Attribute.IsDefined(method, typeof(AnimationEventAttribute), true)).ToList();
                        foreach (MethodInfo definedMethod in definedMethods)
                        {
                            // Only consider attributes on our specific method
                            if (!Attribute.IsDefined(definedMethod, typeof(AnimationEventAttribute), false))
                            {
                                continue;
                            }

                            if (definedMethod.GetCustomAttributes(typeof(AnimationEventAttribute), false)[0] is
                                AnimationEventAttribute { ignoreDerived: true })
                            {
                                ignoreDerived.Add((type, definedMethod.Name));
                            }
                        }

                        return definedMethods;
                    });

            foreach (KeyValuePair<Type, List<MethodInfo>> entry in typesToMethods.ToList())
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

            TypesToMethods = typesToMethods.ToDictionary(kvp => kvp.Key, kvp => (IReadOnlyList<MethodInfo>)kvp.Value);
        }

        public static List<MethodInfo> GetPossibleAnimatorEventsForType(this Type type)
        {
            return type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where
            (
                p =>
                    p.ReturnType == typeof(void) &&
                    (p.GetParameters().Select(q => q.ParameterType).SequenceEqual(new Type[] { }) ||
                     p.GetParameters().Select(q => q.ParameterType)
                         .SequenceEqual(new Type[] { typeof(int) }) ||
                     p.GetParameters().Select(q => q.ParameterType.BaseType)
                         .SequenceEqual(new Type[] { typeof(Enum) }) ||
                     p.GetParameters().Select(q => q.ParameterType)
                         .SequenceEqual(new Type[] { typeof(float) }) ||
                     p.GetParameters().Select(q => q.ParameterType)
                         .SequenceEqual(new Type[] { typeof(string) }) ||
                     p.GetParameters().Select(q => q.ParameterType)
                         .SequenceEqual(new Type[] { typeof(UnityEngine.Object) }))
            ).OrderBy(method => method.Name).ToList();
        }
    }
}
