namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers
{
#if UNITY_EDITOR
    using System;
    using NUnit.Framework;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers;
    using WallstopStudios.UnityHelpers.Tests.Utils;
    using PropertyAttribute = UnityEngine.PropertyAttribute;

    [TestFixture]
    public sealed class StringInListDrawerTests : CommonTestBase
    {
        [Test]
        public void CreatePropertyGUIWithoutOptionsReturnsHelpBox()
        {
            NoOptionsAsset asset = CreateScriptableObject<NoOptionsAsset>();
            using SerializedObject serializedObject = new SerializedObject(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(NoOptionsAsset.unspecified)
            );
            Assert.IsNotNull(property, "Failed to locate string property.");

            StringInListDrawer drawer = new();
            AssignAttribute(drawer, new StringInListAttribute());
            VisualElement element = drawer.CreatePropertyGUI(property);
            Assert.IsInstanceOf<HelpBox>(element);
        }

        [Test]
        public void SelectorUpdatesStringSerializedProperty()
        {
            StringOptionsAsset asset = CreateScriptableObject<StringOptionsAsset>();
            asset.state = "Run";
            using SerializedObject serializedObject = new SerializedObject(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(StringOptionsAsset.state)
            );
            Assert.IsNotNull(property, "Failed to locate state property.");

            StringInListDrawer drawer = new();
            AssignAttribute(drawer, new StringInListAttribute("Idle", "Run", "Jump"));
            VisualElement element = drawer.CreatePropertyGUI(property);
            Assert.IsInstanceOf<BaseField<string>>(element);

            BaseField<string> selector = (BaseField<string>)element;
            DropdownField dropdown = selector.Q<DropdownField>();
            Assert.IsNotNull(dropdown, "Dropdown field was not created.");
            Assert.That(dropdown.value, Is.EqualTo("Run"));

            InvokeApplySelection(selector, 2);
            serializedObject.Update();
            Assert.That(asset.state, Is.EqualTo("Jump"));
        }

        [Test]
        public void SelectorWritesSelectedIndexToIntegerProperty()
        {
            IntegerOptionsAsset asset = CreateScriptableObject<IntegerOptionsAsset>();
            asset.selection = 0;
            using SerializedObject serializedObject = new SerializedObject(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(IntegerOptionsAsset.selection)
            );
            Assert.IsNotNull(property, "Failed to locate integer-backed dropdown.");

            StringInListDrawer drawer = new();
            AssignAttribute(drawer, new StringInListAttribute("Low", "Medium", "High"));
            VisualElement element = drawer.CreatePropertyGUI(property);
            DropdownField dropdown = element.Q<DropdownField>();
            Assert.IsNotNull(dropdown, "Dropdown field was not created.");

            InvokeApplySelection((BaseField<string>)element, 2);
            serializedObject.Update();
            Assert.That(asset.selection, Is.EqualTo(2));
        }

        [Test]
        public void PopupChromeIncludesFooterPadding()
        {
            float chrome = StringInListDrawer.TestHooks.CalculatePopupChromeHeight(
                includePagination: true
            );
            float searchHeight = EditorGUIUtility.singleLineHeight;
            float paginationHeight = StringInListDrawer.TestHooks.PaginationButtonHeight;
            float footerHeight = chrome - (searchHeight + paginationHeight);
            float expectedFooterHeight =
                EditorGUIUtility.standardVerticalSpacing
                + StringInListDrawer.TestHooks.OptionFooterPadding;
            Assert.That(footerHeight, Is.EqualTo(expectedFooterHeight).Within(0.001f));
        }

        [Test]
        public void PopupChromeAddsPaginationHeightWhenRequired()
        {
            float withPagination = StringInListDrawer.TestHooks.CalculatePopupChromeHeight(
                includePagination: true
            );
            float withoutPagination = StringInListDrawer.TestHooks.CalculatePopupChromeHeight(
                includePagination: false
            );
            float difference = withPagination - withoutPagination;
            float expectedDifference =
                StringInListDrawer.TestHooks.PaginationButtonHeight
                - EditorGUIUtility.standardVerticalSpacing;
            Assert.That(difference, Is.EqualTo(expectedDifference).Within(0.001f));
        }

        [Test]
        public void PopupTargetHeightAggregatesChromeAndRows()
        {
            const int pageSize = 10;
            float chrome = StringInListDrawer.TestHooks.CalculatePopupChromeHeight(
                includePagination: false
            );
            float rowHeight = StringInListDrawer.TestHooks.GetOptionRowHeight();
            float expected = chrome + (pageSize * rowHeight);
            float actual = StringInListDrawer.TestHooks.CalculatePopupTargetHeight(
                pageSize,
                includePagination: false
            );
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void PopupTargetHeightFitsSingleRowWithoutMinimumClamp()
        {
            float chrome = StringInListDrawer.TestHooks.CalculatePopupChromeHeight(
                includePagination: false
            );
            float rowHeight = StringInListDrawer.TestHooks.GetOptionRowHeight();
            float expected = chrome + rowHeight;
            float actual = StringInListDrawer.TestHooks.CalculatePopupTargetHeight(
                1,
                includePagination: false
            );
            Assert.That(actual, Is.EqualTo(expected).Within(0.001f));
        }

        [Test]
        public void PopupTargetHeightScalesWithLargePageSizes()
        {
            float twentyFiveRows = StringInListDrawer.TestHooks.CalculatePopupTargetHeight(
                25,
                includePagination: true
            );
            float fiftyRows = StringInListDrawer.TestHooks.CalculatePopupTargetHeight(
                50,
                includePagination: true
            );
            float rowHeight = StringInListDrawer.TestHooks.GetOptionRowHeight();
            Assert.That(fiftyRows - twentyFiveRows, Is.EqualTo(25 * rowHeight).Within(0.001f));
        }

        [Test]
        public void PopupTargetHeightTreatsNonPositivePageSizesAsSingleRow()
        {
            float baseline = StringInListDrawer.TestHooks.CalculatePopupTargetHeight(
                1,
                includePagination: false
            );
            float zero = StringInListDrawer.TestHooks.CalculatePopupTargetHeight(
                0,
                includePagination: false
            );
            float negative = StringInListDrawer.TestHooks.CalculatePopupTargetHeight(
                -5,
                includePagination: false
            );
            Assert.That(zero, Is.EqualTo(baseline));
            Assert.That(negative, Is.EqualTo(baseline));
        }

        [Test]
        public void OptionRowHeightMatchesControlPlusEffectiveMargin()
        {
            float control = StringInListDrawer.TestHooks.GetOptionControlHeight();
            int marginVertical = StringInListDrawer.TestHooks.OptionButtonMarginVertical;
            float expected =
                control + Mathf.Max(0f, marginVertical - EditorGUIUtility.standardVerticalSpacing);
            float actual = StringInListDrawer.TestHooks.GetOptionRowHeight();
            Assert.That(actual, Is.EqualTo(expected).Within(0.001f));
        }

        [Test]
        public void EmptySearchHeightLeavesRoomForHelpBox()
        {
            float emptyHeight = StringInListDrawer.TestHooks.CalculateEmptySearchHeight();
            GUIStyle helpStyle = EditorStyles.helpBox;
            int helpMargin = helpStyle.margin?.horizontal ?? 0;
            float helpWidth =
                StringInListDrawer.TestHooks.PopupWidthValue
                - StringInListDrawer.TestHooks.EmptySearchHorizontalPaddingValue
                - helpMargin;
            helpWidth = Mathf.Max(32f, helpWidth);
            float helpHeight =
                helpStyle.CalcHeight(
                    new GUIContent(StringInListDrawer.TestHooks.EmptyResultsMessageValue),
                    helpWidth
                ) + (helpStyle.margin?.vertical ?? 0);
            // Production code uses:
            // searchRow (singleLineHeight + standardVerticalSpacing) + topSpacer + helpBoxHeight
            // + bottomSpacer + footer (standardVerticalSpacing + OptionBottomPadding + EmptySearchExtraPadding)
            // = singleLineHeight + 4*standardVerticalSpacing + helpBoxHeight + OptionBottomPadding + EmptySearchExtraPadding
            float expected =
                EditorGUIUtility.singleLineHeight
                + (EditorGUIUtility.standardVerticalSpacing * 4f)
                + helpHeight
                + StringInListDrawer.TestHooks.OptionFooterPadding
                + StringInListDrawer.TestHooks.EmptySearchExtraPaddingValue;
            Assert.That(emptyHeight, Is.EqualTo(expected).Within(0.001f));
        }

        [Test]
        public void EmptySearchHeightPrefersMeasuredHelpBoxHeight()
        {
            const float measuredHelpHeight = 42f;
            float emptyHeight =
                StringInListDrawer.TestHooks.CalculateEmptySearchHeightWithMeasurement(
                    measuredHelpHeight
                );

            // Same formula as CalculateEmptySearchHeight but with measured height
            float expected =
                EditorGUIUtility.singleLineHeight
                + (EditorGUIUtility.standardVerticalSpacing * 4f)
                + measuredHelpHeight
                + StringInListDrawer.TestHooks.OptionFooterPadding
                + StringInListDrawer.TestHooks.EmptySearchExtraPaddingValue;

            Assert.That(emptyHeight, Is.EqualTo(expected).Within(0.001f));
        }

        // Data-driven test for CalculateRowsOnPage covering various scenarios
        [TestCase(12, 5, 5, 2, TestName = "CalculateRowsOnPage_LastPage_Returns2")]
        [TestCase(0, 5, 0, 1, TestName = "CalculateRowsOnPage_EmptyList_Returns1")]
        [TestCase(6, 0, 0, 1, TestName = "CalculateRowsOnPage_ZeroPageSize_Returns1")]
        [TestCase(3, 2, -2, 2, TestName = "CalculateRowsOnPage_NegativePage_ClampsToFirst")]
        [TestCase(10, 5, 0, 5, TestName = "CalculateRowsOnPage_FirstFullPage_ReturnsPageSize")]
        [TestCase(10, 5, 1, 5, TestName = "CalculateRowsOnPage_SecondFullPage_ReturnsPageSize")]
        [TestCase(7, 5, 1, 2, TestName = "CalculateRowsOnPage_PartialLastPage_ReturnsRemaining")]
        [TestCase(5, 5, 0, 5, TestName = "CalculateRowsOnPage_ExactlyOnePage_ReturnsPageSize")]
        [TestCase(1, 10, 0, 1, TestName = "CalculateRowsOnPage_SingleItem_Returns1")]
        [TestCase(
            100,
            10,
            9,
            10,
            TestName = "CalculateRowsOnPage_LastPageExactFit_ReturnsPageSize"
        )]
        public void CalculateRowsOnPage_DataDrivenScenarios(
            int filteredCount,
            int pageSize,
            int currentPage,
            int expectedRows
        )
        {
            int rows = StringInListDrawer.TestHooks.CalculateRowsOnPage(
                filteredCount: filteredCount,
                pageSize: pageSize,
                currentPage: currentPage
            );
            Assert.That(
                rows,
                Is.EqualTo(expectedRows),
                $"CalculateRowsOnPage({filteredCount}, {pageSize}, {currentPage}) should return {expectedRows} but got {rows}"
            );
        }

        [Serializable]
        private sealed class NoOptionsAsset : ScriptableObject
        {
            [StringInList]
            public string unspecified = string.Empty;
        }

        [Serializable]
        private sealed class StringOptionsAsset : ScriptableObject
        {
            [StringInList("Idle", "Run", "Jump")]
            public string state = "Idle";
        }

        [Serializable]
        private sealed class IntegerOptionsAsset : ScriptableObject
        {
            [StringInList("Low", "Medium", "High")]
            public int selection;
        }

        private static void AssignAttribute(PropertyDrawer drawer, PropertyAttribute attribute)
        {
            FieldInfo attributeField = typeof(PropertyDrawer).GetField(
                "m_Attribute",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            Assert.IsNotNull(attributeField, "Unable to locate PropertyDrawer.m_Attribute.");
            attributeField.SetValue(drawer, attribute);
        }

        private static void InvokeApplySelection(BaseField<string> selector, int optionIndex)
        {
            MethodInfo method = selector
                .GetType()
                .GetMethod("ApplySelection", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "Unable to locate ApplySelection on selector.");
            method.Invoke(selector, new object[] { optionIndex });
        }
    }
#endif
}
