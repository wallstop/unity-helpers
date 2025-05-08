namespace WallstopStudios.UnityHelpers.Styles.Elements.Progress
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UIElements;

    public class GlitchProgressBar : VisualElement
    {
        // --- USS Class Names ---
        public static readonly string ussClassName = "glitch-progress-bar";

        // Track and fill are custom drawn

        // --- USS Custom Property Names ---
        public static readonly string ussNormalColorVarName = "--gpb-normal-color";
        public static readonly string ussGlitchColor1VarName = "--gpb-glitch-color1";
        public static readonly string ussGlitchColor2VarName = "--gpb-glitch-color2";
        public static readonly string ussTrackColorVarName = "--gpb-track-color";

        // --- C# Properties ---
        private float _progress = 0.5f;
        private float _visualProgress = 0.5f; // For smooth animation to target
        public float Progress
        {
            get => _progress;
            set
            {
                _targetProgress = Mathf.Clamp01(value);
                float newTarget = Mathf.Clamp01(value);
                if (!Mathf.Approximately(_targetProgress, newTarget))
                {
                    _targetProgress = newTarget;
                    // Ensure the animation loop is running if it's not already
                    if (panel != null && _animationUpdateItem == null)
                        StartAnimationUpdate();
                }
            }
        }

        private Color _normalColor = Color.green;
        public Color NormalColor
        {
            get => _normalColor;
            set
            {
                _normalColor = value;
                MarkDirtyRepaint();
            }
        }

        private Color _glitchColor1 = Color.red;
        public Color GlitchColor1
        {
            get => _glitchColor1;
            set
            {
                _glitchColor1 = value;
                MarkDirtyRepaint();
            }
        }

        private Color _glitchColor2 = Color.blue;
        public Color GlitchColor2
        {
            get => _glitchColor2;
            set
            {
                _glitchColor2 = value;
                MarkDirtyRepaint();
            }
        }

        private Color _trackColor = Color.black;
        public Color TrackColor
        {
            get => _trackColor;
            set
            {
                _trackColor = value;
                MarkDirtyRepaint();
            }
        }

        // Glitch parameters
        public float glitchIntensity = 0.1f; // Max displacement as a factor of height/width
        public float glitchFrequency = 0.2f; // Probability of a glitch occurring per frame
        public int glitchDurationFrames = 3; // How long a glitch lasts
        public float progressAnimationSpeed = 5f; // Speed for visual progress to catch up

        // Internal state
        private bool _isGlitching = false;
        private int _glitchTimer = 0;
        private float _targetProgress = 0.5f;
        private bool _isAnimatingProgress = false;
        private IVisualElementScheduledItem _animationUpdateItem;

        // --- UXML Factory and Traits ---
        public new class UxmlFactory : UxmlFactory<GlitchProgressBar, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlFloatAttributeDescription m_ProgressAttribute = new UxmlFloatAttributeDescription
            {
                name = "progress",
                defaultValue = 0.5f,
            };
            UxmlColorAttributeDescription m_NormalColorAttribute = new UxmlColorAttributeDescription
            {
                name = "normal-color",
                defaultValue = Color.green,
            };
            UxmlColorAttributeDescription m_GlitchColor1Attribute =
                new UxmlColorAttributeDescription
                {
                    name = "glitch-color1",
                    defaultValue = Color.red,
                };
            UxmlColorAttributeDescription m_GlitchColor2Attribute =
                new UxmlColorAttributeDescription
                {
                    name = "glitch-color2",
                    defaultValue = Color.blue,
                };
            UxmlColorAttributeDescription m_TrackColorAttribute = new UxmlColorAttributeDescription
            {
                name = "track-color",
                defaultValue = Color.black,
            };
            UxmlFloatAttributeDescription m_GlitchIntensityAttribute =
                new UxmlFloatAttributeDescription
                {
                    name = "glitch-intensity",
                    defaultValue = 0.1f,
                };
            UxmlFloatAttributeDescription m_GlitchFrequencyAttribute =
                new UxmlFloatAttributeDescription
                {
                    name = "glitch-frequency",
                    defaultValue = 0.2f,
                };
            UxmlIntAttributeDescription m_GlitchDurationFramesAttribute =
                new UxmlIntAttributeDescription
                {
                    name = "glitch-duration-frames",
                    defaultValue = 3,
                };
            UxmlFloatAttributeDescription m_ProgressAnimationSpeedAttribute =
                new UxmlFloatAttributeDescription
                {
                    name = "progress-animation-speed",
                    defaultValue = 5f,
                };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var bar = ve as GlitchProgressBar;
                // Set target progress first, then actual progress to trigger animation if needed
                bar._targetProgress = m_ProgressAttribute.GetValueFromBag(bag, cc);
                bar._visualProgress = bar._targetProgress; // Start at target initially
                bar._progress = bar._targetProgress;

                bar.NormalColor = m_NormalColorAttribute.GetValueFromBag(bag, cc);
                bar.GlitchColor1 = m_GlitchColor1Attribute.GetValueFromBag(bag, cc);
                bar.GlitchColor2 = m_GlitchColor2Attribute.GetValueFromBag(bag, cc);
                bar.TrackColor = m_TrackColorAttribute.GetValueFromBag(bag, cc);
                bar.glitchIntensity = m_GlitchIntensityAttribute.GetValueFromBag(bag, cc);
                bar.glitchFrequency = m_GlitchFrequencyAttribute.GetValueFromBag(bag, cc);
                bar.glitchDurationFrames = m_GlitchDurationFramesAttribute.GetValueFromBag(bag, cc);
                bar.progressAnimationSpeed = m_ProgressAnimationSpeedAttribute.GetValueFromBag(
                    bag,
                    cc
                );

                if (bar.style.height.keyword == StyleKeyword.Auto || bar.style.height.value == 0)
                    bar.style.height = 20;
                if (bar.style.width.keyword == StyleKeyword.Auto || bar.style.width.value == 0)
                    bar.style.width = 200;
            }
        }

        public GlitchProgressBar()
        {
            AddToClassList(ussClassName);
            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            // Initial state
            _visualProgress = _progress;
            _targetProgress = _progress;
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (!Mathf.Approximately(_visualProgress, _targetProgress) || _isGlitching)
                StartAnimationUpdate();
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            StopAnimationUpdate();
        }

        private void StartAnimationUpdate() // Renamed from StartProgressAnimation for clarity
        {
            if (panel == null || _animationUpdateItem != null)
                return;
            _animationUpdateItem = schedule.Execute(UpdateState).Every(16); // ~60fps
            _isAnimatingProgress = true; // General animation flag
        }

        private void StopAnimationUpdate()
        {
            _animationUpdateItem?.Pause();
            _animationUpdateItem = null;
            _isAnimatingProgress = false;
        }

        private void UpdateState(TimerState ts) // Combined update logic
        {
            bool needsRepaint = false;

            // Animate visual progress towards target progress
            if (!Mathf.Approximately(_visualProgress, _targetProgress))
            {
                _visualProgress = Mathf.Lerp(
                    _visualProgress,
                    _targetProgress,
                    ts.deltaTime / 1000f * progressAnimationSpeed
                );
                _progress = _visualProgress; // Update actual progress for external reading if needed
                if (Mathf.Abs(_visualProgress - _targetProgress) < 0.001f)
                {
                    _visualProgress = _targetProgress;
                    _progress = _targetProgress;
                }
                needsRepaint = true;
            }

            // Handle glitch timing
            if (_isGlitching)
            {
                _glitchTimer--;
                if (_glitchTimer <= 0)
                {
                    _isGlitching = false;
                }
                needsRepaint = true; // Always repaint during glitch
            }
            else
            {
                // Randomly trigger a new glitch if progress is changing
                bool progressIsChanging = !Mathf.Approximately(_visualProgress, _targetProgress);
                if (progressIsChanging && Random.value < glitchFrequency)
                {
                    _isGlitching = true;
                    _glitchTimer = glitchDurationFrames;
                    needsRepaint = true;
                }
            }

            if (needsRepaint)
            {
                MarkDirtyRepaint();
            }

            // Stop animation updates if nothing is happening
            if (
                Mathf.Approximately(_visualProgress, _targetProgress)
                && !_isGlitching
                && _animationUpdateItem != null
            )
            {
                // If we don't stop, it keeps running even if static.
                // This might be okay if glitchFrequency is high and you want idle glitches.
                // For now, let's assume glitches only happen during progress change.
                // StopAnimationUpdate(); // Decided against auto-stopping to allow idle glitches if frequency is non-zero
            }
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (
                customStyle.TryGetValue(
                    new CustomStyleProperty<Color>(ussNormalColorVarName),
                    out Color normCol
                )
            )
                NormalColor = normCol;
            if (
                customStyle.TryGetValue(
                    new CustomStyleProperty<Color>(ussGlitchColor1VarName),
                    out Color g1Col
                )
            )
                GlitchColor1 = g1Col;
            if (
                customStyle.TryGetValue(
                    new CustomStyleProperty<Color>(ussGlitchColor2VarName),
                    out Color g2Col
                )
            )
                GlitchColor2 = g2Col;
            if (
                customStyle.TryGetValue(
                    new CustomStyleProperty<Color>(ussTrackColorVarName),
                    out Color trackCol
                )
            )
                TrackColor = trackCol;
            MarkDirtyRepaint();
        }

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            var painter = mgc.painter2D;
            Rect r = contentRect;

            if (r.width <= 0 || r.height <= 0)
                return;

            // Draw Track
            painter.fillColor = _trackColor;
            painter.BeginPath();
            painter.MoveTo(new Vector2(r.xMin, r.yMin));
            painter.LineTo(new Vector2(r.xMax, r.yMin));
            painter.LineTo(new Vector2(r.xMax, r.yMax));
            painter.LineTo(new Vector2(r.xMin, r.yMax));
            painter.ClosePath();
            painter.Fill();

            if (_visualProgress <= 0.001f)
                return;

            // Draw Progress Fill
            float fillWidth = r.width * _visualProgress;

            if (_isGlitching)
            {
                // Draw multiple displaced and differently colored segments
                int numSegments = Random.Range(2, 5);
                float segmentHeight = r.height / numSegments;

                for (int i = 0; i < numSegments; i++)
                {
                    float offsetX = (Random.value - 0.5f) * 2f * r.width * glitchIntensity;
                    float offsetY = (Random.value - 0.5f) * 2f * r.height * glitchIntensity * 0.3f; // Less vertical glitch

                    float currentSegmentY = r.yMin + i * segmentHeight;

                    // Clamp offsets to stay somewhat within bounds
                    offsetX = Mathf.Clamp(offsetX, -r.width * 0.2f, r.width * 0.2f);
                    offsetY = Mathf.Clamp(offsetY, -r.height * 0.1f, r.height * 0.1f);

                    Rect glitchRect = new Rect(
                        r.xMin + offsetX,
                        currentSegmentY + offsetY,
                        fillWidth, // Could also randomize width slightly
                        segmentHeight
                    );

                    // Clip glitchRect to the main contentRect bounds
                    glitchRect.xMin = Mathf.Max(glitchRect.xMin, r.xMin);
                    glitchRect.yMin = Mathf.Max(glitchRect.yMin, r.yMin);
                    glitchRect.xMax = Mathf.Min(glitchRect.xMax, r.xMax);
                    glitchRect.yMax = Mathf.Min(glitchRect.yMax, r.yMax);

                    if (glitchRect.width > 0 && glitchRect.height > 0)
                    {
                        painter.fillColor = (Random.value < 0.5f) ? _glitchColor1 : _glitchColor2;
                        painter.BeginPath();
                        painter.MoveTo(new Vector2(glitchRect.xMin, glitchRect.yMin));
                        painter.LineTo(new Vector2(glitchRect.xMax, glitchRect.yMin));
                        painter.LineTo(new Vector2(glitchRect.xMax, glitchRect.yMax));
                        painter.LineTo(new Vector2(glitchRect.xMin, glitchRect.yMax));
                        painter.ClosePath();
                        painter.Fill();
                    }
                }
            }
            else
            {
                // Normal fill
                painter.fillColor = _normalColor;
                painter.BeginPath();
                painter.MoveTo(new Vector2(r.xMin, r.yMin));
                painter.LineTo(new Vector2(r.xMin + fillWidth, r.yMin));
                painter.LineTo(new Vector2(r.xMin + fillWidth, r.yMax));
                painter.LineTo(new Vector2(r.xMin, r.yMax));
                painter.ClosePath();
                painter.Fill();
            }
        }
    }
}
