// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
#if UNITY_EDITOR
    using System;
    using UnityEngine;

    /// <summary>
    /// Static source for Unity type dropdown tests.
    /// </summary>
    internal static class WValueDropDownUnityTypesSource
    {
        private static Vector2[] s_vector2Options;
        private static LayerMask[] s_layerMasks;

        internal static Vector2[] GetStaticVector2Options()
        {
            return s_vector2Options ?? Array.Empty<Vector2>();
        }

        internal static LayerMask[] GetLayerMasks()
        {
            return s_layerMasks ?? Array.Empty<LayerMask>();
        }

        internal static void SetVector2Options(Vector2[] options)
        {
            s_vector2Options = options;
        }

        internal static void SetLayerMasks(LayerMask[] masks)
        {
            s_layerMasks = masks;
        }

        internal static void Clear()
        {
            s_vector2Options = null;
            s_layerMasks = null;
        }
    }
#endif
}
