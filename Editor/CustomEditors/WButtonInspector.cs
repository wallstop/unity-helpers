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

        private readonly Dictionary<int, WButtonPaginationState> _paginationStates = new();
        private readonly Dictionary<int, bool> _foldoutStates = new();
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
            bool drawTop = placement == UnityHelpersSettings.WButtonActionsPlacement.Top;
            bool drawBottom = placement == UnityHelpersSettings.WButtonActionsPlacement.Bottom;

            if (
                drawTop
                && WButtonGUI.DrawButtons(
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

            if (
                drawBottom
                && WButtonGUI.DrawButtons(
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
