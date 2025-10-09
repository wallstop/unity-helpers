namespace WallstopStudios.UnityHelpers.Visuals.UIToolkit
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Utils;

    public sealed class LayeredImage : VisualElement
    {
        public float Fps
        {
            get => _fps;
            set
            {
                if (_fps == value)
                {
                    return;
                }

                _fps = value;
                if (_updatesSelf && _computed.Length > 1 && _fps > 0)
                {
#if UNITY_EDITOR
                    if (Application.isEditor && !Application.isPlaying && !_tickAttached)
                    {
                        EditorApplication.update += () => Update(force: false);
                        _tickAttached = true;
                        return;
                    }
#endif
                    if (Application.isPlaying)
                    {
                        if (_coroutine != null)
                        {
                            CoroutineHandler.Instance.StopCoroutine(_coroutine);
                        }

                        _coroutine = CoroutineHandler.Instance.StartFunctionAsCoroutine(
                            () => Update(force: true),
                            1f / _fps
                        );
                    }
                }
            }
        }

        private const int ParallelBlendThreshold = 2048;

        private readonly AnimatedSpriteLayer[] _layers;
        private readonly Texture2D[] _computed;
        private readonly Color _backgroundColor;
        private readonly Rect? _largestArea;
        private readonly Stopwatch _timer;
        private readonly bool _updatesSelf;
        private readonly float _pixelCutoff;

        private TimeSpan _lastTick;
        private Coroutine _coroutine;
        private bool _tickAttached;
        private float _fps;
        private int _index;

        public LayeredImage(
            IEnumerable<AnimatedSpriteLayer> inputSpriteLayers,
            Color? backgroundColor = null,
            float fps = AnimatedSpriteLayer.FrameRate,
            bool updatesSelf = true,
            float pixelCutoff = 0.01f
        )
        {
            _pixelCutoff = pixelCutoff;
            _layers = inputSpriteLayers.ToArray();
            _backgroundColor = backgroundColor ?? Color.white;
            _computed = ComputeTextures().ToArray();
            _largestArea = null;
            _updatesSelf = updatesSelf;

            foreach (Texture2D computedTexture in _computed)
            {
                if (computedTexture == null)
                {
                    continue;
                }

                if (_largestArea == null)
                {
                    _largestArea = new Rect(0, 0, computedTexture.width, computedTexture.height);
                }
                else
                {
                    Rect currentLargest = _largestArea.Value;
                    currentLargest.width = Mathf.Max(currentLargest.width, computedTexture.width);
                    currentLargest.height = Mathf.Max(
                        currentLargest.height,
                        computedTexture.height
                    );
                    _largestArea = currentLargest;
                }
            }

            _timer = Stopwatch.StartNew();
            Fps = fps;
            Update();
        }

        public void Update(bool force = false)
        {
            if (panel == null)
            {
                return;
            }

            if (_computed.Length == 0)
            {
                return;
            }

            TimeSpan elapsed = _timer.Elapsed;
            TimeSpan deltaTime = TimeSpan.FromMilliseconds(1000 / _fps);
            if (!force && _lastTick + deltaTime > elapsed)
            {
                return;
            }

            _index = _index.WrappedIncrement(_computed.Length);
            _lastTick += deltaTime;
            Render(_index);
        }

        private void Render(int index)
        {
            if (index < 0 || index >= _computed.Length)
            {
                return;
            }

            Texture2D computed = _computed[index];
            if (computed != null)
            {
                style.backgroundImage = computed;
                style.width = computed.width;
                style.height = computed.height;
            }
            else
            {
                style.backgroundImage = null;
                style.width = _largestArea?.width ?? 0;
                style.height = _largestArea?.height ?? 0;
            }

            style.marginRight = 0;
            style.marginBottom = 0;
            if (_largestArea == null)
            {
                return;
            }

            Rect largestAreaRect = _largestArea.Value;
            float currentWidth = computed != null ? computed.width : _largestArea?.width ?? 0;
            float currentHeight = computed != null ? computed.height : _largestArea?.height ?? 0;

            if (currentWidth < largestAreaRect.width)
            {
                style.marginRight = largestAreaRect.width - currentWidth;
            }
            if (currentHeight < largestAreaRect.height)
            {
                style.marginBottom = largestAreaRect.height - currentHeight;
            }
        }

        private Texture2D[] ComputeTextures()
        {
            if (_layers is not { Length: > 0 })
            {
                yield break;
            }

            AnimatedSpriteLayer[] layers = _layers;
            int frameCount = 0;
            for (int layerIndex = 0; layerIndex < layers.Length; ++layerIndex)
            {
                Sprite[] layerFrames = layers[layerIndex].frames;
                if (layerFrames == null)
                {
                    continue;
                }

                if (layerFrames.Length > frameCount)
                {
                    frameCount = layerFrames.Length;
                }
            }

            if (frameCount == 0)
            {
                yield break;
            }

            Color backgroundColor = _backgroundColor;
            float pixelCutoff = _pixelCutoff;

            for (int frameIndex = 0; frameIndex < frameCount; ++frameIndex)
            {
                float overallMinX = float.MaxValue;
                float overallMaxX = float.MinValue;
                float overallMinY = float.MaxValue;
                float overallMaxY = float.MinValue;
                bool hasVisibleSprite = false;

                for (int layerIndex = 0; layerIndex < layers.Length; ++layerIndex)
                {
                    ref readonly AnimatedSpriteLayer layer = ref layers[layerIndex];
                    if (
                        layer.alpha <= 0f
                        || !TryGetLayerFrame(layer, frameIndex, out Sprite sprite)
                    )
                    {
                        continue;
                    }

                    hasVisibleSprite = true;

                    Rect spriteRect = sprite.rect;
                    Vector2 pivot = sprite.pivot;
                    Vector2 pixelOffset = GetPixelOffset(layer, frameIndex);

                    float spriteMinX = -pivot.x + pixelOffset.x;
                    float spriteMaxX = spriteRect.width - pivot.x + pixelOffset.x;
                    float spriteMinY = -pivot.y + pixelOffset.y;
                    float spriteMaxY = spriteRect.height - pivot.y + pixelOffset.y;

                    if (spriteMinX < overallMinX)
                    {
                        overallMinX = spriteMinX;
                    }

                    if (spriteMaxX > overallMaxX)
                    {
                        overallMaxX = spriteMaxX;
                    }

                    if (spriteMinY < overallMinY)
                    {
                        overallMinY = spriteMinY;
                    }

                    if (spriteMaxY > overallMaxY)
                    {
                        overallMaxY = spriteMaxY;
                    }
                }

                if (!hasVisibleSprite)
                {
                    yield return null;
                    continue;
                }

                int compositeOriginX = Mathf.FloorToInt(overallMinX);
                int compositeOriginY = Mathf.FloorToInt(overallMinY);
                int compositeWidth = Mathf.CeilToInt(overallMaxX) - compositeOriginX;
                int compositeHeight = Mathf.CeilToInt(overallMaxY) - compositeOriginY;

                if (compositeWidth <= 0 || compositeHeight <= 0)
                {
                    yield return null;
                    continue;
                }

                int compositeLength = compositeWidth * compositeHeight;
                using PooledResource<Color[]> compositeLease = WallstopFastArrayPool<Color>.Get(
                    compositeLength,
                    out Color[] bufferPixels
                );
                bufferPixels.AsSpan(0, compositeLength).Clear();

                for (int layerIndex = 0; layerIndex < layers.Length; ++layerIndex)
                {
                    ref readonly AnimatedSpriteLayer layer = ref layers[layerIndex];
                    float layerAlpha = layer.alpha;
                    if (layerAlpha <= 0f || !TryGetLayerFrame(layer, frameIndex, out Sprite sprite))
                    {
                        continue;
                    }

                    Texture2D spriteTexture = sprite.texture;
                    if (spriteTexture == null || !spriteTexture.isReadable)
                    {
                        continue;
                    }

                    Rect spriteRect = sprite.rect;
                    int spriteRectX = Mathf.FloorToInt(spriteRect.x);
                    int spriteRectY = Mathf.FloorToInt(spriteRect.y);
                    int spriteWidth = Mathf.FloorToInt(spriteRect.width);
                    int spriteHeight = Mathf.FloorToInt(spriteRect.height);

                    if (spriteWidth <= 0 || spriteHeight <= 0)
                    {
                        continue;
                    }

                    Color[] spritePixels = spriteTexture.GetPixels(
                        spriteRectX,
                        spriteRectY,
                        spriteWidth,
                        spriteHeight
                    );
                    Vector2 pixelOffset = GetPixelOffset(layer, frameIndex);
                    float baseX = pixelOffset.x - sprite.pivot.x - compositeOriginX;
                    float baseY = pixelOffset.y - sprite.pivot.y - compositeOriginY;

                    ComposeSpriteOntoBuffer(
                        bufferPixels,
                        compositeWidth,
                        compositeHeight,
                        spritePixels,
                        spriteWidth,
                        spriteHeight,
                        baseX,
                        baseY,
                        layerAlpha,
                        pixelCutoff
                    );
                }

                int minX = compositeWidth;
                int maxX = -1;
                int minY = compositeHeight;
                int maxY = -1;

                for (int y = 0; y < compositeHeight; ++y)
                {
                    int rowOffset = y * compositeWidth;
                    int rowMin = compositeWidth;
                    int rowMax = -1;

                    for (int x = 0; x < compositeWidth; ++x)
                    {
                        if (bufferPixels[rowOffset + x].a < pixelCutoff)
                        {
                            continue;
                        }

                        if (x < rowMin)
                        {
                            rowMin = x;
                        }

                        if (x > rowMax)
                        {
                            rowMax = x;
                        }
                    }

                    if (rowMax < rowMin)
                    {
                        continue;
                    }

                    if (y < minY)
                    {
                        minY = y;
                    }

                    if (y > maxY)
                    {
                        maxY = y;
                    }

                    if (rowMin < minX)
                    {
                        minX = rowMin;
                    }

                    if (rowMax > maxX)
                    {
                        maxX = rowMax;
                    }
                }

                if (maxX < minX || maxY < minY)
                {
                    yield return null;
                    continue;
                }

                int finalWidth = maxX - minX + 1;
                int finalHeight = maxY - minY + 1;
                int finalLength = finalWidth * finalHeight;

                using PooledResource<Color[]> finalLease = WallstopFastArrayPool<Color>.Get(
                    finalLength,
                    out Color[] finalPixels
                );
                Span<Color> finalSpan = finalPixels.AsSpan(0, finalLength);
                finalSpan.Fill(backgroundColor);

                for (int y = 0; y < finalHeight; ++y)
                {
                    int destinationRow = y * finalWidth;
                    int sourceRow = (minY + y) * compositeWidth + minX;

                    for (int x = 0; x < finalWidth; ++x)
                    {
                        Color pixel = bufferPixels[sourceRow + x];
                        if (pixel.a >= pixelCutoff)
                        {
                            finalPixels[destinationRow + x] = pixel;
                        }
                    }
                }

                Texture2D finalTexture = new Texture2D(
                    finalWidth,
                    finalHeight,
                    TextureFormat.RGBA32,
                    mipChain: false,
                    linear: false
                )
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                };

                finalTexture.SetPixels(finalPixels);
                finalTexture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
                yield return finalTexture;
            }
        }

        private static void ComposeSpriteOntoBuffer(
            Color[] bufferPixels,
            int bufferWidth,
            int bufferHeight,
            Color[] spritePixels,
            int spriteWidth,
            int spriteHeight,
            float baseX,
            float baseY,
            float layerAlpha,
            float pixelCutoff
        )
        {
            if (spriteWidth == 0 || spriteHeight == 0)
            {
                return;
            }

            if (spriteWidth * spriteHeight >= ParallelBlendThreshold)
            {
                Parallel.For(
                    0,
                    spriteHeight,
                    y =>
                        BlendSpriteRow(
                            bufferPixels,
                            bufferWidth,
                            bufferHeight,
                            spritePixels,
                            spriteWidth,
                            baseX,
                            baseY,
                            layerAlpha,
                            pixelCutoff,
                            y
                        )
                );
                return;
            }

            for (int y = 0; y < spriteHeight; ++y)
            {
                BlendSpriteRow(
                    bufferPixels,
                    bufferWidth,
                    bufferHeight,
                    spritePixels,
                    spriteWidth,
                    baseX,
                    baseY,
                    layerAlpha,
                    pixelCutoff,
                    y
                );
            }
        }

        private static void BlendSpriteRow(
            Color[] bufferPixels,
            int bufferWidth,
            int bufferHeight,
            Color[] spritePixels,
            int spriteWidth,
            float baseX,
            float baseY,
            float layerAlpha,
            float pixelCutoff,
            int spriteRow
        )
        {
            int spriteRowOffset = spriteRow * spriteWidth;
            float targetY = baseY + spriteRow;
            int bufferY = Mathf.FloorToInt(targetY);
            if (bufferY < 0 || bufferY >= bufferHeight)
            {
                return;
            }

            int bufferRowOffset = bufferY * bufferWidth;

            for (int spriteColumn = 0; spriteColumn < spriteWidth; ++spriteColumn)
            {
                Color spritePixel = spritePixels[spriteRowOffset + spriteColumn];
                float spriteAlpha = spritePixel.a * layerAlpha;
                if (spriteAlpha <= pixelCutoff)
                {
                    continue;
                }

                if (spriteAlpha > 1f)
                {
                    spriteAlpha = 1f;
                }

                int bufferX = Mathf.FloorToInt(baseX + spriteColumn);
                if (bufferX < 0 || bufferX >= bufferWidth)
                {
                    continue;
                }

                int bufferIndex = bufferRowOffset + bufferX;
                ref Color destination = ref bufferPixels[bufferIndex];

                float destinationAlpha = destination.a;
                if (destinationAlpha <= pixelCutoff)
                {
                    destination.r = spritePixel.r;
                    destination.g = spritePixel.g;
                    destination.b = spritePixel.b;
                    destination.a = spriteAlpha;
                    continue;
                }

                float inverseSourceAlpha = 1f - spriteAlpha;
                float outAlpha = spriteAlpha + destinationAlpha * inverseSourceAlpha;
                if (outAlpha <= pixelCutoff)
                {
                    destination = Color.clear;
                    continue;
                }

                if (outAlpha > 1f)
                {
                    outAlpha = 1f;
                }

                float invOutAlpha = 1f / outAlpha;
                float sourceWeight = spriteAlpha * invOutAlpha;
                float destinationWeight = destinationAlpha * inverseSourceAlpha * invOutAlpha;

                float destR = destination.r;
                float destG = destination.g;
                float destB = destination.b;

                destination.r = spritePixel.r * sourceWeight + destR * destinationWeight;
                destination.g = spritePixel.g * sourceWeight + destG * destinationWeight;
                destination.b = spritePixel.b * sourceWeight + destB * destinationWeight;
                destination.a = outAlpha;
            }
        }

        private static bool TryGetLayerFrame(
            in AnimatedSpriteLayer layer,
            int frameIndex,
            out Sprite sprite
        )
        {
            Sprite[] frames = layer.frames;
            if (frames == null || frameIndex < 0 || frameIndex >= frames.Length)
            {
                sprite = null;
                return false;
            }

            sprite = frames[frameIndex];
            return sprite != null;
        }

        private static Vector2 GetPixelOffset(in AnimatedSpriteLayer layer, int frameIndex)
        {
            Vector2[] offsets = layer.perFramePixelOffsets;
            if (offsets == null || frameIndex < 0 || frameIndex >= offsets.Length)
            {
                return Vector2.zero;
            }

            return offsets[frameIndex];
        }
    }
}
