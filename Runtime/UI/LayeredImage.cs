namespace UnityHelpers.UI
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

        public readonly Vector2[] offsets;
        public readonly Sprite[] frames;
        public readonly float alpha;

        public AnimatedSpriteLayer(
            IEnumerable<Sprite> sprites,
            IEnumerable<Vector2> offsets,
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
            }

            this.offsets =
                offsets?.Zip(frames, (offset, frame) => frame.pixelsPerUnit * offset).ToArray()
                ?? Array.Empty<Vector2>();
            Debug.Assert(
                this.offsets.Length == frames.Length,
                $"Expected {frames.Length} to match {this.offsets.Length}"
            );
            this.alpha = Mathf.Clamp01(alpha);
        }

        public AnimatedSpriteLayer(
            AnimationClip clip,
            IEnumerable<Vector2> offsets,
            float alpha = 1
        )
            : this(
#if UNITY_EDITOR
                clip.GetSpritesFromClip(),
#else
                Enumerable.Empty<Sprite>(),
#endif
                offsets, alpha) { }
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
            foreach (Texture2D computed in _computed)
            {
                if (_largestArea == null)
                {
                    _largestArea = new Rect(0, 0, computed.width, computed.height);
                }
                else
                {
                    Rect largestArea = _largestArea.Value;
                    largestArea.width = Mathf.Max(largestArea.width, computed.width);
                    largestArea.height = Mathf.Max(largestArea.height, computed.height);
                    _largestArea = largestArea;
                }
            }

            Render(0);
            float fpsMs = 1000f / fps;
            if (1 < _computed.Length)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    TimeSpan lastTick = TimeSpan.Zero;
                    TimeSpan fpsSpan = TimeSpan.FromMilliseconds(fpsMs);
                    int index = 0;
                    Stopwatch timer = Stopwatch.StartNew();
                    EditorApplication.update += () =>
                    {
                        TimeSpan elapsed = timer.Elapsed;
                        if (lastTick + fpsSpan < elapsed)
                        {
                            index = index.WrappedIncrement(_computed.Length);
                            lastTick = elapsed;
                            Render(index);
                        }
                    };
                    return;
                }

