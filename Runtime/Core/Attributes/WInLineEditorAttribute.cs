namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Draws a referenced Unity object inline in the inspector with optional foldout, header, and preview support.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class WInLineEditorAttribute : PropertyAttribute
    {
        /// <summary>
        /// Determines how the nested inspector renders inside the drawer.
        /// </summary>
        public readonly WInLineEditorMode mode;

        /// <summary>
        /// Gets a value indicating whether the standard object field should remain visible.
        /// </summary>
        public readonly bool drawObjectField;

        /// <summary>
        /// Gets a value indicating whether the nested inspector should draw its header row.
        /// </summary>
        public readonly bool drawHeader;

        /// <summary>
        /// Gets a value indicating whether a preview texture is rendered below the inspector.
        /// </summary>
        public readonly bool drawPreview;

        /// <summary>
        /// Gets the height allocated to the preview texture when enabled.
        /// </summary>
        public readonly float previewHeight;

        /// <summary>
        /// Gets the height reserved for the nested inspector content area.
        /// </summary>
        public readonly float inspectorHeight;

        /// <summary>
        /// Gets a value indicating whether the nested inspector area uses a scroll view.
        /// </summary>
        public readonly bool enableScrolling;

        /// <summary>
        /// Gets the minimum content width the inline inspector should reserve before enabling horizontal scrolling.
        /// </summary>
        public readonly float minInspectorWidth;

        /// <summary>
        /// Configures inline inspector rendering for a referenced Unity object.
        /// </summary>
        /// <param name="mode">How the nested inspector should expand or collapse.</param>
        /// <param name="inspectorHeight">Height allocated to the inspector region.</param>
        /// <param name="drawObjectField">Whether to render the regular object field alongside the inline UI.</param>
        /// <param name="drawHeader">Whether to display the inspector header.</param>
        /// <param name="drawPreview">Whether to draw a preview texture, if available.</param>
        /// <param name="previewHeight">Height allocated to the preview texture.</param>
        /// <param name="enableScrolling">Whether to wrap the inspector region in a scroll view.</param>
        /// <param name="minInspectorWidth">Minimum content width before the inline inspector enables horizontal scrolling.</param>
        /// <example>
        /// <code>
        /// [WInLineEditor(WInLineEditorMode.FoldoutExpanded, inspectorHeight: 180f, drawPreview: true)]
        /// public ScriptableObject settings;
        /// </code>
        /// </example>
        public WInLineEditorAttribute(
            WInLineEditorMode mode = WInLineEditorMode.FoldoutExpanded,
            float inspectorHeight = 200f,
            bool drawObjectField = true,
            bool drawHeader = true,
            bool drawPreview = false,
            float previewHeight = 96f,
            bool enableScrolling = true,
            float minInspectorWidth = 520f
        )
        {
            this.mode = mode;
            this.drawObjectField = drawObjectField;
            this.drawHeader = drawHeader;
            this.drawPreview = drawPreview;
            this.previewHeight = previewHeight < 0f ? 0f : previewHeight;
            this.inspectorHeight = inspectorHeight < 160f ? 160f : inspectorHeight;
            this.enableScrolling = enableScrolling;
            this.minInspectorWidth = minInspectorWidth < 0f ? 0f : minInspectorWidth;
        }
    }

    public enum WInLineEditorMode
    {
        /// <summary>
        /// Deprecated placeholder; use one of the concrete options instead.
        /// </summary>
        [Obsolete("Please use a valid value")]
        None = 0,

        /// <summary>
        /// Always renders the nested inspector content without any foldout affordance.
        /// </summary>
        AlwaysExpanded = 1,

        /// <summary>
        /// Shows the nested inspector inside an expanded foldout by default.
        /// </summary>
        FoldoutExpanded = 2,

        /// <summary>
        /// Shows the nested inspector inside a collapsed foldout by default.
        /// </summary>
        FoldoutCollapsed = 3,
    }
}
