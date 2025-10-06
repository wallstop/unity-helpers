namespace WallstopStudios.UnityHelpers.Editor
{
#if UNITY_EDITOR
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Core.Extension;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Utils;
    using Object = UnityEngine.Object;

    public sealed class PrefabChecker : EditorWindow
    {
        private const float ToggleWidth = 18f;
        private const float ToggleSpacing = 4f;

        private static readonly Dictionary<Type, List<FieldInfo>> FieldsByType = new();
        private static readonly Dictionary<Type, List<FieldInfo>> ListFieldsByType = new();
        private static readonly Dictionary<Type, List<FieldInfo>> StringFieldsByType = new();
        private static readonly Dictionary<Type, List<FieldInfo>> ObjectFieldsByType = new();
        private static readonly Dictionary<Type, RequireComponent[]> RequiredComponentsByType =
            new();

        private readonly List<string> _assetPaths = new();
        private Vector2 _scrollPosition;

        private bool _checkMissingScripts = true;
        private bool _checkNullElementsInLists = true;
        private bool _checkMissingRequiredComponents = true;
        private bool _checkEmptyStringFields;
        private bool _checkNullObjectReferences = true;
        private bool _onlyCheckNullObjectsWithAttribute = true;
        private bool _checkDisabledRootGameObjects = true;
        private bool _checkDisabledComponents;

        private const string DefaultPrefabsFolder = "Assets/Prefabs";

        [MenuItem("Tools/Wallstop Studios/Unity Helpers/Prefab Checker", priority = -1)]
        public static void ShowWindow()
        {
            GetWindow<PrefabChecker>("Prefab Check");
        }

        private void OnEnable()
        {
            PopulateDefaultPaths();
        }

        private void PopulateDefaultPaths()
        {
            if (_assetPaths.Count == 0 && AssetDatabase.IsValidFolder(DefaultPrefabsFolder))
            {
                _assetPaths.Add(DefaultPrefabsFolder);
            }
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            try
            {
                DrawConfigurationOptions();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Target Folders", EditorStyles.boldLabel);
                DrawAssetPaths();
                if (GUILayout.Button("Add Folder"))
                {
                    AddFolder();
                }

                EditorGUILayout.Space();
                if (GUILayout.Button("Run Checks", GUILayout.Height(30)))
                {
                    RunChecks();
                }
            }
            finally
            {
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawConfigurationOptions()
        {
            EditorGUILayout.LabelField("Validation Checks", EditorStyles.boldLabel);

            Func<GUIContent, bool, float?, bool, bool> drawRightAlignedToggle =
                SetupDrawRightAlignedToggle();

            float targetAlignmentX = 0f;
            bool alignmentCalculated = false;

            DrawAndAlign(
                new GUIContent(
                    "Missing Scripts",
                    "Check for GameObjects with missing script references."
                ),
                () => _checkMissingScripts,
                v => _checkMissingScripts = v,
                false
            );
            DrawAndAlign(
                new GUIContent(
                    "Nulls in Lists/Arrays",
                    "Check for null elements within serialized lists or arrays."
                ),
                () => _checkNullElementsInLists,
                v => _checkNullElementsInLists = v,
                false
            );
            DrawAndAlign(
                new GUIContent(
                    "Missing Required Components",
                    "Check if components are missing dependencies defined by [RequireComponent]."
                ),
                () => _checkMissingRequiredComponents,
                v => _checkMissingRequiredComponents = v,
                false
            );
            DrawAndAlign(
                new GUIContent(
                    "Empty String Fields",
                    "Check for serialized string fields that are empty."
                ),
                () => _checkEmptyStringFields,
                v => _checkEmptyStringFields = v,
                false
            );

            DrawAndAlign(
                new GUIContent(
                    "Null Object References",
                    "Check for serialized UnityEngine.Object fields that are null."
                ),
                () => _checkNullObjectReferences,
                v => _checkNullObjectReferences = v,
                false
            );

            bool wasEnabled = GUI.enabled;
            GUI.enabled = wasEnabled && _checkNullObjectReferences;
            try
            {
                using EditorGUI.IndentLevelScope indent = new();
                DrawAndAlign(
                    new GUIContent(
                        "Only if [ValidateAssignment]",
                        "Only report null object references if the field has the [ValidateAssignment] attribute."
                    ),
                    () => _onlyCheckNullObjectsWithAttribute,
                    v => _onlyCheckNullObjectsWithAttribute = v,
                    true
                );
            }
            finally
            {
                GUI.enabled = wasEnabled;
            }

            DrawAndAlign(
                new GUIContent(
                    "Disabled Root GameObject",
                    "Check if the prefab's root GameObject is inactive."
                ),
                () => _checkDisabledRootGameObjects,
                v => _checkDisabledRootGameObjects = v,
                false
            );
            DrawAndAlign(
                new GUIContent(
                    "Disabled Components",
                    "Check for any components on the prefab that are disabled."
                ),
                () => _checkDisabledComponents,
                v => _checkDisabledComponents = v,
                false
            );
            return;

            void DrawAndAlign(
                GUIContent content,
                Func<bool> getter,
                Action<bool> setter,
                bool isNested
            )
            {
                switch (alignmentCalculated)
                {
                    case false when !isNested:
                    {
                        float viewWidth = EditorGUIUtility.currentViewWidth;

                        float availableWidth = viewWidth - 18f;
                        targetAlignmentX = availableWidth - ToggleWidth;
                        alignmentCalculated = true;
                        break;
                    }
                    case false when isNested:
                    {
                        float viewWidth = EditorGUIUtility.currentViewWidth;
                        float availableWidth = viewWidth - 20f - 15f;
                        targetAlignmentX = availableWidth - ToggleWidth;
                        alignmentCalculated = true;
                        this.LogWarn(
                            $"Calculated alignment X based on first item being nested. Alignment might be approximate."
                        );
                        break;
                    }
                }

                float? overrideX = isNested ? targetAlignmentX : null;

                bool newValue = drawRightAlignedToggle(content, getter(), overrideX, isNested);
                if (newValue != getter())
                {
                    setter(newValue);
                }
            }
        }

        private static Func<GUIContent, bool, float?, bool, bool> SetupDrawRightAlignedToggle()
        {
            return (label, value, overrideToggleX, isNested) =>
            {
                Rect lineRect;
                if (isNested)
                {
                    EditorGUI.indentLevel++;
                }

                try
                {
                    lineRect = EditorGUILayout.GetControlRect(
                        true,
                        EditorGUIUtility.singleLineHeight
                    );
                }
                finally
                {
                    if (isNested)
                    {
                        EditorGUI.indentLevel--;
                    }
                }

                float defaultToggleX = lineRect.x + lineRect.width - ToggleWidth;
                float finalToggleX = overrideToggleX ?? defaultToggleX;

                finalToggleX = Mathf.Max(finalToggleX, lineRect.x + ToggleSpacing);

                Rect toggleRect = new(finalToggleX, lineRect.y, ToggleWidth, lineRect.height);

                float labelWidth = Mathf.Max(0, finalToggleX - lineRect.x - ToggleSpacing);
                Rect labelRect = new(lineRect.x, lineRect.y, labelWidth, lineRect.height);

                EditorGUI.LabelField(labelRect, label);
                return EditorGUI.Toggle(toggleRect, value);
            };
        }

        private void DrawAssetPaths()
        {
            if (_assetPaths.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No target folders specified. Add folders containing prefabs to check.",
                    MessageType.Info
                );
            }

            List<string> pathsToRemove = null;

            foreach (string assetPath in _assetPaths)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    string currentPath = assetPath;
                    EditorGUILayout.LabelField(currentPath);

                    DefaultAsset folderAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(
                        currentPath
                    );
                    if (folderAsset != null)
                    {
                        if (GUILayout.Button("Ping", GUILayout.Width(50)))
                        {
                            EditorGUIUtility.PingObject(folderAsset);
                        }
                    }
                    else
                    {
                        GUILayout.Space(56);
                    }

                    if (GUILayout.Button("X", GUILayout.Width(25)))
                    {
                        pathsToRemove ??= new List<string>();
                        pathsToRemove.Add(currentPath);
                    }
                }
            }

            if (pathsToRemove != null)
            {
                foreach (string path in pathsToRemove)
                {
                    _assetPaths.Remove(path);
                }
                Repaint();
            }
        }

        private void AddFolder()
        {
            string projectPath = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(projectPath))
            {
                this.LogError($"Failed to find project path!");
                return;
            }
            string absolutePath = EditorUtility.OpenFolderPanel(
                "Select Prefab Folder",
                "Assets",
                ""
            );

            if (!string.IsNullOrEmpty(absolutePath))
            {
                if (absolutePath.StartsWith(projectPath, StringComparison.Ordinal))
                {
                    string relativePath =
                        "Assets" + absolutePath.Substring(projectPath.Length).Replace('\\', '/');

                    if (AssetDatabase.IsValidFolder(relativePath))
                    {
                        if (!_assetPaths.Contains(relativePath))
                        {
                            _assetPaths.Add(relativePath);
                        }
                        else
                        {
                            this.LogWarn($"Folder '{relativePath}' is already in the list.");
                        }
                    }
                    else
                    {
                        this.LogWarn(
                            $"Selected path '{relativePath}' is not a valid Unity folder."
                        );
                    }
                }
                else
                {
                    this.LogError(
                        $"Selected folder must be inside the Unity project's Assets folder. Project path: {projectPath}, Selected path: {absolutePath}"
                    );
                }
            }
        }

        private void RunChecks()
        {
            if (_assetPaths is not { Count: > 0 })
            {
                this.LogError($"No asset paths specified. Add folders containing prefabs.");
                return;
            }

            List<string> validPaths = _assetPaths
                .Where(p => !string.IsNullOrEmpty(p) && AssetDatabase.IsValidFolder(p))
                .ToList();

            if (validPaths.Count == 0)
            {
                this.LogError(
                    $"None of the specified paths are valid folders: {string.Join(", ", _assetPaths)}"
                );
                return;
            }

            this.Log($"Starting prefab check for folders: {string.Join(", ", validPaths)}");
            int totalPrefabsChecked = 0;
            int totalIssuesFound = 0;

            foreach (GameObject prefab in Helpers.EnumeratePrefabs(validPaths))
            {
                totalPrefabsChecked++;
                int issuesForThisPrefab = 0;
                string prefabPath = AssetDatabase.GetAssetPath(prefab);

                if (_checkDisabledRootGameObjects && !prefab.activeSelf)
                {
                    prefab.LogWarn($"Prefab root GameObject is disabled.");
                    issuesForThisPrefab++;
                }

                using PooledResource<List<MonoBehaviour>> componentBufferResource =
                    Buffers<MonoBehaviour>.List.Get();
                List<MonoBehaviour> componentBuffer = componentBufferResource.resource;
                prefab.GetComponentsInChildren(true, componentBuffer);

                foreach (MonoBehaviour script in componentBuffer)
                {
                    if (_checkMissingScripts && !script)
                    {
                        GameObject owner = FindOwnerOfMissingScript(prefab, componentBuffer);
                        string ownerName = owner ? owner.name : "[[Unknown GameObject]]";
                        Object context = owner ? (Object)owner : prefab;
                        context.LogError($"Detected missing script on GameObject '{ownerName}'.");
                        issuesForThisPrefab++;
                        continue;
                    }

                    if (!script)
                    {
                        continue;
                    }

                    Type scriptType = script.GetType();
                    GameObject ownerGameObject = script.gameObject;

                    if (_checkNullElementsInLists)
                    {
                        issuesForThisPrefab += ValidateNoNullsInLists(script, ownerGameObject);
                    }

                    if (_checkMissingRequiredComponents)
                    {
                        issuesForThisPrefab += ValidateRequiredComponents(script, ownerGameObject);
                    }

                    if (_checkEmptyStringFields)
                    {
                        issuesForThisPrefab += ValidateEmptyStrings(script, ownerGameObject);
                    }

                    if (_checkNullObjectReferences)
                    {
                        issuesForThisPrefab += ValidateNullObjectReferences(
                            script,
                            ownerGameObject
                        );
                    }

                    if (_checkDisabledComponents && script is Behaviour { enabled: false })
                    {
                        ownerGameObject.LogWarn(
                            $"Component '{scriptType.Name}' on GameObject '{ownerGameObject.name}' is disabled."
                        );
                        issuesForThisPrefab++;
                    }
                }

                if (issuesForThisPrefab > 0)
                {
                    prefab.LogWarn(
                        $"Prefab '{prefab.name}' at path '{prefabPath}' has {issuesForThisPrefab} potential issues."
                    );
                    totalIssuesFound += issuesForThisPrefab;
                }
            }

            if (totalIssuesFound > 0)
            {
                this.LogError(
                    $"Prefab check complete. Found {totalIssuesFound} potential issues across {totalPrefabsChecked} prefabs."
                );
            }
            else
            {
                this.Log(
                    $"Prefab check complete. No issues found in {totalPrefabsChecked} prefabs."
                );
            }
        }

        private static GameObject FindOwnerOfMissingScript(
            GameObject prefabRoot,
            List<MonoBehaviour> buffer
        )
        {
            using PooledResource<List<Transform>> transformBufferResource =
                Buffers<Transform>.List.Get();
            List<Transform> transforms = transformBufferResource.resource;
            prefabRoot.GetComponentsInChildren(true, transforms);
            foreach (Transform transform in transforms)
            {
                using PooledResource<List<MonoBehaviour>> componentBuffer =
                    Buffers<MonoBehaviour>.List.Get();
                List<MonoBehaviour> components = componentBuffer.resource;
                transform.GetComponents(components);
                int bufferCount = 0;
                foreach (MonoBehaviour c in components)
                {
                    if (c != null && c.gameObject == transform.gameObject)
                    {
                        ++bufferCount;
                    }
                }
                if (components.Count == bufferCount)
                {
                    continue;
                }

                bool foundInNonNullBuffer = false;
                foreach (MonoBehaviour c in components)
                {
                    if (buffer.Contains(c))
                    {
                        foundInNonNullBuffer = true;
                        break;
                    }
                }

                if (foundInNonNullBuffer)
                {
                    return transform.gameObject;
                }

                if (components.Count != 0 || !buffer.Exists(c => c == null))
                {
                    continue;
                }

                using PooledResource<HashSet<GameObject>> setResource =
                    Buffers<GameObject>.HashSet.Get();
                HashSet<GameObject> gameObjectsWithComponentsInBuffer = setResource.resource;
                foreach (MonoBehaviour c in buffer)
                {
                    if (c != null)
                    {
                        gameObjectsWithComponentsInBuffer.Add(c.gameObject);
                    }
                }
                if (!gameObjectsWithComponentsInBuffer.Contains(transform.gameObject))
                {
                    return transform.gameObject;
                }
            }

            return prefabRoot;
        }

        private static IEnumerable<FieldInfo> GetFieldsToCheck(
            Type componentType,
            Dictionary<Type, List<FieldInfo>> cache
        )
        {
            return cache.GetOrAdd(
                componentType,
                type =>
                    type.GetFields(
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                        )
                        .Where(field =>
                            field.IsPublic
                            || field.IsAttributeDefined<SerializeField>(out _, inherit: true)
                        )
                        .ToList()
            );
        }

        private static int ValidateNoNullsInLists(Object component, GameObject context)
        {
            int issueCount = 0;
            Type componentType = component.GetType();

            foreach (
                FieldInfo field in ListFieldsByType.GetOrAdd(
                    componentType,
                    type =>
                        GetFieldsToCheck(type, FieldsByType)
                            .Where(f =>
                                typeof(IEnumerable).IsAssignableFrom(f.FieldType)
                                && f.FieldType != typeof(string)
                            )
                            .ToList()
                )
            )
            {
                object fieldValue = field.GetValue(component);

                if (fieldValue is not IEnumerable list)
                {
                    continue;
                }

                int index = 0;
                if (list is Object unityObject)
                {
                    if (list.GetType() != typeof(Transform) && unityObject == null)
                    {
                        unityObject.LogError(
                            $"Field '{field.Name}' ({field.FieldType.Name}) on component '{componentType.Name}' has a null enumerable."
                        );
                    }
                    continue;
                }
                foreach (object element in list)
                {
                    if (element == null || (element is Object unityObj && !unityObj))
                    {
                        context.LogError(
                            $"Field '{field.Name}' ({field.FieldType.Name}) on component '{componentType.Name}' has a null or missing element at index {index}."
                        );
                        issueCount++;
                    }
                    index++;
                }
            }
            return issueCount;
        }

        private static int ValidateRequiredComponents(Component component, GameObject context)
        {
            int issueCount = 0;
            Type componentType = component.GetType();

            RequireComponent[] required = RequiredComponentsByType.GetOrAdd(
                componentType,
                type => type.GetAllAttributesSafe<RequireComponent>(inherit: true)
            );

            if (required.Length <= 0)
            {
                return issueCount;
            }

            foreach (RequireComponent requirement in required)
            {
                if (
                    requirement.m_Type0 != null
                    && component.GetComponent(requirement.m_Type0) == null
                )
                {
                    context.LogError(
                        $"Component '{componentType.Name}' requires component '{requirement.m_Type0.Name}', but it is missing."
                    );
                    issueCount++;
                }
                if (
                    requirement.m_Type1 != null
                    && component.GetComponent(requirement.m_Type1) == null
                )
                {
                    context.LogError(
                        $"Component '{componentType.Name}' requires component '{requirement.m_Type1.Name}', but it is missing."
                    );
                    issueCount++;
                }
                if (
                    requirement.m_Type2 != null
                    && component.GetComponent(requirement.m_Type2) == null
                )
                {
                    context.LogError(
                        $"Component '{componentType.Name}' requires component '{requirement.m_Type2.Name}', but it is missing."
                    );
                    issueCount++;
                }
            }
            return issueCount;
        }

        private static int ValidateEmptyStrings(Object component, GameObject context)
        {
            int issueCount = 0;
            Type componentType = component.GetType();

            foreach (
                FieldInfo field in StringFieldsByType.GetOrAdd(
                    componentType,
                    type =>
                        GetFieldsToCheck(type, FieldsByType)
                            .Where(f => f.FieldType == typeof(string))
                            .ToList()
                )
            )
            {
                object fieldValue = field.GetValue(component);
                if (fieldValue is string stringValue && string.IsNullOrEmpty(stringValue))
                {
                    context.LogWarn(
                        $"String field '{field.Name}' on component '{componentType.Name}' is null or empty."
                    );
                    issueCount++;
                }
            }
            return issueCount;
        }

        private int ValidateNullObjectReferences(Object component, GameObject context)
        {
            int issueCount = 0;
            Type componentType = component.GetType();

            foreach (
                FieldInfo field in ObjectFieldsByType.GetOrAdd(
                    componentType,
                    type =>
                        GetFieldsToCheck(type, FieldsByType)
                            .Where(f => typeof(Object).IsAssignableFrom(f.FieldType))
                            .ToList()
                )
            )
            {
                bool hasValidateAttribute = field.IsAttributeDefined<ValidateAssignmentAttribute>(
                    out _,
                    inherit: false
                );

                if (_onlyCheckNullObjectsWithAttribute && !hasValidateAttribute)
                {
                    continue;
                }

                object fieldValue = field.GetValue(component);

                if (fieldValue != null && (fieldValue is not Object unityObj || unityObj))
                {
                    continue;
                }

                string attributeMarker = hasValidateAttribute ? " (has [ValidateAssignment])" : "";
                context.LogError(
                    $"Object reference field '{field.Name}'{attributeMarker} on component '{componentType.Name}' is null or missing."
                );
                issueCount++;
            }
            return issueCount;
        }
    }
#endif
}
