namespace WallstopStudios.UnityHelpers.Editor.DataVisualizer.Components
{
    using System;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;

    namespace UnityHelpers.Editor // Use your editor script namespace
    {
        public class ConfirmActionPopup : EditorWindow
        {
            private string _message;
            private string _confirmButtonText;
            private string _cancelButtonText;
            private Action<bool> _onCompleteCallback; // Action<confirmed>
            private bool _callbackInvoked = false;
            private Rect _parentPosition; // Store parent window's position for centering
            private VisualElement _contentContainer; // Optional: A VE wrapping all your main content

            // Static method to create, configure, and return the window instance
            public static ConfirmActionPopup CreateAndConfigureInstance(
                string title,
                string message,
                string confirmButtonText,
                string cancelButtonText,
                Rect parentPosition,
                Action<bool> onComplete
            )
            {
                ConfirmActionPopup window = ScriptableObject.CreateInstance<ConfirmActionPopup>();
                window.titleContent = new GUIContent(title);
                window._message = message;
                window._confirmButtonText = confirmButtonText ?? "OK";
                window._cancelButtonText = cancelButtonText ?? "Cancel";
                window._onCompleteCallback = onComplete;
                window._parentPosition = parentPosition;
                return window;
            }

            public void CreateGUI()
            {
                VisualElement root = rootVisualElement;
                root.style.paddingBottom = 15; // Add some padding
                root.style.paddingTop = 15;
                root.style.paddingLeft = 15;
                root.style.paddingRight = 15;

                // Optional: Wrap main content in a single container for easier measurement
                _contentContainer = new VisualElement() { name = "popup-content-wrapper" };
                // Ensure wrapper doesn't grow unnecessarily (adjust as needed for your layout)
                _contentContainer.style.flexGrow = 0;
                _contentContainer.style.flexShrink = 0;
                _contentContainer.style.alignSelf = Align.FlexStart; // Prevent stretching

                root.Add(_contentContainer);
                // Message Label
                var messageLabel = new Label(_message)
                {
                    style =
                    {
                        whiteSpace = WhiteSpace.Normal, // Allow text wrapping
                        marginBottom = 20, // Space between message and buttons
                        fontSize = 12, // Adjust font size if desired
                    },
                };
                _contentContainer.Add(messageLabel);

                // Button Container
                var buttonContainer = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        justifyContent = Justify.FlexEnd, // Align buttons to the right
                    },
                };
                _contentContainer.Add(buttonContainer);

                // Cancel Button
                var cancelButton = new Button(() => ClosePopup(false))
                { // Pass false for cancel
                    text = _cancelButtonText,
                    style = { marginRight = 5 }, // Space between buttons
                };
                buttonContainer.Add(cancelButton);

                // Confirmation Button
                var confirmButton = new Button(() => ClosePopup(true))
                { // Pass true for confirm
                    text = _confirmButtonText,
                };
                // Optional: Add specific styling for confirmation/danger
                // confirmButton.AddToClassList("button-warning");
                // confirmButton.AddToClassList("button-danger");
                buttonContainer.Add(confirmButton);

                // Set initial focus on the cancel button maybe? Or confirm?
                _contentContainer.schedule.Execute(() => cancelButton.Focus()).ExecuteLater(50);
            }

            private void PositionAndResizeWindow()
            {
                // Use the wrapper if defined, otherwise fallback to root (less accurate)
                VisualElement measuredElement = _contentContainer ?? rootVisualElement;

                // Get the size the content *wants* to be after layout
                float contentWidth = measuredElement.resolvedStyle.width;
                float contentHeight = measuredElement.resolvedStyle.height;

                // Check if layout is ready (values are not NaN or zero)
                if (
                    float.IsNaN(contentWidth)
                    || float.IsNaN(contentHeight)
                    || contentWidth <= 0
                    || contentHeight <= 0
                )
                {
                    // Layout likely hasn't completed calculation yet. Reschedule slightly later.
                    // Add a counter check here to prevent infinite rescheduling if layout never settles.
                    rootVisualElement.schedule.Execute(PositionAndResizeWindow).ExecuteLater(20); // Try again
                    // Debug.LogWarning($"Layout not ready for {this.titleContent.text}, rescheduling resize.");
                    return;
                }

                // Estimate window chrome (title bar, borders) - Adjust these values as needed!
                const float chromeWidthPadding = 10f; // Total horizontal padding + border width estimation
                const float chromeHeightPadding = 35f; // Title bar height + vertical padding/border estimation

                // Calculate desired window size based on content + chrome
                float desiredWindowWidth = contentWidth + chromeWidthPadding;
                float desiredWindowHeight = contentHeight + chromeHeightPadding;

                // Optional minimum size constraints
                desiredWindowWidth = Mathf.Max(desiredWindowWidth, 250f); // Ensure a minimum usable width
                desiredWindowHeight = Mathf.Max(desiredWindowHeight, 100f); // Ensure a minimum usable height

                // Calculate centered position based on parent and NEW desired size
                Vector2 popupSize = new Vector2(desiredWindowWidth, desiredWindowHeight);
                float popupX = _parentPosition.x + (_parentPosition.width - popupSize.x) * 0.5f;
                float popupY = _parentPosition.y + (_parentPosition.height - popupSize.y) * 0.5f;

                try
                {
                    // Set the window's position (top-left) and size simultaneously
                    this.position = new Rect(popupX, popupY, popupSize.x, popupSize.y);
                    // Crucially, also set min/max size to the calculated size to prevent resizing *smaller* than content
                    this.minSize = popupSize;
                    this.maxSize = popupSize; // Lock size after calculation (optional)

                    // Debug.Log($"Positioned/Resized Popup {this.titleContent.text}. Target: {this.position}");
                }
                catch (Exception ex)
                {
                    Debug.LogError(
                        $"Error setting popup window position/size for '{this.titleContent.text}': {ex}"
                    );
                }
            }

            private void OnEnable()
            {
                if (rootVisualElement != null) // Should exist by now
                {
                    // Using ExecuteLater with minimal delay (e.g., 1ms)
                    rootVisualElement.schedule.Execute(PositionAndResizeWindow).ExecuteLater(1);
                }
                else
                {
                    // Fallback just in case root isn't ready, though unlikely
                    EditorApplication.delayCall += PositionAndResizeWindow;
                }
            }

            private void ClosePopup(bool result)
            {
                if (!_callbackInvoked)
                {
                    _onCompleteCallback?.Invoke(result);
                    _callbackInvoked = true;
                }
                this.Close(); // Close this popup window
            }

            // Ensure callback runs if window closed via 'X' etc. Treat as Cancel.
            private void OnDestroy()
            {
                if (!_callbackInvoked)
                {
                    _onCompleteCallback?.Invoke(false); // Invoke with 'false' if destroyed without button press
                }
            }
        }
    }
}
