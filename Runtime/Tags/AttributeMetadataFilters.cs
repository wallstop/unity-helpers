namespace WallstopStudios.UnityHelpers.Tags
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Core.Helper;

    internal static class AttributeMetadataFilters
    {
        internal static bool ShouldSerialize(Type type)
        {
            if (type == null || type.IsAbstract || type.IsGenericTypeDefinition)
            {
                return false;
            }

            if (IsExplicitInclude(type))
            {
                return true;
            }

            return !TestAssemblyHelper.IsTestType(type);
        }

        internal static bool HasTestAssembliesLoaded()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (TestAssemblyHelper.IsTestAssembly(assembly))
                {
                    return true;
                }
            }

            return false;
        }

        internal static string[] MergeWithExcludedAttributeNames(string[] cachedNames)
        {
            string[] baseNames = cachedNames ?? Array.Empty<string>();

            if (!HasTestAssembliesLoaded())
            {
                return baseNames;
            }

            HashSet<string> names = new(baseNames, StringComparer.Ordinal);
            bool added = false;

            foreach (Type type in EnumerateLoadedTypes())
            {
                if (type == null || type.IsAbstract)
                {
                    continue;
                }

                if (!typeof(AttributesComponent).IsAssignableFrom(type))
                {
                    continue;
                }

                if (ShouldSerialize(type))
                {
                    continue;
                }

                foreach (
                    FieldInfo field in type.GetFields(
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                    )
                )
                {
                    if (field.FieldType != typeof(Attribute))
                    {
                        continue;
                    }

                    if (names.Add(field.Name))
                    {
                        added = true;
                    }
                }
            }

            if (!added)
            {
                return baseNames;
            }

            string[] result = new string[names.Count];
            names.CopyTo(result);
            Array.Sort(result, StringComparer.Ordinal);
            return result;
        }

        private static bool IsExplicitInclude(Type type)
        {
            return type.IsDefined(
                typeof(AlwaysIncludeInAttributeMetadataCacheAttribute),
                inherit: true
            );
        }

        private static IEnumerable<Type> EnumerateLoadedTypes()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly == null || assembly.IsDynamic)
                {
                    continue;
                }

                Type[] types;

                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types;
                }
                catch
                {
                    continue;
                }

                if (types == null)
                {
                    continue;
                }

                foreach (Type type in types)
                {
                    if (type != null)
                    {
                        yield return type;
                    }
                }
            }
        }
    }
}
