#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
{
    using System;
    using System.Buffers.Binary;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Core.Extension;

    [CustomPropertyDrawer(typeof(WGuid))]
    public sealed class WGuidPropertyDrawer : PropertyDrawer
    {
        internal sealed class DrawerState
        {
            public string displayText = string.Empty;
            public string serializedText = string.Empty;
            public bool hasPendingInvalid;
            public string warningMessage = string.Empty;
        }

        private const float ButtonWidth = 24f;
        private const string ClearUndoLabel = nameof(WGuid) + ".Clear";
        private const string SetUndoLabel = nameof(WGuid) + ".Set";
        private const string GenerateUndoLabel = nameof(WGuid) + ".Generate";

        private static readonly Dictionary<string, DrawerState> States = new();
        private static readonly GUIContent GenerateContent = CreateGenerateContent();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            DrawerState state = GetState(property);
            float lineHeight = EditorGUIUtility.singleLineHeight;
            if (!state.hasPendingInvalid || string.IsNullOrEmpty(state.warningMessage))
            {
                return lineHeight;
            }

            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float warningWidth = GetWarningWidth();
            GUIContent warningContent = new(state.warningMessage);
            float warningHeight = EditorStyles.helpBox.CalcHeight(warningContent, warningWidth);
            return lineHeight + spacing + warningHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty lowProperty = property.FindPropertyRelative(WGuid.LowFieldName);
            SerializedProperty highProperty = property.FindPropertyRelative(WGuid.HighFieldName);
            if (lowProperty == null || highProperty == null)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            DrawerState state = GetState(property);
            string serializedText = ConvertToString(lowProperty.longValue, highProperty.longValue);
            if (!state.hasPendingInvalid || string.IsNullOrEmpty(state.displayText))
            {
                if (!string.Equals(state.displayText, serializedText, StringComparison.Ordinal))
                {
                    state.displayText = serializedText;
                }

                state.serializedText = serializedText;
                state.hasPendingInvalid = false;
                state.warningMessage = string.Empty;
            }
            else if (!string.Equals(state.serializedText, serializedText, StringComparison.Ordinal))
            {
                state.serializedText = serializedText;
                state.displayText = serializedText;
                state.hasPendingInvalid = false;
                state.warningMessage = string.Empty;
            }

            EditorGUI.BeginProperty(position, label, property);
            int previousIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            Rect contentRect = EditorGUI.PrefixLabel(position, label);
            float buttonX = contentRect.x + Mathf.Max(0f, contentRect.width - ButtonWidth);
            Rect buttonRect = new(buttonX, contentRect.y, ButtonWidth, lineHeight);
            float textWidth = buttonRect.x - contentRect.x - spacing;
            if (textWidth < 0f)
            {
                textWidth = 0f;
            }

            Rect textRect = new(contentRect.x, contentRect.y, textWidth, lineHeight);

            EditorGUI.BeginChangeCheck();
            string incoming = EditorGUI.DelayedTextField(
                textRect,
                state.displayText ?? string.Empty
            );
            if (EditorGUI.EndChangeCheck())
            {
                HandleTextChange(property, lowProperty, highProperty, state, incoming);
            }

            if (GUI.Button(buttonRect, GenerateContent, EditorStyles.miniButton))
            {
                GenerateNewGuid(property, lowProperty, highProperty, state);
            }

            if (state.hasPendingInvalid && !string.IsNullOrEmpty(state.warningMessage))
            {
                Rect helpRect = new(
                    position.x,
                    position.y + lineHeight + spacing,
                    position.width,
                    EditorStyles.helpBox.CalcHeight(
                        new GUIContent(state.warningMessage),
                        position.width
                    )
                );
                EditorGUI.HelpBox(helpRect, state.warningMessage, MessageType.Warning);
            }

            EditorGUI.indentLevel = previousIndent;
            EditorGUI.EndProperty();
        }

        internal static void HandleTextChange(
            SerializedProperty property,
            SerializedProperty lowProperty,
            SerializedProperty highProperty,
            DrawerState state,
            string incoming
        )
        {
            string normalizedInput = incoming ?? string.Empty;
            string trimmed = normalizedInput.Trim();
            state.displayText = normalizedInput;

            if (string.IsNullOrEmpty(trimmed))
            {
                WGuid empty = WGuid.EmptyGuid;
                UpdateGuidValue(property, lowProperty, highProperty, empty, ClearUndoLabel);
                string serializedText = ConvertToString(
                    lowProperty.longValue,
                    highProperty.longValue
                );
                state.displayText = serializedText;
                state.serializedText = serializedText;
                state.hasPendingInvalid = false;
                state.warningMessage = string.Empty;
                return;
            }

            if (!System.Guid.TryParse(trimmed, out _))
            {
                state.hasPendingInvalid = true;
                state.warningMessage = $"Enter a valid {nameof(System.Guid)} string.";
                return;
            }

            if (!WGuid.TryParse(trimmed, out WGuid parsed))
            {
                state.hasPendingInvalid = true;
                state.warningMessage =
                    $"{nameof(WGuid)} expects a version 4 {nameof(System.Guid)}.";
                return;
            }

            UpdateGuidValue(property, lowProperty, highProperty, parsed, SetUndoLabel);
            string normalized = parsed.ToString();
            state.displayText = normalized;
            state.serializedText = normalized;
            state.hasPendingInvalid = false;
            state.warningMessage = string.Empty;
        }

        internal static void GenerateNewGuid(
            SerializedProperty property,
            SerializedProperty lowProperty,
            SerializedProperty highProperty,
            DrawerState state
        )
        {
            WGuid generated = WGuid.NewGuid();
            UpdateGuidValue(property, lowProperty, highProperty, generated, GenerateUndoLabel);
            string normalized = generated.ToString();
            state.displayText = normalized;
            state.serializedText = normalized;
            state.hasPendingInvalid = false;
            state.warningMessage = string.Empty;
        }

        private static void UpdateGuidValue(
            SerializedProperty property,
            SerializedProperty lowProperty,
            SerializedProperty highProperty,
            WGuid value,
            string undoLabel
        )
        {
            SerializedObject serializedObject = property.serializedObject;
            UnityEngine.Object[] targets = serializedObject.targetObjects;
            if (targets is { Length: > 0 })
            {
                Undo.RecordObjects(targets, undoLabel);
            }

            Span<byte> buffer = stackalloc byte[16];
            bool success = value.TryWriteBytes(buffer);
            if (!success)
            {
                throw new InvalidOperationException($"Failed to write {nameof(WGuid)} bytes.");
            }

            ulong low = BinaryPrimitives.ReadUInt64LittleEndian(buffer.Slice(0, 8));
            ulong high = BinaryPrimitives.ReadUInt64LittleEndian(buffer.Slice(8, 8));
            lowProperty.longValue = unchecked((long)low);
            highProperty.longValue = unchecked((long)high);

            serializedObject.ApplyModifiedProperties();
            serializedObject.UpdateIfRequiredOrScript();
        }

        internal static string ConvertToString(long low, long high)
        {
            Span<byte> buffer = stackalloc byte[16];
            ulong lowUnsigned = unchecked((ulong)low);
            ulong highUnsigned = unchecked((ulong)high);
            BinaryPrimitives.WriteUInt64LittleEndian(buffer.Slice(0, 8), lowUnsigned);
            BinaryPrimitives.WriteUInt64LittleEndian(buffer.Slice(8, 8), highUnsigned);
            Guid guid = new(buffer);
            return guid.ToString();
        }

        internal static DrawerState GetState(SerializedProperty property)
        {
            string key = property.propertyPath;
            return States.GetOrAdd(key);
        }

        internal static void ClearCachedStates()
        {
            States.Clear();
        }

        private static GUIContent CreateGenerateContent()
        {
            string tooltip = $"Generate a new {nameof(WGuid)} (v4).";
            GUIContent content = EditorGUIUtility.IconContent("Refresh", tooltip);
            if (content == null)
            {
                return new GUIContent("New", tooltip);
            }

            if (content.image == null && string.IsNullOrEmpty(content.text))
            {
                content.text = "New";
            }

            if (string.IsNullOrEmpty(content.tooltip))
            {
                content.tooltip = tooltip;
            }

            return content;
        }

        private static float GetWarningWidth()
        {
            try
            {
                return EditorGUIUtility.currentViewWidth;
            }
            catch (ArgumentException)
            {
                return 400f;
            }
        }
    }
}
#endif
