namespace WallstopStudios.UnityHelpers.Styles.Elements.Progress
{
    using System.Collections.Generic;
    using System.Collections.Generic;
    using System.Collections.Generic;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine;
    using UnityEngine;
    using UnityEngine;
    using UnityEngine.UIElements;
    using UnityEngine.UIElements;
    using UnityEngine.UIElements;
    using UnityEngine.UIElements;

    public class LiquidProgressBar : VisualElement
    {
        public static readonly string ussClassName = "liquid-progress-bar";
        public static readonly string ussTrackColorVarName = "--lpb-track-color";
        public static readonly string ussTrackThicknessVarName = "--lpb-track-thickness"; // For stroked track
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

        private Color _trackColor = new Color(0.4f, 0.4f, 0.4f, 1f); // Lighter track for stroke
        public Color TrackColor
        {
            get => _trackColor;
            set
            {
                _trackColor = value;
                MarkDirtyRepaint();
            }
        }

        private float _trackThickness = 2f; // Thickness for the stroked track
        public float TrackThickness
        {
            get => _trackThickness;
            set
            {
                _trackThickness = Mathf.Max(0, value);
                MarkDirtyRepaint();
            }
        }

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

        private float _leadingEdgeCurvature = 0.6f; // 0 = flat, 1 = max bulge for given height
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

        private float _wobbleMagnitude = 0.3f; // How much the bulge point can shift horizontally
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

