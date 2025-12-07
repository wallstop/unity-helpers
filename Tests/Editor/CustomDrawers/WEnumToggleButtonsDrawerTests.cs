namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers
{
    using System;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers;
    using WallstopStudios.UnityHelpers.Tests.Core;
    using WallstopStudios.UnityHelpers.Tests.Editor.TestTypes;

    [TestFixture]
    public sealed class WEnumToggleButtonsDrawerTests : CommonTestBase
    {
        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            WEnumToggleButtonsPagination.Reset();
        }

        [Test]
        public void FlagEnumOptionsIncludeDiscreteValues()
        {
            ToggleTestAsset asset = CreateScriptableObject<ToggleTestAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(ToggleTestAsset.flags)
            );
            Assert.NotNull(property);

            ToggleSet toggleSet = WEnumToggleButtonsUtility.CreateToggleSet(
                property,
                GetFieldInfo(nameof(ToggleTestAsset.flags))
            );
            Assert.IsFalse(toggleSet.IsEmpty);
            Assert.True(toggleSet.SupportsMultipleSelection);
            Assert.AreEqual(4, toggleSet.Options.Count);

            bool seenMove = false;
            bool seenJump = false;
            bool seenDash = false;
            for (int index = 0; index < toggleSet.Options.Count; index += 1)
            {
                ToggleOption option = toggleSet.Options[index];
                if (option.FlagValue == Convert.ToUInt64(ToggleTestAsset.ExampleFlags.Move))
                {
                    seenMove = true;
                }
                else if (option.FlagValue == Convert.ToUInt64(ToggleTestAsset.ExampleFlags.Jump))
                {
                    seenJump = true;
                }
                else if (option.FlagValue == Convert.ToUInt64(ToggleTestAsset.ExampleFlags.Dash))
                {
                    seenDash = true;
                }
            }

            Assert.True(seenMove);
            Assert.True(seenJump);
            Assert.True(seenDash);
        }

        [Test]
        public void FlagEnumToggleMutatesMask()
        {
            ToggleTestAsset asset = CreateScriptableObject<ToggleTestAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(ToggleTestAsset.flags)
            );
            Assert.NotNull(property);

            ToggleSet toggleSet = WEnumToggleButtonsUtility.CreateToggleSet(
                property,
                GetFieldInfo(nameof(ToggleTestAsset.flags))
            );

            ToggleOption moveOption = GetFlagOption(toggleSet, ToggleTestAsset.ExampleFlags.Move);

            WEnumToggleButtonsUtility.ApplyOption(property, toggleSet, moveOption, true);
            serializedObject.ApplyModifiedProperties();
            Assert.AreEqual(ToggleTestAsset.ExampleFlags.Move, asset.flags);

            WEnumToggleButtonsUtility.ApplyOption(property, toggleSet, moveOption, false);
            serializedObject.ApplyModifiedProperties();
            Assert.AreEqual(ToggleTestAsset.ExampleFlags.None, asset.flags);
        }

        [Test]
        public void FlagEnumSelectAllAndNoneOperate()
        {
            ToggleTestAsset asset = CreateScriptableObject<ToggleTestAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(ToggleTestAsset.flags)
            );
            Assert.NotNull(property);

            ToggleSet toggleSet = WEnumToggleButtonsUtility.CreateToggleSet(
                property,
                GetFieldInfo(nameof(ToggleTestAsset.flags))
            );

            WEnumToggleButtonsUtility.ApplySelectAll(property, toggleSet);
            serializedObject.ApplyModifiedProperties();
            Assert.True(
                WEnumToggleButtonsUtility.AreAllFlagsSelected(property, toggleSet),
                "All flags should be active after ApplySelectAll."
            );
            Assert.AreEqual(
                ToggleTestAsset.ExampleFlags.Move
                    | ToggleTestAsset.ExampleFlags.Jump
                    | ToggleTestAsset.ExampleFlags.Dash,
                asset.flags
            );

            WEnumToggleButtonsUtility.ApplySelectNone(property);
            serializedObject.ApplyModifiedProperties();
            Assert.True(
                WEnumToggleButtonsUtility.AreNoFlagsSelected(property),
                "No flags should be active after ApplySelectNone."
            );
            Assert.AreEqual(ToggleTestAsset.ExampleFlags.None, asset.flags);
        }

        [Test]
        public void StandardEnumHonorsSingleSelection()
        {
            ToggleTestAsset asset = CreateScriptableObject<ToggleTestAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(ToggleTestAsset.mode)
            );
            Assert.NotNull(property);

            ToggleSet toggleSet = WEnumToggleButtonsUtility.CreateToggleSet(
                property,
                GetFieldInfo(nameof(ToggleTestAsset.mode))
            );

            Assert.False(toggleSet.SupportsMultipleSelection);
            Assert.AreEqual(3, toggleSet.Options.Count);

            ToggleOption secondOption = GetEnumOption(
                toggleSet,
                ToggleTestAsset.ExampleEnum.Second
            );

            WEnumToggleButtonsUtility.ApplyOption(property, toggleSet, secondOption, true);
            serializedObject.ApplyModifiedProperties();
            Assert.AreEqual(ToggleTestAsset.ExampleEnum.Second, asset.mode);

            ToggleOption thirdOption = GetEnumOption(toggleSet, ToggleTestAsset.ExampleEnum.Third);

            WEnumToggleButtonsUtility.ApplyOption(property, toggleSet, thirdOption, true);
            serializedObject.ApplyModifiedProperties();
            Assert.AreEqual(ToggleTestAsset.ExampleEnum.Third, asset.mode);
        }

        [Test]
        public void IntDropdownOptionsRespectSelection()
        {
            ToggleTestAsset asset = CreateScriptableObject<ToggleTestAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(ToggleTestAsset.intSelection)
            );
            Assert.NotNull(property);

            ToggleSet toggleSet = WEnumToggleButtonsUtility.CreateToggleSet(
                property,
                GetFieldInfo(nameof(ToggleTestAsset.intSelection))
            );

            Assert.False(toggleSet.SupportsMultipleSelection);
            Assert.AreEqual(3, toggleSet.Options.Count);

            ToggleOption desiredOption = GetOptionByLabel(toggleSet, "60");
            WEnumToggleButtonsUtility.ApplyOption(property, toggleSet, desiredOption, true);
            serializedObject.ApplyModifiedProperties();
            Assert.AreEqual(60, asset.intSelection);
        }

        [Test]
        public void StringInListOptionsRespectSelection()
        {
            ToggleTestAsset asset = CreateScriptableObject<ToggleTestAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(ToggleTestAsset.stateName)
            );
            Assert.NotNull(property);

            ToggleSet toggleSet = WEnumToggleButtonsUtility.CreateToggleSet(
                property,
                GetFieldInfo(nameof(ToggleTestAsset.stateName))
            );

            Assert.False(toggleSet.SupportsMultipleSelection);
            Assert.AreEqual(3, toggleSet.Options.Count);

            ToggleOption runOption = GetOptionByLabel(toggleSet, "Run");
            WEnumToggleButtonsUtility.ApplyOption(property, toggleSet, runOption, true);
            serializedObject.ApplyModifiedProperties();
            Assert.AreEqual("Run", asset.stateName);
        }

        [Test]
        public void WValueDropDownOptionsPopulateAndSelect()
        {
            ToggleTestAsset asset = CreateScriptableObject<ToggleTestAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(ToggleTestAsset.priority)
            );
            Assert.NotNull(property);

            ToggleSet toggleSet = WEnumToggleButtonsUtility.CreateToggleSet(
                property,
                GetFieldInfo(nameof(ToggleTestAsset.priority))
            );

            Assert.False(toggleSet.SupportsMultipleSelection);
            Assert.AreEqual(3, toggleSet.Options.Count);

            ToggleOption highOption = GetOptionByLabel(toggleSet, "3");
            WEnumToggleButtonsUtility.ApplyOption(property, toggleSet, highOption, true);
            serializedObject.ApplyModifiedProperties();
            Assert.AreEqual(3, asset.priority);
        }

        [Test]
        public void FloatWValueDropDownOptionsRespectSelection()
        {
            ToggleTestAsset asset = CreateScriptableObject<ToggleTestAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(ToggleTestAsset.floatPriority)
            );
            Assert.NotNull(property);

            ToggleSet toggleSet = WEnumToggleButtonsUtility.CreateToggleSet(
                property,
                GetFieldInfo(nameof(ToggleTestAsset.floatPriority))
            );

            Assert.False(toggleSet.SupportsMultipleSelection);
            Assert.AreEqual(3, toggleSet.Options.Count);

            ToggleOption mediumOption = GetOptionByLabel(toggleSet, "1.5");
            WEnumToggleButtonsUtility.ApplyOption(property, toggleSet, mediumOption, true);
            serializedObject.ApplyModifiedProperties();
            Assert.That(asset.floatPriority, Is.EqualTo(1.5f).Within(0.0001f));
        }

        [Test]
        public void PaginationStateClampsIndicesAndUpdatesVisibleCount()
        {
            ToggleTestAsset asset = CreateScriptableObject<ToggleTestAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(ToggleTestAsset.paginatedInt)
            );
            Assert.NotNull(property);

            ToggleSet toggleSet = WEnumToggleButtonsUtility.CreateToggleSet(
                property,
                GetFieldInfo(nameof(ToggleTestAsset.paginatedInt))
            );

            FieldInfo fieldInfo = GetFieldInfo(nameof(ToggleTestAsset.paginatedInt));
            WEnumToggleButtonsAttribute attribute =
                ReflectionHelpers.GetAttributeSafe<WEnumToggleButtonsAttribute>(
                    fieldInfo,
                    inherit: true
                );
            Assert.NotNull(attribute);

            bool shouldPaginate = WEnumToggleButtonsUtility.ShouldPaginate(
                attribute,
                toggleSet.Options.Count,
                out int pageSize
            );
            Assert.True(shouldPaginate);
            Assert.AreEqual(attribute.PageSize, pageSize);

            WEnumToggleButtonsPagination.PaginationState state =
                WEnumToggleButtonsPagination.GetState(property, toggleSet.Options.Count, pageSize);

            Assert.AreEqual(2, state.TotalPages);
            Assert.AreEqual(pageSize, state.VisibleCount);
            Assert.AreEqual(0, state.StartIndex);

            state.PageIndex = 10;

            WEnumToggleButtonsPagination.PaginationState clamped =
                WEnumToggleButtonsPagination.GetState(property, toggleSet.Options.Count, pageSize);

            Assert.AreEqual(
                1,
                clamped.PageIndex,
                "Page index should clamp to last available page."
            );
            Assert.AreEqual(
                4,
                clamped.VisibleCount,
                "Last page should only show remaining entries."
            );
            Assert.AreEqual(6, clamped.StartIndex);
        }

        [Test]
        public void DisablePaginationAttributePreventsPagination()
        {
            ToggleTestAsset asset = CreateScriptableObject<ToggleTestAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(ToggleTestAsset.noPaginationState)
            );
            Assert.NotNull(property);

            FieldInfo fieldInfo = GetFieldInfo(nameof(ToggleTestAsset.noPaginationState));
            WEnumToggleButtonsAttribute attribute =
                ReflectionHelpers.GetAttributeSafe<WEnumToggleButtonsAttribute>(
                    fieldInfo,
                    inherit: true
                );
            Assert.NotNull(attribute);

            ToggleSet toggleSet = WEnumToggleButtonsUtility.CreateToggleSet(property, fieldInfo);
            bool shouldPaginate = WEnumToggleButtonsUtility.ShouldPaginate(
                attribute,
                toggleSet.Options.Count,
                out int pageSize
            );
            Assert.False(shouldPaginate);
            Assert.Greater(pageSize, 0);
        }

        [Test]
        public void SummaryCreatedForSelectionOutsideVisiblePage()
        {
            ToggleTestAsset asset = CreateScriptableObject<ToggleTestAsset>();
            asset.paginatedInt = 8;
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(ToggleTestAsset.paginatedInt)
            );
            ToggleSet toggleSet = WEnumToggleButtonsUtility.CreateToggleSet(
                property,
                GetFieldInfo(nameof(ToggleTestAsset.paginatedInt))
            );

            FieldInfo fieldInfo = GetFieldInfo(nameof(ToggleTestAsset.paginatedInt));
            WEnumToggleButtonsAttribute attribute =
                ReflectionHelpers.GetAttributeSafe<WEnumToggleButtonsAttribute>(
                    fieldInfo,
                    inherit: true
                );
            Assert.NotNull(attribute);

            bool usePagination = WEnumToggleButtonsUtility.ShouldPaginate(
                attribute,
                toggleSet.Options.Count,
                out int pageSize
            );
            Assert.True(usePagination);

            WEnumToggleButtonsPagination.PaginationState state =
                WEnumToggleButtonsPagination.GetState(property, toggleSet.Options.Count, pageSize);

            SummaryResult summary = InvokeSummary(
                toggleSet,
                property,
                state.StartIndex,
                state.VisibleCount,
                true
            );

            Assert.True(summary.HasSummary);
            Assert.That(summary.Content.text, Does.Contain("8"));
        }

        [Test]
        public void SummaryNotCreatedWhenSelectionInVisibleRange()
        {
            ToggleTestAsset asset = CreateScriptableObject<ToggleTestAsset>();
            asset.paginatedInt = 2;
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(ToggleTestAsset.paginatedInt)
            );
            ToggleSet toggleSet = WEnumToggleButtonsUtility.CreateToggleSet(
                property,
                GetFieldInfo(nameof(ToggleTestAsset.paginatedInt))
            );

            SummaryResult summary = InvokeSummary(toggleSet, property, 0, 6, true);
            Assert.False(summary.HasSummary);
        }

        [Test]
        public void SummaryNotCreatedWhenPaginationDisabled()
        {
            ToggleTestAsset asset = CreateScriptableObject<ToggleTestAsset>();
            asset.paginatedInt = 8;
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(ToggleTestAsset.paginatedInt)
            );
            ToggleSet toggleSet = WEnumToggleButtonsUtility.CreateToggleSet(
                property,
                GetFieldInfo(nameof(ToggleTestAsset.paginatedInt))
            );

            SummaryResult summary = InvokeSummary(
                toggleSet,
                property,
                0,
                toggleSet.Options.Count,
                false
            );

            Assert.False(summary.HasSummary);
        }

        private static SummaryResult InvokeSummary(
            ToggleSet toggleSet,
            SerializedProperty property,
            int startIndex,
            int visibleCount,
            bool usePagination
        )
        {
            WEnumToggleButtonsDrawer.SelectionSummary summary =
                WEnumToggleButtonsDrawer.BuildSelectionSummary(
                    toggleSet,
                    property,
                    startIndex,
                    visibleCount,
                    usePagination
                );
            return new SummaryResult(summary.HasSummary, summary.Content ?? GUIContent.none);
        }

        private static ToggleOption GetFlagOption(
            ToggleSet toggleSet,
            ToggleTestAsset.ExampleFlags flag
        )
        {
            ulong target = Convert.ToUInt64(flag);
            for (int index = 0; index < toggleSet.Options.Count; index += 1)
            {
                ToggleOption option = toggleSet.Options[index];
                if (option.FlagValue == target)
                {
                    return option;
                }
            }

            Assert.Fail("Expected flag option was not located.");
            return default;
        }

        private static ToggleOption GetEnumOption(
            ToggleSet toggleSet,
            ToggleTestAsset.ExampleEnum value
        )
        {
            for (int index = 0; index < toggleSet.Options.Count; index += 1)
            {
                ToggleOption option = toggleSet.Options[index];
                if (
                    option.Value is ToggleTestAsset.ExampleEnum enumValue
                    && enumValue.Equals(value)
                )
                {
                    return option;
                }
            }

            Assert.Fail("Expected enum option was not located.");
            return default;
        }

        private static ToggleOption GetOptionByLabel(ToggleSet toggleSet, string label)
        {
            for (int index = 0; index < toggleSet.Options.Count; index += 1)
            {
                ToggleOption option = toggleSet.Options[index];
                if (string.Equals(option.Label, label, StringComparison.Ordinal))
                {
                    return option;
                }
            }

            Assert.Fail("Expected label was not located: " + label);
            return default;
        }

        private static FieldInfo GetFieldInfo(string fieldName)
        {
            FieldInfo fieldInfo = typeof(ToggleTestAsset).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );
            Assert.NotNull(fieldInfo, "Unable to resolve field: " + fieldName);
            return fieldInfo;
        }

        private readonly struct SummaryResult
        {
            internal SummaryResult(bool hasSummary, GUIContent content)
            {
                HasSummary = hasSummary;
                Content = content;
            }

            internal bool HasSummary { get; }

            internal GUIContent Content { get; }
        }
    }
}
