/*
    Original implementation provided by JWoe
 */

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("WallstopStudios.UnityHelpers.Editor", AllInternalsVisible = true)]

namespace WallstopStudios.UnityHelpers.Visuals.UGUI
{
    using UnityEngine;
    using UnityEngine.Serialization;
    using UnityEngine.UI;

    public sealed class EnhancedImage : Image
    {
        private static readonly int ShapeMaskPropertyID = Shader.PropertyToID("_ShapeMask");
        private static readonly int ColorPropertyID = Shader.PropertyToID("_Color");

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

        /*
            ShapeMask: This functionality mimics Unity's UI mask components, but works with custom materials.
            To function, your image material must use a shader with a Texture2D property of this name, its
            alpha mapped to the sprite shader's alpha.
         */
        [FormerlySerializedAs("shapeMask")]
        [SerializeField]
        internal Texture2D _shapeMask;

        // HDR Color field that will override the base Image color if values set > 1
        [FormerlySerializedAs("hdrColor")]
        [SerializeField]
        [ColorUsage(showAlpha: true, hdr: true)]
        internal Color _hdrColor = Color.white;

        protected override void Start()
        {
            base.Start();
            UpdateMaterialInstance();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            UpdateMaterialInstance();
        }
#endif

        private void UpdateMaterialInstance()
        {
            Material localMaterial = material;
            if (localMaterial == null)
            {
                return;
            }

            Material materialInstance = new(localMaterial);
            if (_shapeMask != null)
            {
                materialInstance.SetTexture(ShapeMaskPropertyID, _shapeMask);
            }

            materialInstance.SetColor(
                ColorPropertyID,
                _hdrColor.maxColorComponent > 1 ? _hdrColor : color
            );
            material = materialInstance;
        }
    }
}