#endif
                {
                    int index = 0;
                    CoroutineHandler.Instance.StartFunctionAsCoroutine(
                        () =>
                        {
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
            Texture2D computed = _computed[index];
            if (computed != null)
            {
                style.backgroundImage = computed;
                style.width = computed.width;
                style.height = computed.height;
            }

            style.marginRight = 0;
            style.marginBottom = 0;
            if (_largestArea != null)
            {
                Rect largestArea = _largestArea.Value;
                if (style.width.value.value < largestArea.width)
                {
                    style.marginRight = largestArea.width - style.width.value.value;
                }

                if (style.height.value.value < largestArea.height)
                {
                    style.marginBottom = largestArea.height - style.height.value.value;
                }
            }
        }

        private IEnumerable<Texture2D> ComputeTextures()
        {
            const float pixelCutoff = 0.01f;
            int frameCount = _layers.Select(layer => layer.frames.Length).Distinct().Single();

            Color transparent = Color.clear;
            for (int frameIndex = 0; frameIndex < frameCount; ++frameIndex)
            {
                int minX = int.MaxValue;
                int maxX = int.MinValue;
                int minY = int.MaxValue;
                int maxY = int.MinValue;
                foreach (AnimatedSpriteLayer layer in _layers)
                {
                    if (!layer.frames.Any())
                    {
                        continue;
                    }

                    Sprite sprite = layer.frames[frameIndex];
                    Vector2 offset = layer.offsets[frameIndex];
                    Rect spriteRect = sprite.rect;

                    int left = Mathf.RoundToInt(offset.x + spriteRect.xMin);
                    int right = Mathf.RoundToInt(offset.x + spriteRect.xMax);
                    int bottom = Mathf.RoundToInt(offset.y + spriteRect.yMin);
                    int top = Mathf.RoundToInt(offset.y + spriteRect.yMax);

                    minX = Mathf.Min(minX, left);
                    maxX = Mathf.Max(maxX, right);
                    minY = Mathf.Min(minY, bottom);
                    maxY = Mathf.Max(maxY, top);
                }

                if (minX == int.MaxValue)
                {
                    continue;
                }

                // Calculate the width and height of the non-transparent region
                int width = maxX - minX + 1;
                int height = maxY - minY + 1;

                Color[] pixels = new Color[width * height];
                Array.Fill(pixels, Color.clear);

                foreach (AnimatedSpriteLayer layer in _layers)
                {
                    if (!layer.frames.Any())
                    {
                        continue;
                    }

                    Sprite sprite = layer.frames[frameIndex];
                    Vector2 offset = layer.offsets[frameIndex];
                    float alpha = layer.alpha;
                    int offsetX = Mathf.RoundToInt(offset.x);
                    int offsetY = Mathf.RoundToInt(offset.y);
                    Texture2D texture = sprite.texture;
                    Rect spriteRect = sprite.rect;

                    int spriteX = Mathf.RoundToInt(spriteRect.xMin);
                    int spriteWidth = Mathf.RoundToInt(spriteRect.width);
                    int spriteY = Mathf.RoundToInt(spriteRect.yMin);
                    int spriteHeight = Mathf.RoundToInt(spriteRect.height);
                    Color[] spritePixels = texture.GetPixels(
                        spriteX,
                        spriteY,
                        spriteWidth,
                        spriteHeight
                    );

                    Parallel.For(
                        0,
                        spritePixels.Length,
                        inIndex =>
                        {
                            int x = inIndex % spriteWidth;
                            int y = inIndex / spriteWidth;

                            Color pixelColor = spritePixels[inIndex];
                            if (pixelColor.a < pixelCutoff)
                            {
                                return;
                            }

                            int textureX = offsetX + x + spriteX;
                            int textureY = offsetY + y + spriteY;
                            int index = textureY * width + textureX;

                            if (index < 0 || pixels.Length <= index)
                            {
                                return;
                            }

                            Color existingColor = pixels[index];
                            if (existingColor == transparent)
                            {
                                existingColor = _backgroundColor;
                            }

                            Color blendedColor = Color.Lerp(existingColor, pixelColor, alpha);
                            pixels[index] = blendedColor;
                        }
                    );
                }

                // Find the bounds of the non-transparent pixels in the temporary texture
                int finalMinX = int.MaxValue;
                int finalMaxX = int.MinValue;
                int finalMinY = int.MaxValue;
                int finalMaxY = int.MinValue;

                Parallel.For(
                    0,
                    height * width,
                    inIndex =>
                    {
                        Color pixelColor = pixels[inIndex];
                        if (pixelColor.a < pixelCutoff)
                        {
                            return;
                        }

                        int x = inIndex % width;
                        int y = inIndex / width;

                        int expectedX = finalMinX;
                        while (x < expectedX)
                        {
                            expectedX = Interlocked.CompareExchange(ref finalMinX, x, expectedX);
                        }

                        expectedX = finalMaxX;
                        while (expectedX < x)
                        {
                            expectedX = Interlocked.CompareExchange(ref finalMaxX, x, expectedX);
                        }

                        int expectedY = finalMinY;
                        while (y < expectedY)
                        {
                            expectedY = Interlocked.CompareExchange(ref finalMinY, y, expectedY);
                        }

                        expectedY = finalMaxY;
                        while (expectedY < y)
                        {
                            expectedY = Interlocked.CompareExchange(ref finalMaxY, y, expectedY);
                        }
                    }
                );

                if (finalMinX == int.MaxValue)
                {
                    continue;
                }

                // Calculate the final width and height of the culled texture
                int finalWidth = finalMaxX - finalMinX + 1;
                int finalHeight = finalMaxY - finalMinY + 1;

                Color[] finalPixels = new Color[finalWidth * finalHeight];
                Array.Fill(finalPixels, _backgroundColor);

                // Copy the non-transparent pixels from the temporary texture to the final texture
                Parallel.For(
                    0,
                    finalWidth * finalHeight,
                    inIndex =>
                    {
                        int x = inIndex % finalWidth;
                        int y = inIndex / finalWidth;
                        int outerX = x + finalMinX;
                        int outerY = y + finalMinY;
                        Color pixelColor = pixels[outerY * width + outerX];
                        if (pixelColor.a < pixelCutoff)
                        {
                            return;
                        }
                        finalPixels[y * finalWidth + x] = pixelColor;
                    }
                );

                Texture2D finalTexture = new(
                    finalWidth,
                    finalHeight,
                    TextureFormat.RGBA32,
                    mipChain: false,
                    linear: false,
                    createUninitialized: true
                );
                finalTexture.SetPixels(finalPixels);
                finalTexture.Apply(false, false);

                yield return finalTexture;
            }
        }
    }
}
