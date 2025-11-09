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
        public readonly WInLineEditorMode mode;
        public readonly bool drawObjectField;
        public readonly bool drawHeader;
        public readonly bool drawPreview;
        public readonly float previewHeight;
        public readonly float inspectorHeight;
        public readonly bool enableScrolling;

        public WInLineEditorAttribute(
            WInLineEditorMode mode = WInLineEditorMode.FoldoutExpanded,
            float inspectorHeight = 200f,
            bool drawObjectField = true,
            bool drawHeader = true,
            bool drawPreview = false,
            float previewHeight = 96f,
            bool enableScrolling = true
        )
        {
            this.mode = mode;
            this.drawObjectField = drawObjectField;
            this.drawHeader = drawHeader;
            this.drawPreview = drawPreview;
            this.previewHeight = previewHeight < 0f ? 0f : previewHeight;
            this.inspectorHeight = inspectorHeight < 32f ? 32f : inspectorHeight;
            this.enableScrolling = enableScrolling;
        }
    }

    public enum WInLineEditorMode
    {
        AlwaysExpanded,
        FoldoutExpanded,
        FoldoutCollapsed,
    }
}
