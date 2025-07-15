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
                string currentPath = DirectoryHelper.GetCallerScriptDirectory();
                string packagePath = DirectoryHelper.FindPackageRootPath(currentPath);
                string materialPathFullPath =
                    $"{packagePath}/Shaders/Materials/BackgroundMask-Material.mat".SanitizePath();
                string materialRelativePath = DirectoryHelper.AbsoluteToUnityRelativePath(
                    materialPathFullPath
                );
                Material defaultMask = AssetDatabase.LoadAssetAtPath<Material>(
                    materialRelativePath
                );
                if (defaultMask != null)
                {
                    instance.material = defaultMask;
                }
                else
                {
                    Debug.LogError($"Failed to load material at path '{materialRelativePath}'.");
                }
            }

            EditorGUILayout.PropertyField(_hdrColorProperty, new GUIContent("HDR Color"));
            EditorGUILayout.PropertyField(
                _shapeMaskProperty,
                new GUIContent(
                    "Shape Mask",
                    "Material shader must have an exposed _ShapeMask texture2D property to function. Otherwise, this does nothing."
                )
            );

            serializedObject.ApplyModifiedProperties();
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
