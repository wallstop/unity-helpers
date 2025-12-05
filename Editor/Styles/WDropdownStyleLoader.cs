namespace WallstopStudios.UnityHelpers.Editor.Styles
{
#if UNITY_EDITOR
    using System;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;

    /// <summary>
    /// Provides lazy loading and caching of USS stylesheets for dropdown components.
    /// Stylesheets are loaded on first access and cached for the editor session.
    /// </summary>
    public static class WDropdownStyleLoader
    {
        private const string StylesPath =
            "Packages/com.wallstop-studios.unity-helpers/Editor/Styles/Dropdowns/";
        private const string VariablesFileName = "WDropdownVariables.uss";
        private const string StylesFileName = "WDropdownStyles.uss";
        private const string LightThemeFileName = "WDropdownLight.uss";

        private static StyleSheet _variablesStyleSheet;
        private static StyleSheet _stylesStyleSheet;
        private static StyleSheet _lightThemeStyleSheet;
        private static bool _initialized;

        /// <summary>
        /// Gets the variables stylesheet containing CSS custom properties.
        /// </summary>
        public static StyleSheet Variables
        {
            get
            {
                EnsureInitialized();
                return _variablesStyleSheet;
            }
        }

        /// <summary>
        /// Gets the main styles stylesheet.
        /// </summary>
        public static StyleSheet Styles
        {
            get
            {
                EnsureInitialized();
                return _stylesStyleSheet;
            }
        }

        /// <summary>
        /// Gets the light theme override stylesheet.
        /// </summary>
        public static StyleSheet LightTheme
        {
            get
            {
                EnsureInitialized();
                return _lightThemeStyleSheet;
            }
        }

        /// <summary>
        /// Returns true if the current editor is using the Pro (dark) skin.
        /// </summary>
        public static bool IsProSkin => EditorGUIUtility.isProSkin;

        /// <summary>
        /// Applies all dropdown stylesheets to a visual element in the correct order.
        /// Automatically selects the appropriate theme based on the current editor skin.
        /// </summary>
        /// <param name="element">The element to apply styles to.</param>
        public static void ApplyStyles(VisualElement element)
        {
            if (element == null)
            {
                return;
            }

            EnsureInitialized();

            // Apply base variables first
            if (_variablesStyleSheet != null && !element.styleSheets.Contains(_variablesStyleSheet))
            {
                element.styleSheets.Add(_variablesStyleSheet);
            }

            // Apply main styles
            if (_stylesStyleSheet != null && !element.styleSheets.Contains(_stylesStyleSheet))
            {
                element.styleSheets.Add(_stylesStyleSheet);
            }

            // Apply theme override if using light theme
            if (
                !IsProSkin
                && _lightThemeStyleSheet != null
                && !element.styleSheets.Contains(_lightThemeStyleSheet)
            )
            {
                element.styleSheets.Add(_lightThemeStyleSheet);
            }
        }

        /// <summary>
        /// Removes all dropdown stylesheets from a visual element.
        /// </summary>
        /// <param name="element">The element to remove styles from.</param>
        public static void RemoveStyles(VisualElement element)
        {
            if (element == null)
            {
                return;
            }

            if (_variablesStyleSheet != null && element.styleSheets.Contains(_variablesStyleSheet))
            {
                element.styleSheets.Remove(_variablesStyleSheet);
            }

            if (_stylesStyleSheet != null && element.styleSheets.Contains(_stylesStyleSheet))
            {
                element.styleSheets.Remove(_stylesStyleSheet);
            }

            if (
                _lightThemeStyleSheet != null
                && element.styleSheets.Contains(_lightThemeStyleSheet)
            )
            {
                element.styleSheets.Remove(_lightThemeStyleSheet);
            }
        }

        /// <summary>
        /// Forces a reload of all stylesheets. Call this if styles are modified at runtime.
        /// </summary>
        public static void ReloadStyles()
        {
            _initialized = false;
            _variablesStyleSheet = null;
            _stylesStyleSheet = null;
            _lightThemeStyleSheet = null;
            EnsureInitialized();
        }

        private static void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _variablesStyleSheet = LoadStyleSheet(VariablesFileName);
            _stylesStyleSheet = LoadStyleSheet(StylesFileName);
            _lightThemeStyleSheet = LoadStyleSheet(LightThemeFileName);

            _initialized = true;
        }

        private static StyleSheet LoadStyleSheet(string fileName)
        {
            string path = StylesPath + fileName;
            StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);

            if (styleSheet == null)
            {
                // Try alternative path formats
                string altPath =
                    "Assets/Plugins/WallstopStudios/UnityHelpers/Editor/Styles/Dropdowns/"
                    + fileName;
                styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(altPath);
            }

            if (styleSheet == null)
            {
                Debug.LogWarning($"[WDropdownStyleLoader] Could not load stylesheet: {path}");
            }

            return styleSheet;
        }

        /// <summary>
        /// USS class names for dropdown components.
        /// Use these constants when adding/removing classes from visual elements.
        /// </summary>
        public static class ClassNames
        {
            // Container
            public const string Popup = "w-dropdown-popup";

            // Search
            public const string SearchContainer = "w-dropdown-search-container";
            public const string SearchWrapper = "w-dropdown-search-wrapper";
            public const string Search = "w-dropdown-search";
            public const string SearchIcon = "w-dropdown-search-icon";
            public const string ClearButton = "w-dropdown-clear-button";

            // Options
            public const string OptionsContainer = "w-dropdown-options-container";
            public const string Option = "w-dropdown-option";
            public const string OptionSelected = "w-dropdown-option--selected";
            public const string OptionHover = "w-dropdown-option--hover";
            public const string OptionFocused = "w-dropdown-option--focused";
            public const string OptionLabel = "w-dropdown-option-label";
            public const string OptionIndicator = "w-dropdown-option-indicator";

            // Pagination
            public const string Pagination = "w-dropdown-pagination";
            public const string PaginationButton = "w-dropdown-pagination-button";
            public const string PaginationLabel = "w-dropdown-pagination-label";

            // Misc
            public const string Suggestion = "w-dropdown-suggestion";
            public const string NoResults = "w-dropdown-no-results";
            public const string Button = "w-dropdown-button";
            public const string ButtonLabel = "w-dropdown-button-label";
            public const string ButtonArrow = "w-dropdown-button-arrow";

            // Animation
            public const string FadeIn = "w-dropdown-fade-in";
            public const string FadeOut = "w-dropdown-fade-out";
        }
    }
#endif
}
