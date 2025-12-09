namespace WallstopStudios.UnityHelpers.Editor.CustomEditors
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using WallstopStudios.UnityHelpers.Editor.Internal;
    using WallstopStudios.UnityHelpers.Editor.Settings;
    using WallstopStudios.UnityHelpers.Editor.Utils.WButton;
    using WallstopStudios.UnityHelpers.Editor.Utils.WGroup;
    using WallstopStudios.UnityHelpers.Utils;

    [CustomEditor(typeof(UnityEngine.Object), true)]
    [CanEditMultipleObjects]
    public sealed class WButtonInspector : Editor
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
            bool placementIsTop = placement == UnityHelpersSettings.WButtonActionsPlacement.Top;

            // When placement is Top: render ALL buttons above properties
            // When placement is Bottom: render ALL buttons below properties
            // We always call both Top and Bottom placements to ensure all buttons render
            // regardless of their individual drawOrder values.
            // Lower drawOrder values should render FIRST, so we render Bottom placement
            // (which has lower drawOrder values like -21, -5, etc.) before Top placement
            // (which has higher drawOrder values like -1, 0, 1, etc.)
            if (placementIsTop)
            {
                // Draw buttons with drawOrder < -1 (bottom placement buttons) FIRST
                // These have lower drawOrder values and should appear first
                if (
                    WButtonGUI.DrawButtons(
                        this,
                        WButtonPlacement.Bottom,
                        _paginationStates,
                        _foldoutStates,
                        foldoutBehavior,
                        triggeredContexts
                    )
                )
                {
                    EditorGUILayout.Space();
                }

                // Draw buttons with drawOrder >= -1 (top placement buttons) SECOND
                // These have higher drawOrder values and should appear after
                if (
                    WButtonGUI.DrawButtons(
                        this,
                        WButtonPlacement.Top,
                        _paginationStates,
                        _foldoutStates,
                        foldoutBehavior,
                        triggeredContexts
                    )
                )
                {
                    EditorGUILayout.Space();
                }
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

                if (
                    !propertyLookup.TryGetValue(
                        operation.PropertyPath,
                        out SerializedProperty property
                    )
                )
                {
                    continue;
                }

                EditorGUILayout.PropertyField(property, true);
            }

            serializedObject.ApplyModifiedProperties();

            // When placement is Bottom: render ALL buttons below properties
            // Same ordering as Top: lower drawOrder values render first
            if (!placementIsTop)
            {
                // Draw buttons with drawOrder < -1 (bottom placement buttons) FIRST
                // These have lower drawOrder values and should appear first
                if (
                    WButtonGUI.DrawButtons(
                        this,
                        WButtonPlacement.Bottom,
                        _paginationStates,
                        _foldoutStates,
                        foldoutBehavior,
                        triggeredContexts
                    )
                )
                {
                    EditorGUILayout.Space();
                }

                // Draw buttons with drawOrder >= -1 (top placement buttons) SECOND
                // These have higher drawOrder values and should appear after
                if (
                    WButtonGUI.DrawButtons(
                        this,
                        WButtonPlacement.Top,
                        _paginationStates,
                        _foldoutStates,
                        foldoutBehavior,
                        triggeredContexts
                    )
                )
                {
                    EditorGUILayout.Space();
                }
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
    }
#endif
}
