namespace WallstopStudios.UnityHelpers.Editor.Settings
{
    using System;
    using System.Collections.Generic;
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

#if UNITY_HELPERS_PALETTE_DIAGNOSTICS
        // Define UNITY_HELPERS_PALETTE_DIAGNOSTICS to re-enable verbose palette logging during investigations.
        private const bool DiagnosticsEnabled = true;
#else
        private const bool DiagnosticsEnabled = false;
#endif

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
            if (
                !DiagnosticsEnabled
                || !ShouldLog(serializedObject, propertyPath, hadChangesBefore, result)
            )
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
            if (!DiagnosticsEnabled || !paletteChanged || !ShouldLogForTargets(serializedObject))
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

        internal static bool ShouldLogPaletteSort(
            SerializedProperty dictionaryProperty,
            Type keyType
        )
        {
            if (
                !DiagnosticsEnabled
                || dictionaryProperty == null
                || dictionaryProperty.serializedObject == null
                || keyType != typeof(string)
            )
            {
                return false;
            }

            if (!IsPaletteProperty(dictionaryProperty.propertyPath))
            {
                return false;
            }

            return ShouldLogForTargets(dictionaryProperty.serializedObject);
        }

        internal static void ReportDictionarySort(
            SerializedProperty dictionaryProperty,
            IReadOnlyList<string> serializedBefore,
            IReadOnlyList<string> snapshotAfterSort,
            IReadOnlyList<string> serializedAfter
        )
        {
            if (
                !DiagnosticsEnabled
                || dictionaryProperty == null
                || !IsPaletteProperty(dictionaryProperty.propertyPath)
            )
            {
                return;
            }

            SerializedObject serializedObject = dictionaryProperty.serializedObject;
            if (!ShouldLogForTargets(serializedObject))
            {
                return;
            }

            string beforeSequence = FormatKeySequence(serializedBefore);
            string snapshotSequence = FormatKeySequence(snapshotAfterSort);
            string afterSequence = FormatKeySequence(serializedAfter);
            string message =
                $"{LogPrefix} Sort property={dictionaryProperty.propertyPath} before=[{beforeSequence}] snapshot=[{snapshotSequence}] after=[{afterSequence}]";

            Debug.Log(message, serializedObject?.targetObject);
        }

        private static bool ShouldLog(
            SerializedObject serializedObject,
            string propertyPath,
            bool hadChangesBefore,
            DrawerApplyResult result
        )
        {
            if (
                !DiagnosticsEnabled
                || !ShouldLogForTargets(serializedObject)
                || !IsPaletteProperty(propertyPath)
            )
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
            if (!DiagnosticsEnabled || serializedObject == null)
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

        private static string FormatKeySequence(IReadOnlyList<string> keys)
        {
            if (keys == null || keys.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder builder = new();
            for (int index = 0; index < keys.Count; index++)
            {
                if (index > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(keys[index] ?? "<null>");
            }

            return builder.ToString();
        }
    }
}
