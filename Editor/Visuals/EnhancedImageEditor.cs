﻿namespace WallstopStudios.UnityHelpers.Editor.Visuals
{
#if UNITY_EDITOR
    using System;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UI;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Visuals.UGUI;
    using Object = UnityEngine.Object;

    /*
        This is needed because Unity already has a custom Inspector for Images which will not display our nice, cool new
        fields.
     */
    [CustomEditor(typeof(EnhancedImage))]
    [CanEditMultipleObjects]
    public sealed class ImageExtendedEditor : UnityEditor.UI.ImageEditor
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

            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();

            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, nameof(Image.material));
            serializedObject.ApplyModifiedProperties();
        }
    }

    // Set the icon for ImageExtended to match the icon of the Image component
    [InitializeOnLoad]
    public static class ImageExtendedIcon
    {
        private static EnhancedImage IconReference;

        static ImageExtendedIcon()
        {
            IconReference = new GameObject("Icon Object")
            {
                hideFlags = HideFlags.HideAndDontSave,
            }.AddComponent<EnhancedImage>();

            EditorGUIUtility.SetIconForObject(
                MonoScript.FromMonoBehaviour(IconReference),
                EditorGUIUtility.IconContent("Image Icon").image as Texture2D
            );

            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }

        private static void OnBeforeAssemblyReload()
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
