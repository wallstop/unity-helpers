namespace Samples.UnityHelpers.UGUI.EnhancedImage
{
    using UnityEngine;
    using UnityEngine.UI;
    using WallstopStudios.UnityHelpers.Visuals.UGUI;

    /// <summary>
    /// Creates a Canvas + EnhancedImage at runtime and applies an HDR tint.
    /// </summary>
    public sealed class EnhancedImageDemo : MonoBehaviour
    {
        [SerializeField]
        private Material materialTemplate;

        private void Start()
        {
            GameObject canvasGo = new GameObject("DemoCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            GameObject imageGo = new GameObject("EnhancedImage");
            imageGo.transform.SetParent(canvasGo.transform, false);
            EnhancedImage image = imageGo.AddComponent<EnhancedImage>();
            image.rectTransform.sizeDelta = new Vector2(200f, 200f);

            if (materialTemplate != null)
            {
                image.material = Object.Instantiate(materialTemplate);
            }

            image.HdrColor = new Color(1.6f, 1.2f, 0.8f, 1f);
        }
    }
}
