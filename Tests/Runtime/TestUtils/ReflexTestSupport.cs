#if REFLEX_PRESENT
namespace WallstopStudios.UnityHelpers.Tests.TestUtils
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using UnityEngine;

    public static class ReflexTestSupport
    {
        private const string ReflexSettingsTypeName = "Reflex.Configuration.ReflexSettings";
        private const string InstanceFieldName = "_instance";
        private const string LogLevelBackingFieldName = "<LogLevel>k__BackingField";
        private const string ProjectScopesBackingFieldName = "<ProjectScopes>k__BackingField";
        private const string LogLevelTypeName = "Reflex.Logging.LogLevel";
        private const string ProjectScopeTypeName = "Reflex.Core.ProjectScope";

        private static bool _initialized;

        public static void EnsureReflexSettings()
        {
            if (_initialized)
            {
                return;
            }

            Type settingsType = FindType(ReflexSettingsTypeName);
            if (settingsType == null)
            {
                return;
            }

            FieldInfo instanceField = settingsType.GetField(
                InstanceFieldName,
                BindingFlags.NonPublic | BindingFlags.Static
            );
            if (instanceField == null)
            {
                return;
            }

            object currentInstance = instanceField.GetValue(null);
            if (currentInstance == null)
            {
                ScriptableObject settings = ScriptableObject.CreateInstance(settingsType);
                SetInstanceField(
                    settingsType,
                    settings,
                    LogLevelBackingFieldName,
                    GetLogLevelInfo()
                );
                SetInstanceField(
                    settingsType,
                    settings,
                    ProjectScopesBackingFieldName,
                    CreateEmptyProjectScopesList()
                );
                instanceField.SetValue(null, settings);
            }

            _initialized = true;
        }

        private static void SetInstanceField(
            Type declaringType,
            object instance,
            string fieldName,
            object value
        )
        {
            FieldInfo field = declaringType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            field?.SetValue(instance, value);
        }

        private static object GetLogLevelInfo()
        {
            Type logLevelType = FindType(LogLevelTypeName);
            if (logLevelType == null)
            {
                return null;
            }

            return Enum.Parse(logLevelType, "Info", ignoreCase: true);
        }

        private static object CreateEmptyProjectScopesList()
        {
            Type projectScopeType = FindType(ProjectScopeTypeName);
            if (projectScopeType == null)
            {
                return null;
            }

            Type listType = typeof(List<>).MakeGenericType(projectScopeType);
            return Activator.CreateInstance(listType);
        }

        private static Type FindType(string fullName)
        {
            return AppDomain
                .CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(fullName))
                .FirstOrDefault(type => type != null);
        }
    }
}
#endif
