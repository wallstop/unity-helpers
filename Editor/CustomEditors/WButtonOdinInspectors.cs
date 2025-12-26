namespace WallstopStudios.UnityHelpers.Editor.CustomEditors
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Sirenix.OdinInspector;
    using UnityEditor;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers;
    using WallstopStudios.UnityHelpers.Editor.Extensions;
    using WallstopStudios.UnityHelpers.Editor.Internal;
    using WallstopStudios.UnityHelpers.Editor.Settings;
    using WallstopStudios.UnityHelpers.Editor.Utils.WButton;
    using WallstopStudios.UnityHelpers.Editor.Utils.WGroup;
    using WallstopStudios.UnityHelpers.Utils;

    /// <summary>
    /// Custom editor for Odin Inspector's SerializedMonoBehaviour that adds WButton and WGroup support.
    /// This editor takes precedence over Odin's default editor when ODIN_INSPECTOR is defined.
    /// </summary>
    [CustomEditor(typeof(SerializedMonoBehaviour), true)]
    [CanEditMultipleObjects]
    public sealed class WButtonOdinMonoBehaviourInspector : Editor
    {
        private const string ScriptPropertyPath = "m_Script";

        private static readonly WallstopGenericPool<
            Dictionary<string, SerializedProperty>
        > PropertyLookupPool = new(
            () => new Dictionary<string, SerializedProperty>(16, StringComparer.Ordinal),
            onRelease: d => d.Clear()
        );

        private readonly Dictionary<WButtonGroupKey, WButtonPaginationState> _paginationStates =
            new();
        private readonly Dictionary<WButtonGroupKey, bool> _foldoutStates = new();
        private readonly Dictionary<int, bool> _groupFoldoutStates = new();

        public override void OnInspectorGUI()
        {
            using PooledResource<List<WButtonMethodContext>> triggeredContextsLease =
                Buffers<WButtonMethodContext>.GetList(
                    4,
                    out List<WButtonMethodContext> triggeredContexts
                );

            serializedObject.UpdateIfRequiredOrScript();

            using PooledResource<Dictionary<string, SerializedProperty>> propertyLookupLease =
                PropertyLookupPool.Get(out Dictionary<string, SerializedProperty> propertyLookup);

            SerializedProperty scriptProperty = BuildPropertyLookup(
                serializedObject,
                propertyLookup
            );

            bool drawScriptField = scriptProperty != null && !InlineInspectorContext.IsActive;
            if (drawScriptField)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(scriptProperty, true);
                }
                EditorGUILayout.Space();
            }

            UnityHelpersSettings.WButtonActionsPlacement placement =
                UnityHelpersSettings.GetWButtonActionsPlacement();
            UnityHelpersSettings.WButtonFoldoutBehavior foldoutBehavior =
                UnityHelpersSettings.GetWButtonFoldoutBehavior();
            bool globalPlacementIsTop =
                placement == UnityHelpersSettings.WButtonActionsPlacement.Top;

            if (
                WButtonGUI.DrawButtons(
                    this,
                    WButtonPlacement.Top,
                    _paginationStates,
                    _foldoutStates,
                    foldoutBehavior,
                    triggeredContexts,
                    globalPlacementIsTop
                )
            )
            {
                EditorGUILayout.Space();
            }

            string scriptPathOrNull = scriptProperty != null ? scriptProperty.propertyPath : null;
            WGroupLayout layout = WGroupLayoutBuilder.Build(serializedObject, scriptPathOrNull);
            IReadOnlyList<WGroupDrawOperation> operations = layout.Operations;

            for (int index = 0; index < operations.Count; index++)
            {
                WGroupDrawOperation operation = operations[index];
                if (operation.Type == WGroupDrawOperationType.Group)
                {
                    WGroupDefinition definition = operation.Group;
                    if (definition == null)
                    {
                        continue;
                    }

                    WGroupGUI.DrawGroup(
                        definition,
                        serializedObject,
                        _groupFoldoutStates,
                        propertyLookup
                    );
                    continue;
                }

                if (operation.IsHiddenInInspector)
                {
                    continue;
                }

                if (
                    !propertyLookup.TryGetValue(
                        operation.PropertyPath,
                        out SerializedProperty property
                    )
                )
                {
                    continue;
                }

                if (!WShowIfPropertyDrawer.ShouldShowProperty(property))
                {
                    continue;
                }

                DrawValidationHelpBoxIfNeeded(property);

                EditorGUILayout.PropertyField(property, true);
            }

            serializedObject.ApplyModifiedProperties();

            if (
                WButtonGUI.DrawButtons(
                    this,
                    WButtonPlacement.Bottom,
                    _paginationStates,
                    _foldoutStates,
                    foldoutBehavior,
                    triggeredContexts,
                    globalPlacementIsTop
                )
            )
            {
                EditorGUILayout.Space();
            }

            if (triggeredContexts.Count > 0)
            {
                WButtonInvocationController.ProcessTriggeredMethods(triggeredContexts);
            }
        }

        private static SerializedProperty BuildPropertyLookup(
            SerializedObject serializedObject,
            Dictionary<string, SerializedProperty> propertyLookup
        )
        {
            propertyLookup.Clear();
            SerializedProperty scriptProperty = null;
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                string path = iterator.propertyPath;

                if (string.Equals(path, ScriptPropertyPath, StringComparison.Ordinal))
                {
                    scriptProperty = iterator.Copy();
                    continue;
                }

                propertyLookup[path] = iterator.Copy();
            }

            return scriptProperty;
        }

        private static void DrawValidationHelpBoxIfNeeded(SerializedProperty property)
        {
            if (!property.isArray || property.propertyType == SerializedPropertyType.String)
            {
                return;
            }

            property.GetEnclosingObject(out FieldInfo fieldInfo);
            if (fieldInfo == null)
            {
                return;
            }

            ValidateAssignmentAttribute validateAttribute =
                fieldInfo.GetCustomAttribute<ValidateAssignmentAttribute>();
            if (validateAttribute != null)
            {
                ValidateAssignmentPropertyDrawer.DrawValidationHelpBoxIfNeeded(
                    property,
                    validateAttribute
                );
                return;
            }

            WNotNullAttribute notNullAttribute = fieldInfo.GetCustomAttribute<WNotNullAttribute>();
            if (notNullAttribute != null)
            {
                WNotNullPropertyDrawer.DrawValidationHelpBoxIfNeeded(property, notNullAttribute);
            }
        }
    }

    /// <summary>
    /// Custom editor for Odin Inspector's SerializedScriptableObject that adds WButton and WGroup support.
    /// This editor takes precedence over Odin's default editor when ODIN_INSPECTOR is defined.
    /// </summary>
    [CustomEditor(typeof(SerializedScriptableObject), true)]
    [CanEditMultipleObjects]
    public sealed class WButtonOdinScriptableObjectInspector : Editor
    {
        private const string ScriptPropertyPath = "m_Script";

        private static readonly WallstopGenericPool<
            Dictionary<string, SerializedProperty>
        > PropertyLookupPool = new(
            () => new Dictionary<string, SerializedProperty>(16, StringComparer.Ordinal),
            onRelease: d => d.Clear()
        );

        private readonly Dictionary<WButtonGroupKey, WButtonPaginationState> _paginationStates =
            new();
        private readonly Dictionary<WButtonGroupKey, bool> _foldoutStates = new();
        private readonly Dictionary<int, bool> _groupFoldoutStates = new();

        public override void OnInspectorGUI()
        {
            using PooledResource<List<WButtonMethodContext>> triggeredContextsLease =
                Buffers<WButtonMethodContext>.GetList(
                    4,
                    out List<WButtonMethodContext> triggeredContexts
                );

            serializedObject.UpdateIfRequiredOrScript();

            using PooledResource<Dictionary<string, SerializedProperty>> propertyLookupLease =
                PropertyLookupPool.Get(out Dictionary<string, SerializedProperty> propertyLookup);

            SerializedProperty scriptProperty = BuildPropertyLookup(
                serializedObject,
                propertyLookup
            );

            bool drawScriptField = scriptProperty != null && !InlineInspectorContext.IsActive;
            if (drawScriptField)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(scriptProperty, true);
                }
                EditorGUILayout.Space();
            }

            UnityHelpersSettings.WButtonActionsPlacement placement =
                UnityHelpersSettings.GetWButtonActionsPlacement();
            UnityHelpersSettings.WButtonFoldoutBehavior foldoutBehavior =
                UnityHelpersSettings.GetWButtonFoldoutBehavior();
            bool globalPlacementIsTop =
                placement == UnityHelpersSettings.WButtonActionsPlacement.Top;

            if (
                WButtonGUI.DrawButtons(
                    this,
                    WButtonPlacement.Top,
                    _paginationStates,
                    _foldoutStates,
                    foldoutBehavior,
                    triggeredContexts,
                    globalPlacementIsTop
                )
            )
            {
                EditorGUILayout.Space();
            }

            string scriptPathOrNull = scriptProperty != null ? scriptProperty.propertyPath : null;
            WGroupLayout layout = WGroupLayoutBuilder.Build(serializedObject, scriptPathOrNull);
            IReadOnlyList<WGroupDrawOperation> operations = layout.Operations;

            for (int index = 0; index < operations.Count; index++)
            {
                WGroupDrawOperation operation = operations[index];
                if (operation.Type == WGroupDrawOperationType.Group)
                {
                    WGroupDefinition definition = operation.Group;
                    if (definition == null)
                    {
                        continue;
                    }

                    WGroupGUI.DrawGroup(
                        definition,
                        serializedObject,
                        _groupFoldoutStates,
                        propertyLookup
                    );
                    continue;
                }

                if (operation.IsHiddenInInspector)
                {
                    continue;
                }

                if (
                    !propertyLookup.TryGetValue(
                        operation.PropertyPath,
                        out SerializedProperty property
                    )
                )
                {
                    continue;
                }

                if (!WShowIfPropertyDrawer.ShouldShowProperty(property))
                {
                    continue;
                }

                DrawValidationHelpBoxIfNeeded(property);

                EditorGUILayout.PropertyField(property, true);
            }

            serializedObject.ApplyModifiedProperties();

            if (
                WButtonGUI.DrawButtons(
                    this,
                    WButtonPlacement.Bottom,
                    _paginationStates,
                    _foldoutStates,
                    foldoutBehavior,
                    triggeredContexts,
                    globalPlacementIsTop
                )
            )
            {
                EditorGUILayout.Space();
            }

            if (triggeredContexts.Count > 0)
            {
                WButtonInvocationController.ProcessTriggeredMethods(triggeredContexts);
            }
        }

        private static SerializedProperty BuildPropertyLookup(
            SerializedObject serializedObject,
            Dictionary<string, SerializedProperty> propertyLookup
        )
        {
            propertyLookup.Clear();
            SerializedProperty scriptProperty = null;
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                string path = iterator.propertyPath;

                if (string.Equals(path, ScriptPropertyPath, StringComparison.Ordinal))
                {
                    scriptProperty = iterator.Copy();
                    continue;
                }

                propertyLookup[path] = iterator.Copy();
            }

            return scriptProperty;
        }

        private static void DrawValidationHelpBoxIfNeeded(SerializedProperty property)
        {
            if (!property.isArray || property.propertyType == SerializedPropertyType.String)
            {
                return;
            }

            property.GetEnclosingObject(out FieldInfo fieldInfo);
            if (fieldInfo == null)
            {
                return;
            }

            ValidateAssignmentAttribute validateAttribute =
                fieldInfo.GetCustomAttribute<ValidateAssignmentAttribute>();
            if (validateAttribute != null)
            {
                ValidateAssignmentPropertyDrawer.DrawValidationHelpBoxIfNeeded(
                    property,
                    validateAttribute
                );
                return;
            }

            WNotNullAttribute notNullAttribute = fieldInfo.GetCustomAttribute<WNotNullAttribute>();
            if (notNullAttribute != null)
            {
                WNotNullPropertyDrawer.DrawValidationHelpBoxIfNeeded(property, notNullAttribute);
            }
        }
    }
#endif
}
