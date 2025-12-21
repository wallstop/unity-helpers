#if UNITY_EDITOR
#pragma warning disable CS0414 // Field is assigned but its value is never used
namespace WallstopStudios.UnityHelpers.Tests.WGroup
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for validating themed backgrounds on lists/arrays with different colorKeys.
    /// This helps verify that WGroup color theming applies correctly to built-in Unity drawers.
    /// </summary>
    internal sealed class ThemedListsTarget : ScriptableObject
    {
        [WGroup("LightGroup", "Light Theme Group", colorKey: "Default-Light")]
        public int lightGroupInt;

        public bool lightGroupBool;

        public List<string> lightGroupList = new();

        [WGroupEnd("LightGroup")]
        public string[] lightGroupArray = Array.Empty<string>();

        [WGroup("DarkGroup", "Dark Theme Group", colorKey: "Default-Dark")]
        public int darkGroupInt;

        public bool darkGroupBool;

        public List<string> darkGroupList = new();

        [WGroupEnd("DarkGroup")]
        public string[] darkGroupArray = Array.Empty<string>();

        [WGroup("CustomGroup", "Custom Colored Group", colorKey: "Neon")]
        public int customGroupInt;

        public bool customGroupBool;

        public List<int> customGroupList = new();

        [WGroupEnd("CustomGroup")]
        public float[] customGroupArray = Array.Empty<float>();
    }
}
#pragma warning restore CS0414 // Field is assigned but its value is never used
#endif