        private float _wobbleOffset = 0f; // Horizontal offset for the peak of the bulge
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
                defaultValue = new Color(0.4f, 0.4f, 0.4f, 1f),
            };
            UxmlFloatAttributeDescription m_TrackThicknessAttribute =
                new UxmlFloatAttributeDescription { name = "track-thickness", defaultValue = 2f };
            UxmlColorAttributeDescription m_ProgressColorAttribute =
                new UxmlColorAttributeDescription
                {
                    name = "progress-color",
                    defaultValue = new Color(0.3f, 0.7f, 1f, 1f),
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
                    bar.style.height = 22; // Min height to accommodate track
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
                * _wobbleMagnitude; // Multiplier controls frequency
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
            float effectiveRadius = Mathf.Min(_borderRadius, halfHeight - _trackThickness / 2f); // Radius for inner fill area
            if (effectiveRadius < 0)
                effectiveRadius = 0;

            float trackInset = _trackThickness / 2f;
            if (trackInset < 0.01f)
                trackInset = 0; // No inset if no track thickness

            // Define the outer bounds for the track stroke and inner bounds for the fill
            Rect outerRect = r;
            Rect innerRect = new Rect(
                r.x + trackInset,
                r.y + trackInset,
                r.width - _trackThickness,
                r.height - _trackThickness
            );
            if (innerRect.width <= 0 || innerRect.height <= 0)
                return; // No space for fill if track is too thick

            // === 1. Draw Track (as a STROKE) ===
            if (_trackThickness > 0.01f)
            {
                painter.strokeColor = _trackColor;
                painter.lineWidth = _trackThickness;
                painter.BeginPath();
                // Path for a rounded rectangle for the track stroke
                painter.MoveTo(new Vector2(outerRect.xMin + _borderRadius, outerRect.yMin));
                painter.LineTo(new Vector2(outerRect.xMax - _borderRadius, outerRect.yMin));
                if (_borderRadius > 0)
                    painter.Arc(
                        new Vector2(outerRect.xMax - _borderRadius, outerRect.yMin + _borderRadius),
                        _borderRadius,
                        270f,
                        90f
                    );
                painter.LineTo(new Vector2(outerRect.xMax, outerRect.yMax - _borderRadius));
                if (_borderRadius > 0)
                    painter.Arc(
                        new Vector2(outerRect.xMax - _borderRadius, outerRect.yMax - _borderRadius),
                        _borderRadius,
                        0f,
                        90f
                    );
                painter.LineTo(new Vector2(outerRect.xMin + _borderRadius, outerRect.yMax));
                if (_borderRadius > 0)
                    painter.Arc(
                        new Vector2(outerRect.xMin + _borderRadius, outerRect.yMax - _borderRadius),
                        _borderRadius,
                        90f,
                        90f
                    );
                painter.LineTo(new Vector2(outerRect.xMin, outerRect.yMin + _borderRadius));
                if (_borderRadius > 0)
                    painter.Arc(
                        new Vector2(outerRect.xMin + _borderRadius, outerRect.yMin + _borderRadius),
                        _borderRadius,
                        180f,
                        90f
                    );
                painter.ClosePath();
                painter.Stroke();
            }

            // === 2. Draw Liquid Fill ===
            if (_progress <= 0.0001f)
                return;

            painter.fillColor = _progressColor;

            float baseFillWidth = innerRect.width * _progress;
            float animatedPeakX =
                innerRect.xMin + baseFillWidth + (_wobbleOffset * innerRect.height * 0.3f); // Wobble affects the peak X
            animatedPeakX = Mathf.Clamp(animatedPeakX, innerRect.xMin, innerRect.xMax);

            if (animatedPeakX <= innerRect.xMin + effectiveRadius) // Fill is too small, draw a small pill/circle at start
            {
                if (animatedPeakX > innerRect.xMin) // Only draw if there's some width
                {
                    float smallPillRadius = Mathf.Min(
                        (animatedPeakX - innerRect.xMin) / 2f,
                        innerRect.height / 2f
                    );
                    if (smallPillRadius > 0.1f) // Threshold to draw
                    {
                        painter.BeginPath();
                        painter.MoveTo(
                            new Vector2(innerRect.xMin + smallPillRadius, innerRect.yMin)
                        );
                        painter.LineTo(
                            new Vector2(animatedPeakX - smallPillRadius, innerRect.yMin)
                        );
                        painter.Arc(
                            new Vector2(
                                animatedPeakX - smallPillRadius,
                                innerRect.yMin + smallPillRadius
                            ),
                            smallPillRadius,
                            270f,
                            180f
                        );
                        painter.LineTo(
                            new Vector2(innerRect.xMin + smallPillRadius, innerRect.yMax)
                        );
                        painter.Arc(
                            new Vector2(
                                innerRect.xMin + smallPillRadius,
                                innerRect.yMin + smallPillRadius
                            ),
                            smallPillRadius,
                            90f,
                            180f
                        );
                        painter.ClosePath();
                        painter.Fill();
                    }
                }
                return;
            }

            painter.BeginPath();

            // Start: Top-left of fill area
            painter.MoveTo(new Vector2(innerRect.xMin + effectiveRadius, innerRect.yMin));

            // Top edge: leading to the dynamic leading edge or top-right corner
            float topEdgeEndX;
            bool useLiquidEdge =
                _progress < 0.995f
                && _leadingEdgeCurvature > 0.01f
                && (animatedPeakX < innerRect.xMax - effectiveRadius - 1);

            if (useLiquidEdge)
            {
                // The straight part of the top edge ends before the bulge truly starts
                float curveStartBaseX =
                    animatedPeakX - (innerRect.height * _leadingEdgeCurvature * 0.4f);
                topEdgeEndX = Mathf.Max(innerRect.xMin + effectiveRadius, curveStartBaseX);
                painter.LineTo(new Vector2(topEdgeEndX, innerRect.yMin));

                // Liquid Leading Edge
                Vector2 peak = new Vector2(animatedPeakX, innerRect.yMin + innerRect.height / 2f);

                // Control points for top curve: (P1 is topEdgeEndX, P2 is peak)
                // CP1: Pulls away from P1 horizontally, slightly down
                // CP2: Pulls towards P2 horizontally, slightly up from P2's y
                float controlPointBulgeX =
                    animatedPeakX + (innerRect.height * _leadingEdgeCurvature * 0.5f); // How far control points extend past peak

                Vector2 cp1_top = new Vector2(
                    topEdgeEndX + (peak.x - topEdgeEndX) * 0.3f,
                    innerRect.yMin
                );
                Vector2 cp2_top = new Vector2(controlPointBulgeX, peak.y - innerRect.height * 0.2f);
                painter.BezierCurveTo(cp1_top, cp2_top, peak);

                // Control points for bottom curve: (P1 is peak, P2 is (topEdgeEndX, innerRect.yMax))
                Vector2 cp1_bottom = new Vector2(
                    controlPointBulgeX,
                    peak.y + innerRect.height * 0.2f
                );
                Vector2 cp2_bottom = new Vector2(
                    topEdgeEndX + (peak.x - topEdgeEndX) * 0.3f,
                    innerRect.yMax
                );
                painter.BezierCurveTo(
                    cp1_bottom,
                    cp2_bottom,
                    new Vector2(topEdgeEndX, innerRect.yMax)
                );

                painter.LineTo(new Vector2(innerRect.xMin + effectiveRadius, innerRect.yMax)); // Bottom line
            }
            else // Standard rounded rectangle fill
            {
                topEdgeEndX = animatedPeakX - effectiveRadius; // animatedPeakX is effectively the rightmost edge of the fill
                if (topEdgeEndX < innerRect.xMin + effectiveRadius)
                    topEdgeEndX = innerRect.xMin + effectiveRadius;
                painter.LineTo(new Vector2(topEdgeEndX, innerRect.yMin));

                if (effectiveRadius > 0)
                    painter.Arc(
                        new Vector2(
                            animatedPeakX - effectiveRadius,
                            innerRect.yMin + effectiveRadius
                        ),
                        effectiveRadius,
                        270f,
                        90f
                    ); // TR
                painter.LineTo(new Vector2(animatedPeakX, innerRect.yMax - effectiveRadius));
                if (effectiveRadius > 0)
                    painter.Arc(
                        new Vector2(
                            animatedPeakX - effectiveRadius,
                            innerRect.yMax - effectiveRadius
                        ),
                        effectiveRadius,
                        0f,
                        90f
                    ); // BR
                painter.LineTo(new Vector2(innerRect.xMin + effectiveRadius, innerRect.yMax)); // Bottom line
            }

            // Bottom-left corner
            if (effectiveRadius > 0)
                painter.Arc(
                    new Vector2(innerRect.xMin + effectiveRadius, innerRect.yMax - effectiveRadius),
                    effectiveRadius,
                    90f,
                    90f
                );
            // Left edge is implicitly closed by ClosePath

            painter.ClosePath();
            painter.Fill();
        }
    }
}
