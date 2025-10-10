/*
    Original implementation provided by JWoe
 */

namespace WallstopStudios.UnityHelpers.Visuals.UGUI
{
    using UnityEngine;
    using UnityEngine.Serialization;
    using UnityEngine.UI;

    /// <summary>
    /// Extends Unity's <see cref="Image"/> with per-instance material instancing, HDR color support, and optional shape mask textures.
    /// </summary>
    /// <remarks>
    /// <para>Assign a material that exposes a `_Color` property (for tint) and, optionally, a `_ShapeMask` texture slot. EnhancedImage duplicates that material at runtime so per-instance HDR adjustments stay local to the image.</para>
    /// <para>Upsides:</para>
    /// <list type="bullet">
    /// <item>
    /// <description>Automatically instantiates materials so HDR tints and mask assignments do not leak to other UI elements.</description>
    /// </item>
    /// <item>
    /// <description>Supports shape masks without relying on additional UI mask components, enabling custom shader workflows.</description>
    /// </item>
    /// <item>
    /// <description>Falls back to the underlying <see cref="Graphic.color"/> whenever HDR values are not required.</description>
    /// </item>
    /// </list>
    /// <para>Downsides:</para>
    /// <list type="bullet">
    /// <item>
    /// <description>Creates and manages a material copy per instance, which adds allocation and cleanup overhead.</description>
    /// </item>
    /// <item>
    /// <description>Requires shaders that expose the `_ShapeMask` slot to benefit from mask-driven outlines or wipes.</description>
    /// </item>
    /// </list>
    /// <para>Reach for <see cref="EnhancedImage"/> when you need per-control HDR highlights, stylised wipes, or shader-driven reveal effects. Prefer the stock <see cref="Image"/> when shared materials and low overhead matter more than these features.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// using UnityEngine;
    /// using WallstopStudios.UnityHelpers.Visuals.UGUI;
    ///
    /// public sealed class AbilityIconPresenter : MonoBehaviour
    /// {
    ///     [SerializeField] private EnhancedImage icon;
    ///     [SerializeField] private Material abilityMaterial;
    ///
    ///     void Awake()
    ///     {
    ///         icon.material = Instantiate(abilityMaterial);
    ///         icon.HdrColor = new Color(1.6f, 1.2f, 0.6f, 1f);
    ///     }
    ///
    ///     public void SetCharge(float normalizedCharge)
    ///     {
    ///         float intensity = Mathf.Lerp(1f, 2.5f, normalizedCharge);
    ///         icon.HdrColor = new Color(intensity, 1f, 0.4f, 1f);
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="Image"/>
    public sealed class EnhancedImage : Image
    {
        private static readonly int ShapeMaskPropertyID = Shader.PropertyToID("_ShapeMask");
        private static readonly int ColorPropertyID = Shader.PropertyToID("_Color");

        /// <summary>
        /// Stores the dedicated material instance produced by <see cref="UpdateMaterialInstance"/>.
        /// </summary>
        private Material _cachedMaterialInstance;

        /// <summary>
        /// HDR-capable tint applied to the instantiated material. Values above 1 keep their intensity instead of being clamped.
        /// </summary>
        /// <remarks>
        /// Changing this value refreshes the cached material instance, using <see cref="Graphic.color"/> whenever the supplied color remains within the standard dynamic range.
        /// </remarks>
        public Color HdrColor
        {
            get => _hdrColor;
            set
            {
                if (_hdrColor == value)
                {
                    return;
                }

                _hdrColor = value;
                UpdateMaterialInstance();
            }
        }

        /// <summary>
        /// Optional shape mask texture assigned to the material's `_ShapeMask` slot to drive custom shader based masking.
        /// </summary>
        /// <remarks>
        /// Mimics UI mask behaviour without additional components. Provide a shader that samples `_ShapeMask` alpha and multiplies it with the sprite alpha.
        /// </remarks>
        [FormerlySerializedAs("shapeMask")]
        [SerializeField]
        internal Texture2D _shapeMask;

        /// <summary>
        /// Backing field for <see cref="HdrColor"/>, persisted for inspector integration and HDR authoring.
        /// </summary>
        // HDR Color field that will override the base Image color if values set > 1
        [FormerlySerializedAs("hdrColor")]
        [SerializeField]
        [ColorUsage(showAlpha: true, hdr: true)]
        internal Color _hdrColor = Color.white;

        /// <inheritdoc/>
        protected override void Start()
        {
            base.Start();
            UpdateMaterialInstance();
        }

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            // Ensure our instance is released before base classes tear down internals
            CleanupMaterialInstance();
            base.OnDestroy();
        }

#if UNITY_EDITOR
        /// <inheritdoc/>
        protected override void OnValidate()
        {
            base.OnValidate();
            UpdateMaterialInstance();
        }
#endif

        /// <summary>
        /// Ensures this component owns a dedicated material instance and reapplies mask and color data.
        /// </summary>
        private void UpdateMaterialInstance()
        {
            Material localMaterial = material;
            // Treat the built-in default UI material the same as "no material assigned"
            // so tests that explicitly set material = null do not cause an instance to be created.
            if (localMaterial == null || ReferenceEquals(localMaterial, defaultGraphicMaterial))
            {
                return;
            }

            // Cleanup old instance if it exists and is different from the base material
            if (_cachedMaterialInstance != null && _cachedMaterialInstance != localMaterial)
            {
                // Destroy immediately to ensure tests and teardown observe a released instance
                DestroyImmediate(_cachedMaterialInstance);
                _cachedMaterialInstance = null;
            }

            // Create new instance only if we don't have one
            if (_cachedMaterialInstance == null)
            {
                _cachedMaterialInstance = new Material(localMaterial);
            }

            if (_shapeMask != null)
            {
                // If the shader does not expose _ShapeMask, try to swap to a helper shader
                // that defines the property so tests and editor UX remain predictable.
                if (!_cachedMaterialInstance.HasProperty(ShapeMaskPropertyID))
                {
                    Shader fallback = Shader.Find("Hidden/Wallstop/EnhancedImageSupport");
                    if (fallback != null)
                    {
                        // Preserve commonly used properties when swapping shaders
                        Texture mainTex = _cachedMaterialInstance.HasProperty("_MainTex")
                            ? _cachedMaterialInstance.GetTexture("_MainTex")
                            : null;
                        Color currentColor = _cachedMaterialInstance.HasProperty(ColorPropertyID)
                            ? _cachedMaterialInstance.GetColor(ColorPropertyID)
                            : Color.white;

                        _cachedMaterialInstance.shader = fallback;

                        if (mainTex != null)
                        {
                            _cachedMaterialInstance.SetTexture("_MainTex", mainTex);
                        }
                        _cachedMaterialInstance.SetColor(ColorPropertyID, currentColor);
                    }
                }

                if (_cachedMaterialInstance.HasProperty(ShapeMaskPropertyID))
                {
                    _cachedMaterialInstance.SetTexture(ShapeMaskPropertyID, _shapeMask);
                }
            }

            _cachedMaterialInstance.SetColor(
                ColorPropertyID,
                _hdrColor.maxColorComponent > 1 ? _hdrColor : color
            );
            material = _cachedMaterialInstance;
        }

        /// <summary>
        /// Releases the cached material instance created for this image.
        /// </summary>
        private void CleanupMaterialInstance()
        {
            if (_cachedMaterialInstance != null)
            {
                // Use immediate destruction so references become fake-null right away
                DestroyImmediate(_cachedMaterialInstance);
                _cachedMaterialInstance = null;
            }
        }
    }
}
