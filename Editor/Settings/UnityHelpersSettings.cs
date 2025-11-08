namespace WallstopStudios.UnityHelpers.Editor.Settings
{
#if UNITY_EDITOR
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Project-wide configuration surface for Unity Helpers editor tooling.
    /// </summary>
    /// <remarks>
    /// Currently exposes pagination defaults for <see cref="CustomDrawers.StringInListDrawer"/>.
    /// </remarks>
    [FilePath(
        "ProjectSettings/UnityHelpersSettings.asset",
        FilePathAttribute.Location.ProjectFolder
    )]
    public sealed class UnityHelpersSettings : ScriptableSingleton<UnityHelpersSettings>
    {
        public const int MinPageSize = 5;
        public const int MaxPageSize = 500;
        public const int DefaultStringInListPageSize = 25;

        [SerializeField]
        [Tooltip("Maximum number of entries shown per page for StringInList dropdowns.")]
        private int stringInListPageSize = DefaultStringInListPageSize;

        /// <summary>
        /// Retrieves the effective page size for StringInList drawers, clamped to safe bounds.
        /// </summary>
        public int StringInListPageSize
        {
            get => Mathf.Clamp(stringInListPageSize, MinPageSize, MaxPageSize);
            set
            {
                int clamped = Mathf.Clamp(value, MinPageSize, MaxPageSize);
                if (clamped == stringInListPageSize)
                {
                    return;
                }

                stringInListPageSize = clamped;
                SaveSettings();
            }
        }

        /// <summary>
        /// Returns the configured page size, falling back to defaults if unset.
        /// </summary>
        public static int GetStringInListPageLimit()
        {
            return instance.StringInListPageSize;
        }

        /// <summary>
        /// Ensures persisted data stays within valid range.
        /// </summary>
        private void OnEnable()
        {
            stringInListPageSize = Mathf.Clamp(
                stringInListPageSize <= 0 ? DefaultStringInListPageSize : stringInListPageSize,
                MinPageSize,
                MaxPageSize
            );
        }

        /// <summary>
        /// Persists any modifications to disk.
        /// </summary>
        public void SaveSettings()
        {
            Save(true);
        }

        [SettingsProvider]
        private static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider(
                "Project/Wallstop Studios/Unity Helpers",
                SettingsScope.Project
            )
            {
                label = "Unity Helpers",
                guiHandler = _ =>
                {
                    UnityHelpersSettings settings = instance;
                    SerializedObject serializedSettings = new SerializedObject(settings);
                    SerializedProperty pageSizeProperty = serializedSettings.FindProperty(
                        "stringInListPageSize"
                    );

                    serializedSettings.Update();

                    EditorGUI.BeginChangeCheck();

                    EditorGUILayout.IntSlider(
                        pageSizeProperty,
                        MinPageSize,
                        MaxPageSize,
                        new GUIContent(
                            "StringInList Page Size",
                            "Number of options displayed per page in StringInList dropdowns."
                        )
                    );

                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedSettings.ApplyModifiedPropertiesWithoutUndo();
                        settings.SaveSettings();
                    }
                },
                keywords = new[] { "StringInList", "Pagination", "UnityHelpers" },
            };
        }
    }
#endif
}
