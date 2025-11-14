namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers
{
#if UNITY_EDITOR
    using System;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers;
    using WallstopStudios.UnityHelpers.Tests.Utils;

    [TestFixture]
    public sealed class ValueDropdownDrawerTests : CommonTestBase
    {
        [Test]
        public void ApplyOptionUpdatesFloatSerializedProperty()
        {
            FloatDropdownAsset asset = CreateScriptableObject<FloatDropdownAsset>();
            try
            {
                SerializedObject serializedObject = new SerializedObject(asset);
                serializedObject.Update();

                SerializedProperty property = serializedObject.FindProperty(
                    nameof(FloatDropdownAsset.selection)
                );
                Assert.IsNotNull(property, "Failed to locate float selection property.");

                InvokeApplyOption(property, 2.5f);
                serializedObject.ApplyModifiedProperties();

                Assert.That(asset.selection, Is.EqualTo(2.5f).Within(0.0001f));
            }
            finally
            {
                ScriptableObject.DestroyImmediate(asset);
            }
        }

        [Test]
        public void ApplyOptionUpdatesDoubleSerializedProperty()
        {
            FloatDropdownAsset asset = CreateScriptableObject<FloatDropdownAsset>();
            try
            {
                SerializedObject serializedObject = new SerializedObject(asset);
                serializedObject.Update();

                SerializedProperty property = serializedObject.FindProperty(
                    nameof(FloatDropdownAsset.preciseSelection)
                );
                Assert.IsNotNull(property, "Failed to locate double selection property.");

                InvokeApplyOption(property, 5.25);
                serializedObject.ApplyModifiedProperties();

                Assert.That(asset.preciseSelection, Is.EqualTo(5.25d).Within(0.000001d));
            }
            finally
            {
                ScriptableObject.DestroyImmediate(asset);
            }
        }

        private static void InvokeApplyOption(SerializedProperty property, object value)
        {
            MethodInfo applyMethod = typeof(ValueDropdownDrawer).GetMethod(
                "ApplyOption",
                BindingFlags.NonPublic | BindingFlags.Static
            );
            Assert.IsNotNull(applyMethod, "Unable to resolve ValueDropdownDrawer.ApplyOption.");
            applyMethod.Invoke(null, new object[] { property, value });
        }

        [Serializable]
        private sealed class FloatDropdownAsset : ScriptableObject
        {
            [ValueDropdown(typeof(DropdownSource), nameof(DropdownSource.GetFloatValues))]
            public float selection = 1f;

            [ValueDropdown(typeof(DropdownSource), nameof(DropdownSource.GetDoubleValues))]
            public double preciseSelection = 2d;
        }

        private static class DropdownSource
        {
            internal static float[] GetFloatValues()
            {
                return new[] { 1f, 2.5f, 5f };
            }

            internal static double[] GetDoubleValues()
            {
                return new[] { 2d, 4.5d, 5.25d };
            }
        }
    }
#endif
}
