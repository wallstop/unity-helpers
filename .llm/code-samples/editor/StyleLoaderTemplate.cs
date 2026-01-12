// MIT License - Copyright (c) 2026 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

// USS StyleSheet loader template - lazy initialization pattern

namespace WallstopStudios.UnityHelpers.Editor.Styles
{
#if UNITY_EDITOR
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;
    using WallstopStudios.UnityHelpers.Core.Helper;

    public static class MyStyleLoader
    {
        private const string StylesRelativePath = "Editor/Styles/MyComponent/";
        private const string StylesFileName = "MyStyles.uss";

        private static StyleSheet _stylesStyleSheet;
        private static bool _initialized;

        public static StyleSheet Styles
        {
            get
            {
                EnsureInitialized();
                return _stylesStyleSheet;
            }
        }

        public static bool IsProSkin => EditorGUIUtility.isProSkin;

        public static void ApplyStyles(VisualElement element)
        {
            if (element == null)
            {
                return;
            }

            EnsureInitialized();

            if (_stylesStyleSheet != null)
            {
                element.styleSheets.Add(_stylesStyleSheet);
            }
        }

        private static void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;

            string stylesPath = DirectoryHelper.GetPackagePath(StylesRelativePath + StylesFileName);
            _stylesStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(stylesPath);
        }
    }
#endif
}
