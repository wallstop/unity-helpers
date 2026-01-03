// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.TestUtils
{
    using System;
    using UnityEditor;
    using UnityEngine;

    [Serializable]
    public sealed class DrawerVisualRegressionKey : IEquatable<DrawerVisualRegressionKey>
    {
        public int id;

        public DrawerVisualRegressionKey() { }

        internal DrawerVisualRegressionKey(int value)
        {
            id = value;
        }

        public bool Equals(DrawerVisualRegressionKey other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return id == other.id;
        }

        public override bool Equals(object obj)
        {
            return obj is DrawerVisualRegressionKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return id.GetHashCode();
        }
    }

    [Serializable]
    public sealed class DrawerVisualRegressionDictionaryValue
        : IEquatable<DrawerVisualRegressionDictionaryValue>
    {
        public int data;

        public DrawerVisualRegressionDictionaryValue() { }

        internal DrawerVisualRegressionDictionaryValue(int value)
        {
            data = value;
        }

        public bool Equals(DrawerVisualRegressionDictionaryValue other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return data == other.data;
        }

        public override bool Equals(object obj)
        {
            return obj is DrawerVisualRegressionDictionaryValue other && Equals(other);
        }

        public override int GetHashCode()
        {
            return data.GetHashCode();
        }
    }

    [Serializable]
    public sealed class DrawerVisualRegressionSetValue : IEquatable<DrawerVisualRegressionSetValue>
    {
        public int data;

        public DrawerVisualRegressionSetValue() { }

        internal DrawerVisualRegressionSetValue(int value)
        {
            data = value;
        }

        public bool Equals(DrawerVisualRegressionSetValue other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return data == other.data;
        }

        public override bool Equals(object obj)
        {
            return obj is DrawerVisualRegressionSetValue other && Equals(other);
        }

        public override int GetHashCode()
        {
            return data.GetHashCode();
        }
    }

    internal static class DrawerVisualRegressionValueDrawerHelpers
    {
        public static void DrawValue(Rect position, SerializedProperty property)
        {
            SerializedProperty dataProperty = property?.FindPropertyRelative(
                nameof(DrawerVisualRegressionSetValue.data)
            );
            if (dataProperty != null)
            {
                EditorGUI.PropertyField(position, dataProperty, GUIContent.none);
            }
        }

        public static float GetValueHeight(SerializedProperty property)
        {
            SerializedProperty dataProperty = property?.FindPropertyRelative(
                nameof(DrawerVisualRegressionSetValue.data)
            );
            return dataProperty != null
                ? EditorGUI.GetPropertyHeight(dataProperty, GUIContent.none, true)
                : EditorGUIUtility.singleLineHeight;
        }
    }
}
