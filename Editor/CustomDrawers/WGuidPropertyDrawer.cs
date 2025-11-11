#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
{
    using System;
    using System.Buffers.Binary;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

    [CustomPropertyDrawer(typeof(WGuid))]
    public sealed class WGuidPropertyDrawer : PropertyDrawer
    {
        private sealed class DrawerState
        {
            public string displayText = string.Empty;
            public string serializedText = string.Empty;
            public bool hasPendingInvalid;
            public string warningMessage = string.Empty;
        }

        private const float ButtonWidth = 24f;
        private static readonly Dictionary<string, DrawerState> States = new();
        private static readonly GUIContent GenerateContent = CreateGenerateContent();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            DrawerState state = GetState(property);
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float height = lineHeight;
            if (!state.hasPendingInvalid || string.IsNullOrEmpty(state.warningMessage))
            {
                return height;
            }

            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float warningWidth = EditorGUIUtility.currentViewWidth;
            GUIContent warningContent = new(state.warningMessage);
            float warningHeight = EditorStyles.helpBox.CalcHeight(warningContent, warningWidth);
            height += spacing + warningHeight;

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty lowProperty = property.FindPropertyRelative(nameof(WGuid._low));
            SerializedProperty highProperty = property.FindPropertyRelative(nameof(WGuid._high));
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

            if (textRect.width < 0)
            {
                textRect.width = 0;
            }

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

        private static void HandleTextChange(
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
                UpdateGuidValue(property, lowProperty, highProperty, empty, "Clear WGuid");
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

            if (!WGuid.TryParse(trimmed, out WGuid parsed))
            {
                state.hasPendingInvalid = true;
                state.warningMessage = "Enter a valid GUID string.";
                return;
            }

            if (!parsed.IsVersion4)
            {
                state.hasPendingInvalid = true;
                state.warningMessage = "WGuid expects a version 4 GUID.";
                return;
            }

            UpdateGuidValue(property, lowProperty, highProperty, parsed, "Set WGuid");
            string normalized = parsed.ToString();
            state.displayText = normalized;
            state.serializedText = normalized;
            state.hasPendingInvalid = false;
            state.warningMessage = string.Empty;
        }

        private static void GenerateNewGuid(
            SerializedProperty property,
            SerializedProperty lowProperty,
            SerializedProperty highProperty,
            DrawerState state
        )
        {
            WGuid generated = WGuid.NewGuid();
            UpdateGuidValue(property, lowProperty, highProperty, generated, "Generate WGuid");
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
            if (targets != null && targets.Length > 0)
            {
                Undo.RecordObjects(targets, undoLabel);
            }

            Span<byte> buffer = stackalloc byte[16];
            bool success = value.TryWriteBytes(buffer);
            if (!success)
            {
                throw new InvalidOperationException("Failed to write WGuid bytes.");
            }

            ulong low = BinaryPrimitives.ReadUInt64LittleEndian(buffer.Slice(0, 8));
            ulong high = BinaryPrimitives.ReadUInt64LittleEndian(buffer.Slice(8, 8));
            lowProperty.longValue = unchecked((long)low);
            highProperty.longValue = unchecked((long)high);

            serializedObject.ApplyModifiedProperties();
            serializedObject.UpdateIfRequiredOrScript();
        }

        private static string ConvertToString(long low, long high)
        {
            Span<byte> buffer = stackalloc byte[16];
            ulong lowUnsigned = unchecked((ulong)low);
            ulong highUnsigned = unchecked((ulong)high);
            BinaryPrimitives.WriteUInt64LittleEndian(buffer.Slice(0, 8), lowUnsigned);
            BinaryPrimitives.WriteUInt64LittleEndian(buffer.Slice(8, 8), highUnsigned);
            Guid guid = new(buffer);
            return guid.ToString();
        }

        private static DrawerState GetState(SerializedProperty property)
        {
            string key = property.propertyPath;
            if (!States.TryGetValue(key, out DrawerState state))
            {
                state = new DrawerState();
                States[key] = state;
            }

            return state;
        }

        private static GUIContent CreateGenerateContent()
        {
            GUIContent content = EditorGUIUtility.IconContent(
                "Refresh",
                "Generate a new WGuid (v4)."
            );
            if (content == null)
            {
                return new GUIContent("New", "Generate a new WGuid (v4).");
            }

            if (content.image == null && string.IsNullOrEmpty(content.text))
            {
                content.text = "New";
            }

            if (string.IsNullOrEmpty(content.tooltip))
            {
                content.tooltip = "Generate a new WGuid (v4).";
            }

            return content;
        }
    }
}
#endif
