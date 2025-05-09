namespace WallstopStudios.UnityHelpers.UI
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Extension;
    using Core.Helper;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;
    using Utils;
    using Debug = UnityEngine.Debug;

    public readonly struct AnimatedSpriteLayer
    {
        public const float FrameRate = 12f;

        public readonly Vector2[] perFramePixelOffsets;
        public readonly Sprite[] frames;
        public readonly float alpha;

        public AnimatedSpriteLayer(
            IEnumerable<Sprite> sprites,
            IEnumerable<Vector2> worldSpaceOffsets = null,
            float alpha = 1
        )
        {
            frames = sprites?.ToArray() ?? Array.Empty<Sprite>();
            foreach (Sprite frame in frames)
            {
                if (frame == null)
                {
                    continue;
                }

                frame.texture.MakeReadable();
                try
                {
                    frame.texture.GetPixel(0, 0);
                }
                catch (UnityException e)
                {
                    Debug.LogError(
                        $"Texture '{frame.texture.name}' for sprite '{frame.name}' is not readable. Please enable Read/Write in its import settings. Error: {e.Message}"
                    );
                }
            }

            if (worldSpaceOffsets != null && frames is { Length: > 0 })
            {
                perFramePixelOffsets = worldSpaceOffsets
                    .Zip(
                        frames,
                        (offset, frame) =>
                            frame != null && frame.pixelsPerUnit > 0
                                ? frame.pixelsPerUnit * offset
                                : Vector2.zero
                    )
                    .ToArray();
                Debug.Assert(
                    perFramePixelOffsets.Length == frames.Length,
                    $"Expected {frames.Length} sprite frames to match {perFramePixelOffsets.Length} offsets after processing."
                );
            }
            else
            {
                perFramePixelOffsets = null;
            }

            this.alpha = Mathf.Clamp01(alpha);
        }

        public AnimatedSpriteLayer(
            AnimationClip clip,
            IEnumerable<Vector2> worldSpaceOffsets = null,
            float alpha = 1
        )
            : this(
#if UNITY_EDITOR
                clip.GetSpritesFromClip(),
#else
                Enumerable.Empty<Sprite>(),
#endif
                worldSpaceOffsets, alpha) { }
    }

    public sealed class LayeredImage : VisualElement
    {
        private readonly AnimatedSpriteLayer[] _layers;
        private readonly Texture2D[] _computed;
        private readonly Color _backgroundColor;
        private readonly Rect? _largestArea;

        public LayeredImage(
            IEnumerable<AnimatedSpriteLayer> inputSpriteLayers,
            Color? backgroundColor = null,
            float fps = AnimatedSpriteLayer.FrameRate
        )
        {
            _layers = inputSpriteLayers.ToArray();
            _backgroundColor = backgroundColor ?? Color.white;
            _computed = ComputeTextures().ToArray();
            _largestArea = null;

            foreach (Texture2D? computedTexture in _computed)
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

            Render(0);

            if (_computed.Length > 1 && fps > 0)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    TimeSpan lastTick = TimeSpan.Zero;
                    TimeSpan fpsSpan = TimeSpan.FromMilliseconds(1000f / fps);
                    int index = 0;
                    Stopwatch timer = Stopwatch.StartNew();
                    EditorApplication.update += Tick;
                    return;

                    void Tick()
                    {
                        if (panel == null)
                        {
                            EditorApplication.update -= Tick;
                            return;
                        }
                        TimeSpan elapsed = timer.Elapsed;
                        if (lastTick + fpsSpan >= elapsed)
                        {
                            return;
                        }

                        index = index.WrappedIncrement(_computed.Length);
                        lastTick = elapsed;
                        Render(index);
                    }
                }
#endif
                if (Application.isPlaying && CoroutineHandler.Instance != null)
                {
                    int index = 0;
                    CoroutineHandler.Instance.StartFunctionAsCoroutine(
                        () =>
                        {
                            if (panel == null)
                            {
                                return;
                            }

                            index = index.WrappedIncrement(_computed.Length);
                            Render(index);
                        },
                        1f / fps
                    );
                }
            }
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

        private IEnumerable<Texture2D?> ComputeTextures()
        {
            const float pixelCutoff = 0.01f;
            if (_layers is not { Length: > 0 })
            {
                yield break;
            }

            int frameCount = 0;
            foreach (AnimatedSpriteLayer layer in _layers)
            {
                if (layer.frames != null)
                {
                    frameCount = Mathf.Max(frameCount, layer.frames.Length);
                }
            }
            if (frameCount == 0)
            {
                yield break;
            }

            for (int frameIndex = 0; frameIndex < frameCount; ++frameIndex)
            {
                float overallMinX = float.MaxValue;
                float overallMaxX = float.MinValue;
                float overallMinY = float.MaxValue;
                float overallMaxY = float.MinValue;
                bool hasVisibleSpriteThisFrame = false;

                foreach (AnimatedSpriteLayer layer in _layers)
                {
                    if (layer.frames == null || frameIndex >= layer.frames.Length)
                    {
                        continue;
                    }

                    Sprite sprite = layer.frames[frameIndex];
                    if (sprite == null)
                    {
                        continue;
                    }

                    hasVisibleSpriteThisFrame = true;
                    Rect spriteGeomRect = sprite.rect;
                    Vector2 pivot = sprite.pivot;

                    Vector2 additionalPixelOffset = Vector2.zero;
                    if (
                        layer.perFramePixelOffsets != null
                        && frameIndex < layer.perFramePixelOffsets.Length
                    )
                    {
                        additionalPixelOffset = layer.perFramePixelOffsets[frameIndex];
                    }

                    float spriteWorldMinX = -pivot.x + additionalPixelOffset.x;
                    float spriteWorldMaxX =
                        spriteGeomRect.width - pivot.x + additionalPixelOffset.x;
                    float spriteWorldMinY = -pivot.y + additionalPixelOffset.y;
                    float spriteWorldMaxY =
                        spriteGeomRect.height - pivot.y + additionalPixelOffset.y;

                    overallMinX = Mathf.Min(overallMinX, spriteWorldMinX);
                    overallMaxX = Mathf.Max(overallMaxX, spriteWorldMaxX);
                    overallMinY = Mathf.Min(overallMinY, spriteWorldMinY);
                    overallMaxY = Mathf.Max(overallMaxY, spriteWorldMaxY);
                }

                if (!hasVisibleSpriteThisFrame)
                {
                    yield return null;
                    continue;
                }

                int compositeBufferOriginX = Mathf.FloorToInt(overallMinX);
                int compositeBufferOriginY = Mathf.FloorToInt(overallMinY);
                int compositeBufferWidth = Mathf.CeilToInt(overallMaxX) - compositeBufferOriginX;
                int compositeBufferHeight = Mathf.CeilToInt(overallMaxY) - compositeBufferOriginY;

                if (compositeBufferWidth <= 0 || compositeBufferHeight <= 0)
                {
                    yield return null;
                    continue;
                }

                Color[] bufferPixels = new Color[compositeBufferWidth * compositeBufferHeight];

                Array.Fill(bufferPixels, Color.clear);

                foreach (AnimatedSpriteLayer layer in _layers)
                {
                    if (layer.frames == null || frameIndex >= layer.frames.Length)
                    {
                        continue;
                    }

                    Sprite sprite = layer.frames[frameIndex];
                    if (sprite == null)
                    {
                        continue;
                    }

                    float layerAlpha = layer.alpha;
                    Texture2D spriteTexture = sprite.texture;
                    Rect spriteGeomRect = sprite.rect;
                    Vector2 pivot = sprite.pivot;

                    Vector2 additionalPixelOffset = Vector2.zero;
                    if (
                        layer.perFramePixelOffsets != null
                        && frameIndex < layer.perFramePixelOffsets.Length
                    )
                    {
                        additionalPixelOffset = layer.perFramePixelOffsets[frameIndex];
                    }

                    int spriteRectX = Mathf.FloorToInt(spriteGeomRect.x);
                    int spriteRectY = Mathf.FloorToInt(spriteGeomRect.y);
                    int spriteRectWidth = Mathf.FloorToInt(spriteGeomRect.width);
                    int spriteRectHeight = Mathf.FloorToInt(spriteGeomRect.height);

                    if (spriteRectWidth <= 0 || spriteRectHeight <= 0)
                    {
                        continue;
                    }

                    Color[] spriteRawPixels = spriteTexture.GetPixels(
                        spriteRectX,
                        spriteRectY,
                        spriteRectWidth,
                        spriteRectHeight
                    );

                    Parallel.For(
                        0,
                        spriteRectHeight,
                        sySprite =>
                        {
                            for (int sxSprite = 0; sxSprite < spriteRectWidth; ++sxSprite)
                            {
                                Color spritePixelColor = spriteRawPixels[
                                    sySprite * spriteRectWidth + sxSprite
                                ];

                                if (spritePixelColor.a < pixelCutoff)
                                {
                                    continue;
                                }

                                float pixelWorldX = sxSprite - pivot.x + additionalPixelOffset.x;
                                float pixelWorldY = sySprite - pivot.y + additionalPixelOffset.y;
                                int bufferX = Mathf.FloorToInt(
                                    pixelWorldX - compositeBufferOriginX
                                );
                                int bufferY = Mathf.FloorToInt(
                                    pixelWorldY - compositeBufferOriginY
                                );

                                if (
                                    bufferX < 0
                                    || bufferX >= compositeBufferWidth
                                    || bufferY < 0
                                    || bufferY >= compositeBufferHeight
                                )
                                {
                                    continue;
                                }

                                int bufferIndex = bufferY * compositeBufferWidth + bufferX;
                                Color existingColor = bufferPixels[bufferIndex];
                                if (existingColor.a < pixelCutoff)
                                {
                                    existingColor = _backgroundColor;
                                }

                                Color blendedColor = Color.Lerp(
                                    existingColor,
                                    spritePixelColor,
                                    layerAlpha
                                );

                                bufferPixels[bufferIndex] = blendedColor;
                            }
                        }
                    );
                }

                int finalMinX = int.MaxValue,
                    finalMaxX = int.MinValue;
                int finalMinY = int.MaxValue,
                    finalMaxY = int.MinValue;

                Parallel.For(
                    0,
                    compositeBufferHeight * compositeBufferWidth,
                    bufferIndex =>
                    {
                        if (bufferPixels[bufferIndex].a >= pixelCutoff)
                        {
                            int x = bufferIndex % compositeBufferWidth;
                            int y = bufferIndex / compositeBufferWidth;

                            int currentVal;
                            do
                            {
                                currentVal = Volatile.Read(ref finalMinX);
                            } while (
                                x < currentVal
                                && Interlocked.CompareExchange(ref finalMinX, x, currentVal)
                                    != currentVal
                            );
                            do
                            {
                                currentVal = Volatile.Read(ref finalMaxX);
                            } while (
                                x > currentVal
                                && Interlocked.CompareExchange(ref finalMaxX, x, currentVal)
                                    != currentVal
                            );
                            do
                            {
                                currentVal = Volatile.Read(ref finalMinY);
                            } while (
                                y < currentVal
                                && Interlocked.CompareExchange(ref finalMinY, y, currentVal)
                                    != currentVal
                            );
                            do
                            {
                                currentVal = Volatile.Read(ref finalMaxY);
                            } while (
                                y > currentVal
                                && Interlocked.CompareExchange(ref finalMaxY, y, currentVal)
                                    != currentVal
                            );
                        }
                    }
                );

                if (finalMinX == int.MaxValue)
                {
                    yield return null;
                    continue;
                }

                int finalWidth = finalMaxX - finalMinX + 1;
                int finalHeight = finalMaxY - finalMinY + 1;

                Color[] finalPixels = new Color[finalWidth * finalHeight];

                Array.Fill(finalPixels, _backgroundColor);
                Parallel.For(
                    0,
                    finalHeight,
                    yFinal =>
                    {
                        for (int xFinal = 0; xFinal < finalWidth; ++xFinal)
                        {
                            int bufferX = finalMinX + xFinal;
                            int bufferY = finalMinY + yFinal;
                            Color pixelColor = bufferPixels[
                                bufferY * compositeBufferWidth + bufferX
                            ];

                            if (pixelColor.a >= pixelCutoff)
                            {
                                finalPixels[yFinal * finalWidth + xFinal] = pixelColor;
                            }
                        }
                    }
                );

                Texture2D finalTexture = new(
                    finalWidth,
                    finalHeight,
                    TextureFormat.RGBA32,
                    mipChain: false,
                    linear: false
                );

                finalTexture.SetPixels(finalPixels);
                finalTexture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
                yield return finalTexture;
            }
        }
    }
}
