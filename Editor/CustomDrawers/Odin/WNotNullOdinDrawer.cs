namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector.Editor;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers.Utils;

    /// <summary>
    /// Odin Inspector attribute drawer for <see cref="WNotNullAttribute"/>.
    /// Displays a warning or error HelpBox when the field value is null.
    /// </summary>
    /// <remarks>
    /// This drawer ensures WNotNull works correctly when Odin Inspector is installed
    /// and classes derive from SerializedMonoBehaviour or SerializedScriptableObject,
    /// where Unity's standard PropertyDrawer system is bypassed.
    /// </remarks>
    public sealed class WNotNullOdinDrawer : OdinAttributeDrawer<WNotNullAttribute>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            CallNextDrawer(label);

            object value = Property.ValueEntry?.WeakSmartValue;
            if (ValidationShared.IsValueNull(value))
            {
                string message = ValidationShared.GetNotNullMessage(Property.NiceName, Attribute);
                MessageType messageType = ValidationShared.GetMessageType(Attribute);
                EditorGUILayout.HelpBox(message, messageType);
            }
        }
    }
#endif
}
