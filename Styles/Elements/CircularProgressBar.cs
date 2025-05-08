namespace WallstopStudios.UnityHelpers.Styles.Elements
{
    using System.ComponentModel;
    using UnityEngine;
    using UnityEngine.UIElements;

    public sealed class CircularProgressBar : VisualElement
    {
        public enum StartPointLocation
        {
            Top = 0,
            Right = 1,
            Bottom = 2,
            Left = 3,
        }

        public enum FillDirection
        {
            Clockwise = 0,
            CounterClockwise = 1,
        }

        public const string USSClassName = "circular-progress-bar";
        public const string USSTrackColorVarName = "--track-color";
        public const string USSProgressColorVarName = "--progress-color";
        public const string USSThicknessVarName = "--thickness";

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

        private float _radius = 50f;
        public float Radius
        {
            get => _radius;
            set
            {
                _radius = Mathf.Max(1f, value);
                UpdateSize();
                MarkDirtyRepaint();
            }
        }

        private float _thickness = 10f;
        public float Thickness
        {
            get => _thickness;
            set
            {
                _thickness = Mathf.Max(1f, value);
                UpdateSize();
                MarkDirtyRepaint();
            }
        }

        private StartPointLocation _startPoint = StartPointLocation.Top;
        public StartPointLocation StartAt
        {
            get => _startPoint;
            set
            {
                _startPoint = value;
                MarkDirtyRepaint();
            }
        }

        private FillDirection _fillDirection = FillDirection.Clockwise;
        public FillDirection Direction
        {
            get => _fillDirection;
            set
            {
                _fillDirection = value;
                MarkDirtyRepaint();
            }
        }

        private Color _trackColor = Color.gray;
        public Color TrackColor
        {
            get => _trackColor;
            set
            {
                _trackColor = value;
                MarkDirtyRepaint();
            }
        }

        private Color _progressColor = Color.green;
        public Color ProgressColor
        {
            get => _progressColor;
            set
            {
                _progressColor = value;
                MarkDirtyRepaint();
            }
        }

        public new class UxmlFactory : UxmlFactory<CircularProgressBar, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private readonly UxmlFloatAttributeDescription _progressAttribute = new()
            {
                name = "progress",
                defaultValue = 0.5f,
            };

            private readonly UxmlFloatAttributeDescription _radiusAttribute = new()
            {
                name = "radius",
                defaultValue = 50f,
            };

            private readonly UxmlFloatAttributeDescription _thicknessAttribute = new()
            {
                name = "thickness",
                defaultValue = 10f,
            };

            private readonly UxmlEnumAttributeDescription<StartPointLocation> _startPointAttribute =
                new() { name = "start-at", defaultValue = StartPointLocation.Top };

            private readonly UxmlEnumAttributeDescription<FillDirection> _fillDirectionAttribute =
                new() { name = "direction", defaultValue = FillDirection.Clockwise };

            private readonly UxmlColorAttributeDescription _trackColorAttribute = new()
            {
                name = "track-color-attr",
                defaultValue = Color.gray,
            };

            private readonly UxmlColorAttributeDescription _progressColorAttribute = new()
            {
                name = "progress-color-attr",
                defaultValue = Color.green,
            };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                if (ve is not CircularProgressBar bar)
                {
                    Debug.LogError(
                        $"Initialization failed, expected {nameof(CircularProgressBar)}, found {ve?.GetType()}.)"
                    );
                    return;
                }

                bar.Progress = _progressAttribute.GetValueFromBag(bag, cc);
                bar.Radius = _radiusAttribute.GetValueFromBag(bag, cc);
                bar.Thickness = _thicknessAttribute.GetValueFromBag(bag, cc);
                bar.StartAt = _startPointAttribute.GetValueFromBag(bag, cc);
                bar.Direction = _fillDirectionAttribute.GetValueFromBag(bag, cc);
                bar.TrackColor = _trackColorAttribute.GetValueFromBag(bag, cc);
                bar.ProgressColor = _progressColorAttribute.GetValueFromBag(bag, cc);
            }
        }

        public CircularProgressBar()
        {
            AddToClassList(USSClassName);
            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            UpdateSize();
        }

        private void UpdateSize()
        {
            float diameter = (_radius + _thickness / 2f) * 2f;
            style.width = diameter;
            style.height = diameter;
            style.minWidth = diameter;
            style.minHeight = diameter;
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (
                customStyle.TryGetValue(
                    new CustomStyleProperty<Color>(USSTrackColorVarName),
                    out Color trackCol
                )
            )
            {
                _trackColor = trackCol;
            }
            if (
                customStyle.TryGetValue(
                    new CustomStyleProperty<Color>(USSProgressColorVarName),
                    out Color progressCol
                )
            )
            {
                _progressColor = progressCol;
            }
            if (
                customStyle.TryGetValue(
                    new CustomStyleProperty<float>(USSThicknessVarName),
                    out float thickVal
                )
            )
            {
                _thickness = Mathf.Max(1f, thickVal);
            }
            MarkDirtyRepaint();
        }

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            Painter2D painter = mgc.painter2D;
            Rect rect = contentRect;

            float drawRadius = _radius;
            Vector2 center = rect.center;

            if (_progress <= 0.01f)
            {
                painter.strokeColor = _trackColor;
                painter.lineWidth = _thickness;
                painter.BeginPath();
                painter.Arc(center, drawRadius, 0f, 360f);
                painter.Stroke();
                return;
            }

            switch (_fillDirection)
            {
                case FillDirection.CounterClockwise:
                {
                    painter.strokeColor = _progressColor;
                    painter.lineWidth = _thickness;
                    painter.BeginPath();
                    painter.Arc(center, drawRadius, 0f, 360f);
                    painter.Stroke();

                    float startAngleDegrees = GetStartAngleInDegrees(_startPoint);
                    float sweepAngleDegrees = -1 * _progress * 360f;

                    painter.strokeColor = _trackColor;
                    painter.lineWidth = _thickness;
                    painter.BeginPath();
                    painter.Arc(
                        center,
                        drawRadius,
                        startAngleDegrees,
                        (startAngleDegrees + sweepAngleDegrees)
                    );
                    painter.Stroke();
                    return;
                }
                case FillDirection.Clockwise:
                {
                    painter.strokeColor = _trackColor;
                    painter.lineWidth = _thickness;
                    painter.BeginPath();
                    painter.Arc(center, drawRadius, 0f, 360f);
                    painter.Stroke();

                    float startAngleDegrees = GetStartAngleInDegrees(_startPoint);
                    float sweepAngleDegrees = _progress * 360f;

                    painter.strokeColor = _progressColor;
                    painter.lineWidth = _thickness;
                    painter.BeginPath();
                    painter.Arc(
                        center,
                        drawRadius,
                        startAngleDegrees,
                        (startAngleDegrees + sweepAngleDegrees)
                    );
                    painter.Stroke();
                    return;
                }
                default:
                {
                    throw new InvalidEnumArgumentException(
                        nameof(_fillDirection),
                        (int)_fillDirection,
                        typeof(FillDirection)
                    );
                }
            }
        }

        private static float GetStartAngleInDegrees(StartPointLocation location)
        {
            return location switch
            {
                StartPointLocation.Top => -90f,
                StartPointLocation.Right => 0f,
                StartPointLocation.Bottom => 90f,
                StartPointLocation.Left => 180f,
                _ => 90f,
            };
        }
    }
}
