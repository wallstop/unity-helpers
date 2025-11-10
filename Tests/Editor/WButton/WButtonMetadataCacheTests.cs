#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.WButton
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Editor.Settings;
    using WallstopStudios.UnityHelpers.Editor.Utils;
    using WallstopStudios.UnityHelpers.Editor.Utils.WButton;

    [TestFixture]
    public sealed class WButtonMetadataCacheTests
    {
        [Test]
        public void MetadataSortedByDrawOrder()
        {
            IReadOnlyList<WButtonMethodMetadata> metadata = WButtonMetadataCache.GetMetadata(
                typeof(SampleTarget)
            );
            Assert.That(metadata, Is.Not.Empty);
            int previousOrder = metadata[0].DrawOrder;
            for (int index = 1; index < metadata.Count; index++)
            {
                int currentOrder = metadata[index].DrawOrder;
                Assert.That(previousOrder, Is.LessThanOrEqualTo(currentOrder));
                previousOrder = currentOrder;
            }
        }

        [Test]
        public void MetadataCapturesCancellationTokenIndex()
        {
            IReadOnlyList<WButtonMethodMetadata> metadata = WButtonMetadataCache.GetMetadata(
                typeof(SampleTarget)
            );
            WButtonMethodMetadata method = metadata.First(m =>
                m.Method.Name == nameof(SampleTarget.MethodWithCancellation)
            );
            Assert.That(method.CancellationTokenParameterIndex, Is.EqualTo(0));
        }

        [Test]
        public void MetadataCapturesPriority()
        {
            IReadOnlyList<WButtonMethodMetadata> metadata = WButtonMetadataCache.GetMetadata(
                typeof(SampleTarget)
            );
            WButtonMethodMetadata method = metadata.First(m =>
                m.Method.Name == nameof(SampleTarget.PriorityMethod)
            );
            Assert.That(method.Priority, Is.EqualTo("Critical"));
        }

        [Test]
        public void AsyncTaskMetadataDetectsResultType()
        {
            IReadOnlyList<WButtonMethodMetadata> metadata = WButtonMetadataCache.GetMetadata(
                typeof(SampleTarget)
            );
            WButtonMethodMetadata method = metadata.First(m =>
                m.Method.Name == nameof(SampleTarget.TaskMethodAsync)
            );
            Assert.That(method.ExecutionKind, Is.EqualTo(WButtonExecutionKind.Task));
            Assert.AreEqual(typeof(int), method.AsyncResultType);
        }

        [Test]
        public void EnumeratorMetadataClassifiedCorrectly()
        {
            IReadOnlyList<WButtonMethodMetadata> metadata = WButtonMetadataCache.GetMetadata(
                typeof(SampleTarget)
            );
            WButtonMethodMetadata method = metadata.First(m =>
                m.Method.Name == nameof(SampleTarget.EnumeratorMethod)
            );
            Assert.That(method.ExecutionKind, Is.EqualTo(WButtonExecutionKind.Enumerator));
        }

        [Test]
        public void ParameterStatesInitializeWithDefaults()
        {
            SampleTarget asset = ScriptableObject.CreateInstance<SampleTarget>();
            try
            {
                IReadOnlyList<WButtonMethodMetadata> metadata = WButtonMetadataCache.GetMetadata(
                    typeof(SampleTarget)
                );
                WButtonMethodMetadata method = metadata.First(m =>
                    m.Method.Name == nameof(SampleTarget.MethodWithDefaults)
                );

                WButtonTargetState targetState = WButtonStateRepository.GetOrCreate(asset);
                WButtonMethodState methodState = targetState.GetOrCreateMethodState(method);
                Assert.That(methodState.Parameters.Length, Is.EqualTo(2));
                Assert.That(methodState.Parameters[0].CurrentValue, Is.EqualTo(7));
                Assert.That(methodState.Parameters[1].CurrentValue, Is.EqualTo("hello"));
            }
            finally
            {
                if (asset != null)
                {
                    UnityEngine.Object.DestroyImmediate(asset);
                }
            }
        }

        [Test]
        public void ResolvePriorityColorReturnsDefaultsAndCustomOverrides()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            SerializedObject serialized = new SerializedObject(settings);
            SerializedProperty palette = serialized.FindProperty("wbuttonPriorityColors");
            List<(string Priority, Color ButtonColor, Color TextColor)> originalEntries =
                new List<(string, Color, Color)>();
            for (int index = 0; index < palette.arraySize; index++)
            {
                SerializedProperty element = palette.GetArrayElementAtIndex(index);
                SerializedProperty priority = element.FindPropertyRelative("priority");
                SerializedProperty buttonColorProperty = element.FindPropertyRelative(
                    "buttonColor"
                );
                SerializedProperty textColorProperty = element.FindPropertyRelative("textColor");
                originalEntries.Add(
                    (
                        priority.stringValue,
                        buttonColorProperty.colorValue,
                        textColorProperty.colorValue
                    )
                );
            }

            string priorityKey = "Critical";
            Color expectedColor = new Color(0.85f, 0.2f, 0.2f);
            Color expectedTextColor = WButtonColorUtility.GetReadableTextColor(expectedColor);

            try
            {
                bool found = false;
                for (int index = 0; index < palette.arraySize; index++)
                {
                    SerializedProperty element = palette.GetArrayElementAtIndex(index);
                    SerializedProperty priority = element.FindPropertyRelative("priority");
                    if (
                        string.Equals(
                            priority.stringValue,
                            priorityKey,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        SerializedProperty buttonColorProperty = element.FindPropertyRelative(
                            "buttonColor"
                        );
                        buttonColorProperty.colorValue = expectedColor;
                        SerializedProperty textColorProperty = element.FindPropertyRelative(
                            "textColor"
                        );
                        textColorProperty.colorValue = expectedTextColor;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    palette.InsertArrayElementAtIndex(palette.arraySize);
                    SerializedProperty element = palette.GetArrayElementAtIndex(
                        palette.arraySize - 1
                    );
                    element.FindPropertyRelative("priority").stringValue = priorityKey;
                    element.FindPropertyRelative("buttonColor").colorValue = expectedColor;
                    element.FindPropertyRelative("textColor").colorValue = expectedTextColor;
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
                settings.SaveSettings();

                UnityHelpersSettings.WButtonPaletteEntry resolvedCustom =
                    UnityHelpersSettings.ResolveWButtonPalette(priorityKey);
                Assert.That(resolvedCustom.ButtonColor, Is.EqualTo(expectedColor));
                Assert.That(resolvedCustom.TextColor, Is.EqualTo(expectedTextColor));

                UnityHelpersSettings.WButtonPaletteEntry resolvedDefault =
                    UnityHelpersSettings.ResolveWButtonPalette(null);
                Assert.That(resolvedDefault.ButtonColor.a, Is.GreaterThan(0.9f));
                Assert.That(resolvedDefault.TextColor.a, Is.GreaterThan(0.9f));
            }
            finally
            {
                serialized.Update();
                palette.arraySize = 0;
                for (int index = 0; index < originalEntries.Count; index++)
                {
                    palette.InsertArrayElementAtIndex(index);
                    SerializedProperty element = palette.GetArrayElementAtIndex(index);
                    element.FindPropertyRelative("priority").stringValue = originalEntries[
                        index
                    ].Priority;
                    element.FindPropertyRelative("buttonColor").colorValue = originalEntries[
                        index
                    ].ButtonColor;
                    element.FindPropertyRelative("textColor").colorValue = originalEntries[
                        index
                    ].TextColor;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                settings.SaveSettings();
            }
        }

        private sealed class SampleTarget : ScriptableObject
        {
            [WButton(drawOrder: 2)]
            public void NoParamsVoid() { }

            [WButton(drawOrder: 5)]
            public async Task<int> TaskMethodAsync(int value)
            {
                await Task.Delay(10);
                return value;
            }

            [WButton(drawOrder: -2)]
            public IEnumerator<object> EnumeratorMethod()
            {
                yield return null;
            }

            [WButton]
            public void MethodWithCancellation(CancellationToken cancellationToken) { }

            [WButton]
            public void MethodWithDefaults(int count = 7, string label = "hello") { }

            [WButton(priority: "Critical")]
            public void PriorityMethod() { }
        }
    }
}
#endif
