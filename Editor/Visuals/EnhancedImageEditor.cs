// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

/*
    Original implementation provided by JWoe
 */
namespace WallstopStudios.UnityHelpers.Editor.Visuals
{
#if UNITY_EDITOR
    using System;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Visuals.UGUI;
    using Object = UnityEngine.Object;

    /*
        This is needed because Unity already has a custom Inspector for Images which will not display our nice, cool new
        fields.
     */
    [CustomEditor(typeof(EnhancedImage))]
    [CanEditMultipleObjects]
    public sealed class ExtendedImageEditor : UnityEditor.UI.ImageEditor
    {
        private SerializedProperty _hdrColorProperty;
        private SerializedProperty _shapeMaskProperty;

        private GUIStyle _impactButtonStyle;

        protected override void OnEnable()
        {
            base.OnEnable();
            _hdrColorProperty = serializedObject.FindProperty(nameof(EnhancedImage._hdrColor));
            _shapeMaskProperty = serializedObject.FindProperty(nameof(EnhancedImage._shapeMask));
        }

        public override void OnInspectorGUI()
        {
            _impactButtonStyle ??= new GUIStyle(GUI.skin.button)
            {
                normal = { textColor = Color.yellow },
                fontStyle = FontStyle.Bold,
            };

            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Extended Properties", EditorStyles.boldLabel);

            EnhancedImage instance = target as EnhancedImage;
            if (
                instance != null
                && (
                    instance.material == null
                    || string.Equals(
                        instance.material.name,
                        "Default UI Material",
                        StringComparison.Ordinal
                    )
                )
                && GUILayout.Button("Incorrect Material Detected - Try Fix?", _impactButtonStyle)
            )
            {
                string materialRelativePath = DirectoryHelper.ResolvePackageAssetPath(
                    "Shaders/Materials/BackgroundMask-Material.mat"
                );
                Material defaultMask = null;
                if (!string.IsNullOrEmpty(materialRelativePath))
                {
                    defaultMask = AssetDatabase.LoadAssetAtPath<Material>(materialRelativePath);
                }

                if (defaultMask != null)
                {
                    Undo.RecordObject(instance, "Fix EnhancedImage Material");
                    instance.material = defaultMask;
                    // Force immediate material instance creation and color application
                    instance.ForceRefreshMaterialInstance();
                    EditorUtility.SetDirty(instance);
                }
                else
                {
                    Debug.LogError($"Failed to load material at path '{materialRelativePath}'.");
                }
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_hdrColorProperty, new GUIContent("HDR Color"));
            EditorGUILayout.PropertyField(
                _shapeMaskProperty,
                new GUIContent(
                    "Shape Mask",
                    "Material shader must have an exposed _ShapeMask texture2D property to function. Otherwise, this does nothing."
                )
            );
            bool extendedPropertiesChanged = EditorGUI.EndChangeCheck();

            if (extendedPropertiesChanged)
            {
                serializedObject.ApplyModifiedProperties();
                // Force immediate update of the material instance for all targets.
                // OnValidate() is called by Unity after ApplyModifiedProperties(), but
                // we also need to ensure the material is refreshed in the Editor view.
                foreach (Object targetObject in targets)
                {
                    if (targetObject is EnhancedImage enhancedImage)
                    {
                        enhancedImage.ForceRefreshMaterialInstance();
                        EditorUtility.SetDirty(enhancedImage);
                    }
                }
                // Repaint to immediately show updated material in Inspector
                Repaint();
            }

            EditorGUILayout.Space();

            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();
        }
    }

    // Set the icon for ExtendedImages to match the icon of the Image component
    [InitializeOnLoad]
    public static class ExtendedImageIcon
    {
        private const string SingletonName = "_ICON_OBJECT_SINGLETON_";

        private static EnhancedImage IconReference;

        static ExtendedImageIcon()
        {
            RegenerateIconSingleton();
            EnsureSingletonLifecycle();
        }

        private static void EnsureSingletonLifecycle()
        {
            AssemblyReloadEvents.beforeAssemblyReload += SingletonIconCleanup;
            EditorApplication.quitting += SingletonIconCleanup;

            EditorSceneManager.sceneOpened += (_, _) => RegenerateIconSingleton();
            EditorSceneManager.newSceneCreated += (_, _, _) => RegenerateIconSingleton();
            SceneManager.sceneLoaded += (_, _) => RegenerateIconSingleton();
            SceneManager.activeSceneChanged += (_, _) => RegenerateIconSingleton();
        }

        private static void RegenerateIconSingleton()
        {
            GameObject existingSingleton = GameObject.Find(SingletonName);
            if (existingSingleton == null)
            {
                IconReference = new GameObject(SingletonName)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                }.AddComponent<EnhancedImage>();
            }
            else
            {
                IconReference = existingSingleton.GetOrAddComponent<EnhancedImage>();
                IconReference.hideFlags = HideFlags.HideAndDontSave;
            }

            EditorGUIUtility.SetIconForObject(
                MonoScript.FromMonoBehaviour(IconReference),
                EditorGUIUtility.IconContent("Image Icon").image as Texture2D
            );
        }

        private static void SingletonIconCleanup()
        {
            if (IconReference == null)
            {
                return;
            }

            Object.DestroyImmediate(IconReference.gameObject);
            IconReference = null;
        }
    }
#endif
}
