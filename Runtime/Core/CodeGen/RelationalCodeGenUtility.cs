namespace WallstopStudios.UnityHelpers.Core.CodeGen
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Resolves the effective code-generation preference for relational component attributes.
    /// </summary>
    public static class RelationalCodeGenUtility
    {
        private const string SettingsResourcePath = "RelationalCodeGenSettings";

        private static RelationalCodeGenSettings cachedSettings;
        private static bool attemptedLoad;

        /// <summary>
        /// Returns the effective boolean value indicating whether code generation should be used.
        /// </summary>
        public static bool ShouldUseCodeGen(
            BaseRelationalComponentAttribute attribute,
            RelationalAttributeKind attributeKind
        )
        {
            RelationalCodeGenPreference preference = GetEffectivePreference(
                attribute.CodeGenPreference,
                attributeKind
            );
            return preference == RelationalCodeGenPreference.Enabled;
        }

        /// <summary>
        /// Retrieves the effective preference for the supplied attribute kind.
        /// </summary>
        public static RelationalCodeGenPreference GetEffectivePreference(
            RelationalCodeGenPreference attributePreference,
            RelationalAttributeKind attributeKind
        )
        {
            if (attributePreference == RelationalCodeGenPreference.Enabled)
            {
                return RelationalCodeGenPreference.Enabled;
            }

            if (attributePreference == RelationalCodeGenPreference.Disabled)
            {
                return RelationalCodeGenPreference.Disabled;
            }

            RelationalCodeGenSettings settings = LoadSettings();
            if (settings == null)
            {
                return RelationalCodeGenPreference.Disabled;
            }

            switch (attributeKind)
            {
                case RelationalAttributeKind.Sibling:
                    return settings.SiblingDefault;
                case RelationalAttributeKind.Parent:
                    return settings.ParentDefault;
                case RelationalAttributeKind.Child:
                    return settings.ChildDefault;
                default:
                    return RelationalCodeGenPreference.Disabled;
            }
        }

        /// <summary>
        /// Clears the cached settings instance (e.g., after editor modifications).
        /// </summary>
        public static void ClearCachedSettings()
        {
            cachedSettings = null;
            attemptedLoad = false;
        }

        private static RelationalCodeGenSettings LoadSettings()
        {
            if (!attemptedLoad)
            {
                cachedSettings = Resources.Load<RelationalCodeGenSettings>(SettingsResourcePath);
                attemptedLoad = true;
            }

            return cachedSettings;
        }
    }
}
