namespace WallstopStudios.UnityHelpers.Styles.Elements.Progress
{
    using System.Collections.Generic;
    using System.Collections.Generic;
    using System.Collections.Generic;
    using System.Collections.Generic;
    using System.Collections.Generic;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine;
    using UnityEngine;
    using UnityEngine;
    using UnityEngine;
    using UnityEngine;
    using UnityEngine.UIElements;
    using UnityEngine.UIElements;
    using UnityEngine.UIElements;
    using UnityEngine.UIElements;
    using UnityEngine.UIElements;
    using UnityEngine.UIElements;

    public class LiquidProgressBar : VisualElement
    {
        public static readonly string ussClassName = "liquid-progress-bar";
        public static readonly string ussTrackColorVarName = "--lpb-track-color";
        public static readonly string ussTrackThicknessVarName = "--lpb-track-thickness";
        public static readonly string ussProgressColorVarName = "--lpb-progress-color";
        public static readonly string ussBorderRadiusVarName = "--lpb-border-radius";

        private float _progress = 0.5f;
        public float Progress
        {
            get => _progress;
            set
            {
                _progress = Mathf.Clamp01(value);
                MarkDirtyRepaint();
            }
        }

        private Color _trackColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        public Color TrackColor
        {
            get => _trackColor;
            set
            {
                _trackColor = value;
                MarkDirtyRepaint();
            }
        }

        private float _trackThickness = 2f;
        public float TrackThickness
        {
            get => _trackThickness;
            set
            {
                _trackThickness = Mathf.Max(0.01f, value);
                MarkDirtyRepaint();
            }
        } // Ensure min thickness for visibility if stroked

        private Color _progressColor = new Color(0.3f, 0.7f, 1f, 1f);
        public Color ProgressColor
        {
            get => _progressColor;
            set
            {
                _progressColor = value;
                MarkDirtyRepaint();
            }
        }

        private float _borderRadius = 7f;
        public float BorderRadius
        {
            get => _borderRadius;
            set
            {
                _borderRadius = Mathf.Max(0, value);
                MarkDirtyRepaint();
            }
        }

        private float _leadingEdgeCurvature = 0.6f;
        public float LeadingEdgeCurvature
        {
            get => _leadingEdgeCurvature;
            set
            {
                _leadingEdgeCurvature = Mathf.Clamp01(value);
                MarkDirtyRepaint();
            }
        }

        private float _animationSpeed = 2.5f;
        public float AnimationSpeed
        {
            get => _animationSpeed;
            set => _animationSpeed = Mathf.Max(0, value);
        }

        private float _wobbleMagnitude = 0.3f;
        public float WobbleMagnitude
        {
            get => _wobbleMagnitude;
            set => _wobbleMagnitude = Mathf.Clamp(value, 0f, 1f);
        }

        private bool _animateLeadingEdge = true;
        public bool AnimateLeadingEdge
        {
            get => _animateLeadingEdge;
            set
            {
                if (_animateLeadingEdge == value)
                    return;
                _animateLeadingEdge = value;
                if (_animateLeadingEdge)
                {
                    if (panel != null)
                        StartAnimationUpdate();
                }
                else
                {
                    StopAnimationUpdate();
                    _wobbleOffset = 0;
                }
                MarkDirtyRepaint();
            }
        }

        private float _wobbleOffset = 0f;
        private IVisualElementScheduledItem _animationUpdateItem;

