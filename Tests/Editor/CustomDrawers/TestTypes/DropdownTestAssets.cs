namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    // ============================================
    // StringInList Test Assets
    // ============================================

    [Serializable]
    internal sealed class StringInListNoOptionsAsset : ScriptableObject
    {
        [StringInList]
        public string unspecified = string.Empty;
    }

    [Serializable]
    internal sealed class StringInListStringOptionsAsset : ScriptableObject
    {
        [StringInList("Idle", "Run", "Jump")]
        public string state = "Idle";
    }

    [Serializable]
    internal sealed class StringInListIntegerOptionsAsset : ScriptableObject
    {
        [StringInList("Low", "Medium", "High")]
        public int selection;
    }

    [Serializable]
    internal sealed class StringInListInstanceMethodAsset : ScriptableObject
    {
        public List<string> dynamicValues = new();

        [StringInList(nameof(GetDynamicValues))]
        public string selection;

        internal IEnumerable<string> GetDynamicValues()
        {
            return dynamicValues;
        }
    }

    internal static class StringOptionsProvider
    {
        internal static string[] GetOptions()
        {
            return new[] { "Static1", "Static2", "Static3" };
        }
    }

    // ============================================
    // WValueDropDown Test Assets
    // ============================================

    [Serializable]
    internal sealed class WValueDropDownFloatAsset : ScriptableObject
    {
        [WValueDropDown(typeof(WValueDropDownSource), nameof(WValueDropDownSource.GetFloatValues))]
        public float selection = 1f;

        [WValueDropDown(typeof(WValueDropDownSource), nameof(WValueDropDownSource.GetDoubleValues))]
        public double preciseSelection = 2d;
    }

    [Serializable]
    internal sealed class WValueDropDownNoOptionsAsset : ScriptableObject
    {
        [WValueDropDown(
            typeof(WValueDropDownEmptySource),
            nameof(WValueDropDownEmptySource.GetEmptyOptions)
        )]
        public int unspecified;
    }

    [Serializable]
    internal sealed class WValueDropDownIntOptionsAsset : ScriptableObject
    {
        [WValueDropDown(10, 20, 30)]
        public int selection = 10;
    }

    [Serializable]
    internal sealed class WValueDropDownStringOptionsAsset : ScriptableObject
    {
        [WValueDropDown("Alpha", "Beta", "Gamma")]
        public string selection = "Alpha";
    }

    [Serializable]
    internal sealed class WValueDropDownInstanceMethodAsset : ScriptableObject
    {
        public List<int> dynamicValues = new();

        [WValueDropDown(nameof(GetDynamicValues), typeof(int))]
        public int selection;

        internal IEnumerable<int> GetDynamicValues()
        {
            return dynamicValues;
        }
    }

    internal static class WValueDropDownSource
    {
        internal static float[] GetFloatValues()
        {
            return new[] { 1f, 2.5f, 5f };
        }

        internal static double[] GetDoubleValues()
        {
            return new[] { 2d, 4.5d, 5.25d };
        }
    }

    internal static class WValueDropDownEmptySource
    {
        internal static int[] GetEmptyOptions()
        {
            return Array.Empty<int>();
        }
    }

    // ============================================
    // IntDropdown Test Assets
    // ============================================

    [Serializable]
    internal sealed class IntDropdownTestAsset : ScriptableObject
    {
        [IntDropDown(5, 10, 15)]
        public int missingValue = 5;

        [IntDropDown(5, 10, 15)]
        public int validValue = 10;
    }

    [Serializable]
    internal sealed class IntDropdownInstanceMethodAsset : ScriptableObject
    {
        public List<int> dynamicValues = new();

        [IntDropDown(nameof(GetDynamicValues))]
        public int selection;

        internal IEnumerable<int> GetDynamicValues()
        {
            return dynamicValues;
        }
    }

    [Serializable]
    internal sealed class IntDropdownNoOptionsAsset : ScriptableObject
    {
        [IntDropDown(
            typeof(IntDropdownEmptySource),
            nameof(IntDropdownEmptySource.GetEmptyOptions)
        )]
        public int unspecified;
    }

    internal static class IntDropdownSource
    {
        internal static int[] GetStaticOptions()
        {
            return new[] { 100, 200, 300 };
        }
    }

    internal static class IntDropdownEmptySource
    {
        internal static int[] GetEmptyOptions()
        {
            return Array.Empty<int>();
        }
    }

    internal static class IntDropdownLargeSource
    {
        internal static int[] GetLargeOptions()
        {
            // Returns more than the default page size (25) to trigger popup path
            int[] options = new int[50];
            for (int i = 0; i < 50; i++)
            {
                options[i] = (i + 1) * 10;
            }
            return options;
        }
    }

    [Serializable]
    internal sealed class IntDropdownLargeOptionsAsset : ScriptableObject
    {
        [IntDropDown(
            typeof(IntDropdownLargeSource),
            nameof(IntDropdownLargeSource.GetLargeOptions)
        )]
        public int selection = 100;
    }

    // ============================================
    // Type Mismatch Test Assets
    // ============================================

    [Serializable]
    internal sealed class IntDropdownTypeMismatchAsset : ScriptableObject
    {
        [IntDropDown(1, 2, 3)]
        public string stringFieldWithIntDropdown = string.Empty;

        [IntDropDown(1, 2, 3)]
        public float floatFieldWithIntDropdown = 0f;

        [IntDropDown(1, 2, 3)]
        public bool boolFieldWithIntDropdown = false;
    }

    [Serializable]
    internal sealed class StringInListTypeMismatchAsset : ScriptableObject
    {
        [StringInList("A", "B", "C")]
        public float floatFieldWithStringInList = 0f;

        [StringInList("A", "B", "C")]
        public bool boolFieldWithStringInList = false;

        [StringInList("A", "B", "C")]
        public Vector3 vector3FieldWithStringInList = Vector3.zero;
    }

    [Serializable]
    internal sealed class WValueDropDownTypeMismatchAsset : ScriptableObject
    {
        [WValueDropDown(1, 2, 3)]
        public Vector2 vector2FieldWithDropdown = Vector2.zero;

        [WValueDropDown("A", "B", "C")]
        public bool boolFieldWithDropdown = false;

        [WValueDropDown(1.5f, 2.5f, 3.5f)]
        public Color colorFieldWithDropdown = Color.white;
    }
#endif
}
