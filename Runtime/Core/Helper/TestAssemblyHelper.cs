namespace WallstopStudios.UnityHelpers.Core.Helper
{
    using System;
    using System.Reflection;

    internal static class TestAssemblyHelper
    {
        private static readonly StringComparison Comparison = StringComparison.OrdinalIgnoreCase;

        internal static bool IsTestAssembly(Assembly assembly)
        {
            if (assembly == null || assembly.IsDynamic)
            {
                return false;
            }

            string name = assembly.GetName().Name;
            if (!string.IsNullOrEmpty(name) && ContainsTestMarker(name))
            {
                return true;
            }

            if (HasUnityTestAttribute(assembly))
            {
                return true;
            }

            return false;
        }

        internal static bool IsTestType(Type type)
        {
            if (type == null)
            {
                return false;
            }

            if (IsTestAssembly(type.Assembly))
            {
                return true;
            }

            string ns = type.Namespace;
            if (!string.IsNullOrEmpty(ns) && ContainsTestMarker(ns))
            {
                return true;
            }

            return false;
        }

        internal static bool ContainsTestMarker(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            if (
                value.IndexOf(".Tests", Comparison) >= 0
                || value.IndexOf(".Test", Comparison) >= 0
                || value.IndexOf("Tests.", Comparison) >= 0
                || value.IndexOf("Tests_", Comparison) >= 0
                || value.IndexOf("Test.", Comparison) >= 0
                || value.IndexOf("Test_", Comparison) >= 0
            )
            {
                return true;
            }

            if (value.EndsWith("Tests", Comparison) || value.EndsWith("Test", Comparison))
            {
                return true;
            }

            if (value.StartsWith("Tests", Comparison) || value.StartsWith("Test", Comparison))
            {
                return true;
            }

            return false;
        }

        private static bool HasUnityTestAttribute(Assembly assembly)
        {
            try
            {
                foreach (object attribute in assembly.GetCustomAttributes(inherit: false))
                {
                    Type attributeType = attribute?.GetType();
                    if (attributeType == null)
                    {
                        continue;
                    }

                    string fullName = attributeType.FullName;
                    if (
                        string.Equals(
                            fullName,
                            "UnityEngine.TestTools.UnityTestAssemblyAttribute",
                            Comparison
                        )
                        || string.Equals(
                            fullName,
                            "UnityEditor.TestTools.TestRunner.UnityTestAssemblyAttribute",
                            Comparison
                        )
                    )
                    {
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }
    }
}
