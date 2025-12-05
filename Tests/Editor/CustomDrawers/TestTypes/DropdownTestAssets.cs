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
        [IntDropdown(5, 10, 15)]
        public int missingValue = 5;

        [IntDropdown(5, 10, 15)]
        public int validValue = 10;
    }

    [Serializable]
    internal sealed class IntDropdownInstanceMethodAsset : ScriptableObject
    {
        public List<int> dynamicValues = new();

        [IntDropdown(nameof(GetDynamicValues))]
        public int selection;

        internal IEnumerable<int> GetDynamicValues()
        {
            return dynamicValues;
        }
    }

    [Serializable]
    internal sealed class IntDropdownNoOptionsAsset : ScriptableObject
    {
        [IntDropdown(
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
#endif
}