        public new class UxmlFactory : UxmlFactory<LiquidProgressBar, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlFloatAttributeDescription m_ProgressAttribute = new UxmlFloatAttributeDescription
            {
                name = "progress",
                defaultValue = 0.5f,
            };
            UxmlColorAttributeDescription m_TrackColorAttribute = new UxmlColorAttributeDescription
            {
                name = "track-color",
                defaultValue = new Color(0.4f, 0.4f, 0.4f, 1),
            };
            UxmlFloatAttributeDescription m_TrackThicknessAttribute =
                new UxmlFloatAttributeDescription { name = "track-thickness", defaultValue = 2f };
            UxmlColorAttributeDescription m_ProgressColorAttribute =
                new UxmlColorAttributeDescription
                {
                    name = "progress-color",
                    defaultValue = new Color(0.3f, 0.7f, 1, 1),
                };
            UxmlFloatAttributeDescription m_BorderRadiusAttribute =
                new UxmlFloatAttributeDescription { name = "border-radius", defaultValue = 7f };
            UxmlFloatAttributeDescription m_LeadingEdgeCurvatureAttribute =
                new UxmlFloatAttributeDescription
                {
                    name = "leading-edge-curvature",
                    defaultValue = 0.6f,
                };
            UxmlFloatAttributeDescription m_AnimationSpeedAttribute =
                new UxmlFloatAttributeDescription { name = "animation-speed", defaultValue = 2.5f };
            UxmlFloatAttributeDescription m_WobbleMagnitudeAttribute =
                new UxmlFloatAttributeDescription
                {
                    name = "wobble-magnitude",
                    defaultValue = 0.3f,
                };
            UxmlBoolAttributeDescription m_AnimateLeadingEdgeAttribute =
                new UxmlBoolAttributeDescription
                {
                    name = "animate-leading-edge",
                    defaultValue = true,
                };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var bar = ve as LiquidProgressBar;
                bar.Progress = m_ProgressAttribute.GetValueFromBag(bag, cc);
                bar.TrackColor = m_TrackColorAttribute.GetValueFromBag(bag, cc);
                bar.TrackThickness = m_TrackThicknessAttribute.GetValueFromBag(bag, cc);
                bar.ProgressColor = m_ProgressColorAttribute.GetValueFromBag(bag, cc);
                bar.BorderRadius = m_BorderRadiusAttribute.GetValueFromBag(bag, cc);
                bar.LeadingEdgeCurvature = m_LeadingEdgeCurvatureAttribute.GetValueFromBag(bag, cc);
                bar.AnimationSpeed = m_AnimationSpeedAttribute.GetValueFromBag(bag, cc);
                bar.WobbleMagnitude = m_WobbleMagnitudeAttribute.GetValueFromBag(bag, cc);
                bar.AnimateLeadingEdge = m_AnimateLeadingEdgeAttribute.GetValueFromBag(bag, cc);

