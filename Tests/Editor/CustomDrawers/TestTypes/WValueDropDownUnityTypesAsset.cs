// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test asset for WValueDropDown with Unity built-in types (Vector2, Vector3, Color, etc.).
    /// </summary>
    [Serializable]
    internal sealed class WValueDropDownUnityTypesAsset : ScriptableObject
    {
        public List<Vector2> vector2Options = new List<Vector2>();

        [WValueDropDown(nameof(GetVector2Options), typeof(Vector2))]
        public Vector2 selectedVector2;

        public List<Vector3> vector3Options = new List<Vector3>();

        [WValueDropDown(nameof(GetVector3Options), typeof(Vector3))]
        public Vector3 selectedVector3;

        public List<Color> colorOptions = new List<Color>();

        [WValueDropDown(nameof(GetColorOptions), typeof(Color))]
        public Color selectedColor;

        public List<Rect> rectOptions = new List<Rect>();

        [WValueDropDown(nameof(GetRectOptions), typeof(Rect))]
        public Rect selectedRect;

        public List<Vector2Int> vector2IntOptions = new List<Vector2Int>();

        [WValueDropDown(nameof(GetVector2IntOptions), typeof(Vector2Int))]
        public Vector2Int selectedVector2Int;

        public List<Bounds> boundsOptions = new List<Bounds>();

        [WValueDropDown(nameof(GetBoundsOptions), typeof(Bounds))]
        public Bounds selectedBounds;

        public IEnumerable<Vector2> GetVector2Options()
        {
            return vector2Options;
        }

        public IEnumerable<Vector3> GetVector3Options()
        {
            return vector3Options;
        }

        public IEnumerable<Color> GetColorOptions()
        {
            return colorOptions;
        }

        public IEnumerable<Rect> GetRectOptions()
        {
            return rectOptions;
        }

        public IEnumerable<Vector2Int> GetVector2IntOptions()
        {
            return vector2IntOptions;
        }

        public IEnumerable<Bounds> GetBoundsOptions()
        {
            return boundsOptions;
        }
    }
#endif
}
