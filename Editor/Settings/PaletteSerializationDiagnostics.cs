namespace WallstopStudios.UnityHelpers.Editor.Settings
{
    using System;
    using System.Text;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    /// <summary>
    /// Centralizes palette serialization logging so drawers and settings UI can emit consistent diagnostics.
    /// </summary>
    internal static class PaletteSerializationDiagnostics
    {
        private const string LogPrefix = "[UnityHelpers][PaletteSerialization]";

        private static readonly string[] PalettePropertyRoots =
        {
            UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors,
            UnityHelpersSettings.SerializedPropertyNames.WGroupCustomColors,
            UnityHelpersSettings.SerializedPropertyNames.WEnumToggleButtonsCustomColors,
        };

        internal enum DrawerApplyResult
        {
            UndoPathSucceeded,
            FallbackPathSucceeded,
            Failed,
        }

        internal static bool IsPaletteProperty(string propertyPath)
        {
            if (string.IsNullOrEmpty(propertyPath))
            {
                return false;
            }

            for (int index = 0; index < PalettePropertyRoots.Length; index++)
            {
                if (propertyPath.StartsWith(PalettePropertyRoots[index], StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        internal static void ReportDrawerApplyResult(
            SerializedObject serializedObject,
            string propertyPath,
            string operation,
            DrawerApplyResult result,
            bool hadChangesBefore,
            bool hasChangesAfter
        )
        {
            if (!ShouldLog(serializedObject, propertyPath, hadChangesBefore, result))
            {
                return;
            }

            StringBuilder builder = new();
            builder
                .Append(LogPrefix)
                .Append(' ')
                .Append(operation ?? "DrawerApply")
                .Append(" property=")
                .Append(propertyPath ?? "<null>")
                .Append(" result=")
                .Append(result)
                .Append(" hadChangesBefore=")
                .Append(hadChangesBefore)
                .Append(" remainingDirty=")
                .Append(hasChangesAfter)
                .Append(" targets=")
                .Append(serializedObject.targetObjects?.Length ?? 0);

            Object context = serializedObject.targetObject;
            if (result == DrawerApplyResult.Failed)
            {
                Debug.LogWarning(builder.ToString(), context);
                return;
            }

            if (result == DrawerApplyResult.FallbackPathSucceeded)
            {
                Debug.Log(builder.ToString(), context);
                return;
            }

            Debug.Log(builder.ToString(), context);
        }

        internal static void ReportInspectorApplyResult(
            SerializedObject serializedObject,
            bool paletteChanged,
            bool dataChanged,
            bool guiChanged,
            bool applyResult
        )
        {
            if (!paletteChanged || !ShouldLogForTargets(serializedObject))
            {
                return;
            }

            bool stillDirty = serializedObject != null && serializedObject.hasModifiedProperties;
            string message =
                $"{LogPrefix} InspectorApply result={(applyResult ? "Success" : "Failed")} dataChanged={dataChanged} guiChanged={guiChanged} remainingDirty={stillDirty}";

            Object context = serializedObject?.targetObject;
            if (applyResult)
            {
                Debug.Log(message, context);
            }
            else
            {
                Debug.LogWarning(message, context);
            }
        }

        private static bool ShouldLog(
            SerializedObject serializedObject,
            string propertyPath,
            bool hadChangesBefore,
            DrawerApplyResult result
        )
        {
            if (!ShouldLogForTargets(serializedObject) || !IsPaletteProperty(propertyPath))
            {
                return false;
            }

            if (!hadChangesBefore && result != DrawerApplyResult.Failed)
            {
                return false;
            }

            return true;
        }

        private static bool ShouldLogForTargets(SerializedObject serializedObject)
        {
            if (serializedObject == null)
            {
                return false;
            }

            if (serializedObject.targetObject is UnityHelpersSettings)
            {
                return true;
            }

            Object[] targets = serializedObject.targetObjects;
            if (targets == null || targets.Length == 0)
            {
                return false;
            }

            for (int index = 0; index < targets.Length; index++)
            {
                if (targets[index] is UnityHelpersSettings)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