                if (bar.style.height.keyword == StyleKeyword.Auto || bar.style.height.value == 0)
                    bar.style.height = 22;
                if (bar.style.width.keyword == StyleKeyword.Auto || bar.style.width.value == 0)
                    bar.style.width = 200;
            }
        }

        public LiquidProgressBar()
        {
            AddToClassList(ussClassName);
            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (_animateLeadingEdge)
                StartAnimationUpdate();
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            StopAnimationUpdate();
        }

        private void StartAnimationUpdate()
        {
            if (!_animateLeadingEdge || panel == null || _animationUpdateItem != null)
                return;
            _animationUpdateItem = schedule.Execute(UpdateAnimation).Every(33);
        }

        private void StopAnimationUpdate()
        {
            _animationUpdateItem?.Pause();
            _animationUpdateItem = null;
        }

        private void UpdateAnimation(TimerState ts)
        {
            if (!_animateLeadingEdge || panel == null)
            {
                StopAnimationUpdate();
                _wobbleOffset = 0;
                MarkDirtyRepaint();
                return;
            }
            _wobbleOffset =
                Mathf.Sin(UnityEngine.Time.realtimeSinceStartup * _animationSpeed * 4f)
                * _wobbleMagnitude;
            MarkDirtyRepaint();
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (
                customStyle.TryGetValue(
                    new CustomStyleProperty<Color>(ussTrackColorVarName),
                    out Color c
                )
            )
                TrackColor = c;
            if (
                customStyle.TryGetValue(
                    new CustomStyleProperty<float>(ussTrackThicknessVarName),
                    out float tt
                )
            )
                TrackThickness = tt;
            if (
                customStyle.TryGetValue(
                    new CustomStyleProperty<Color>(ussProgressColorVarName),
                    out Color pc
                )
            )
                ProgressColor = pc;
            if (
                customStyle.TryGetValue(
                    new CustomStyleProperty<float>(ussBorderRadiusVarName),
                    out float br
                )
            )
                BorderRadius = br;
        }

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            var painter = mgc.painter2D;
            Rect r = contentRect;

            if (r.width <= 0 || r.height <= 0)
                return;

            float barHeight = r.height;
            float halfHeight = barHeight / 2f;

            float outerRadius = Mathf.Min(_borderRadius, halfHeight, r.width / 2f);
            if (outerRadius < 0.01f)
                outerRadius = 0;

            float halfTrackThickness = _trackThickness / 2f;
            if (halfTrackThickness < 0.01f)
                halfTrackThickness = 0;

            Rect fillRect = new Rect(
                r.xMin + halfTrackThickness,
                r.yMin + halfTrackThickness,
                r.width - _trackThickness,
                r.height - _trackThickness
            );

            if (fillRect.width <= 0 || fillRect.height <= 0)
                return;

            // Effective radius for the static corners of the fill.
            // This is the outer _borderRadius adjusted for the track thickness.
            float innerRadius = Mathf.Max(0, outerRadius - halfTrackThickness);

            // === 1. Draw Track (as a STROKE) ===
            if (_trackThickness > 0.01f)
            {
                painter.strokeColor = _trackColor;
                painter.lineWidth = _trackThickness;
                painter.lineCap = LineCap.Round;
                painter.lineJoin = LineJoin.Round;

                painter.BeginPath();
                painter.MoveTo(new Vector2(r.xMin + outerRadius, r.yMin));
                painter.LineTo(new Vector2(r.xMax - outerRadius, r.yMin)); // Top
                if (outerRadius > 0)
                    painter.Arc(
                        new Vector2(r.xMax - outerRadius, r.yMin + outerRadius),
                        outerRadius,
                        270f,
                        90f
                    ); // TR
                painter.LineTo(new Vector2(r.xMax, r.yMax - outerRadius)); // Right
                if (outerRadius > 0)
                    painter.Arc(
                        new Vector2(r.xMax - outerRadius, r.yMax - outerRadius),
                        outerRadius,
                        0f,
                        90f
                    ); // BR
                painter.LineTo(new Vector2(r.xMin + outerRadius, r.yMax)); // Bottom
                if (outerRadius > 0)
                    painter.Arc(
                        new Vector2(r.xMin + outerRadius, r.yMax - outerRadius),
                        outerRadius,
                        90f,
                        90f
                    ); // BL
                painter.LineTo(new Vector2(r.xMin, r.yMin + outerRadius)); // Left
                if (outerRadius > 0)
                    painter.Arc(
                        new Vector2(r.xMin + outerRadius, r.yMin + outerRadius),
                        outerRadius,
                        180f,
                        90f
                    ); // TL
                painter.ClosePath();
                painter.Stroke();
            }

            // === 2. Draw Liquid Fill ===
            if (_progress <= 0.0001f)
                return;

            painter.fillColor = _progressColor;

            float baseFillWidth = fillRect.width * _progress;
            float animatedPeakXTarget =
                fillRect.xMin + baseFillWidth + (_wobbleOffset * fillRect.height * 0.3f);
            animatedPeakXTarget = Mathf.Clamp(animatedPeakXTarget, fillRect.xMin, fillRect.xMax);

            float currentFillWidth = animatedPeakXTarget - fillRect.xMin;
            if (currentFillWidth < 0.01f)
                currentFillWidth = 0;

            bool drawPillShape = (
                currentFillWidth < fillRect.height && _progress < 0.99f && currentFillWidth > 0.01f
            );

            painter.BeginPath();

            if (drawPillShape)
            {
                float pillCapRadius = fillRect.height / 2f;
                // In this case, currentFillWidth is the total width of the pill
                float straightSegmentWidth = Mathf.Max(0, currentFillWidth - 2 * pillCapRadius);

                // Start at the beginning of the top-left curve (top point of left cap)
                painter.MoveTo(new Vector2(fillRect.xMin + pillCapRadius, fillRect.yMin));

                if (straightSegmentWidth > 0) // Top line if pill is wider than just two caps
                    painter.LineTo(
                        new Vector2(
                            fillRect.xMin + pillCapRadius + straightSegmentWidth,
                            fillRect.yMin
                        )
                    );

                // Right semi-circle cap
                painter.Arc(
                    new Vector2(
                        fillRect.xMin + pillCapRadius + straightSegmentWidth,
                        fillRect.yMin + pillCapRadius
                    ),
                    pillCapRadius,
                    270f,
                    180f
                ); // From top to bottom

                if (straightSegmentWidth > 0) // Bottom line
                    painter.LineTo(new Vector2(fillRect.xMin + pillCapRadius, fillRect.yMax));

                // Left semi-circle cap
                painter.Arc(
                    new Vector2(fillRect.xMin + pillCapRadius, fillRect.yMin + pillCapRadius),
                    pillCapRadius,
                    90f,
                    180f
                ); // From bottom to top
            }
            else // Draw the main liquid/rounded rectangle fill
            {
                // Start: Top-left point, after potential corner
                painter.MoveTo(new Vector2(fillRect.xMin + innerRadius, fillRect.yMin)); // Point A

                bool useLiquidEdge =
                    _progress < 0.995f
                    && _leadingEdgeCurvature > 0.01f
                    && (animatedPeakXTarget < fillRect.xMax - innerRadius - 0.1f)
                    && currentFillWidth > innerRadius;

                if (useLiquidEdge)
                {
                    float curveStartBaseX =
                        animatedPeakXTarget - (fillRect.height * _leadingEdgeCurvature * 0.4f);
                    curveStartBaseX = Mathf.Max(fillRect.xMin + innerRadius, curveStartBaseX);
                    painter.LineTo(new Vector2(curveStartBaseX, fillRect.yMin)); // Line A to B (start of curve)

                    Vector2 peak = new Vector2(
                        animatedPeakXTarget,
                        fillRect.yMin + fillRect.height / 2f
                    );
                    float controlPointBulgeXFactor = fillRect.height * _leadingEdgeCurvature * 0.5f;

                    Vector2 cp1_top = new Vector2(
                        curveStartBaseX + (peak.x - curveStartBaseX) * 0.35f,
                        fillRect.yMin
                    );
                    Vector2 cp2_top = new Vector2(
                        peak.x + controlPointBulgeXFactor,
                        peak.y - fillRect.height * 0.25f
                    );
                    painter.BezierCurveTo(cp1_top, cp2_top, peak); // Curve B to Peak

                    Vector2 cp1_bottom = new Vector2(
                        peak.x + controlPointBulgeXFactor,
                        peak.y + fillRect.height * 0.25f
                    );
                    Vector2 cp2_bottom = new Vector2(
                        curveStartBaseX + (peak.x - curveStartBaseX) * 0.35f,
                        fillRect.yMax
                    );
                    painter.BezierCurveTo(
                        cp1_bottom,
                        cp2_bottom,
                        new Vector2(curveStartBaseX, fillRect.yMax)
                    ); // Curve Peak to C (end of curve)

                    painter.LineTo(new Vector2(fillRect.xMin + innerRadius, fillRect.yMax)); // Line C to D (bottom edge)
                }
                else // Standard rounded rectangle fill for the right edge
                {
                    float fillRightX = animatedPeakXTarget;
                    painter.LineTo(new Vector2(fillRightX - innerRadius, fillRect.yMin)); // Line A to B' (top edge)
                    if (innerRadius > 0)
                        painter.Arc(
                            new Vector2(fillRightX - innerRadius, fillRect.yMin + innerRadius),
                            innerRadius,
                            270f,
                            90f
                        ); // TR Corner
                    // Now at (fillRightX, fillRect.yMin + innerRadius)
                    painter.LineTo(new Vector2(fillRightX, fillRect.yMax - innerRadius)); // Right Vertical Line
                    // Now at (fillRightX, fillRect.yMax - innerRadius)
                    if (innerRadius > 0)
                        painter.Arc(
                            new Vector2(fillRightX - innerRadius, fillRect.yMax - innerRadius),
                            innerRadius,
                            0f,
                            90f
                        ); // BR Corner
                    // Now at (fillRightX - innerRadius, fillRect.yMax)
                    painter.LineTo(new Vector2(fillRect.xMin + innerRadius, fillRect.yMax)); // Line to D (bottom edge)
                }

                // Common path for left side:
                // Current point is (fillRect.xMin + innerRadius, fillRect.yMax)
                if (innerRadius > 0)
                    painter.Arc(
                        new Vector2(fillRect.xMin + innerRadius, fillRect.yMax - innerRadius),
                        innerRadius,
                        90f,
                        90f
                    ); // BL Corner
                // Now at (fillRect.xMin, fillRect.yMax - innerRadius)

                // Explicitly draw left vertical line back to start of Top-Left arc/corner
                painter.LineTo(new Vector2(fillRect.xMin, fillRect.yMin + innerRadius)); // E (point before TL arc)

                if (innerRadius > 0)
                    painter.Arc(
                        new Vector2(fillRect.xMin + innerRadius, fillRect.yMin + innerRadius),
                        innerRadius,
                        180f,
                        90f
                    ); // TL Corner
                // Path should now be at the MoveTo point A: (fillRect.xMin + innerRadius, fillRect.yMin)
            }

            painter.ClosePath(); // This should now correctly close from A back to A.
            painter.Fill();
        }
    }
}
