namespace WallstopStudios.UnityHelpers.UI
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Extension;
    using Core.Helper;
    using UnityEngine;
    using UnityEngine.UIElements;
    using Utils;
    using Debug = UnityEngine.Debug;
#if UNITY_EDITOR
    using UnityEditor;
#endif

    public readonly struct AnimatedSpriteLayer : IEquatable<AnimatedSpriteLayer>
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

        public static bool operator ==(AnimatedSpriteLayer left, AnimatedSpriteLayer right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(AnimatedSpriteLayer left, AnimatedSpriteLayer right)
        {
            return !left.Equals(right);
        }

        public bool Equals(AnimatedSpriteLayer other)
        {
            bool equal = perFramePixelOffsets.AsSpan().SequenceEqual(other.perFramePixelOffsets);
            if (!equal)
            {
                return false;
            }

            equal = frames.Length == other.frames.Length;
            if (!equal)
            {
                return false;
            }

            for (int i = 0; i < frames.Length; ++i)
            {
                if (frames[i] != other.frames[i])
                {
                    return false;
                }
            }

            return alpha.Equals(other.alpha);
        }

        public override bool Equals(object obj)
        {
            return obj is AnimatedSpriteLayer other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Objects.ValueTypeHashCode(perFramePixelOffsets.Length, frames.Length, alpha);
        }
    }

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

        private IEnumerable<Texture2D> ComputeTextures()
        {
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

                                if (spritePixelColor.a < _pixelCutoff)
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
                                if (existingColor.a < _pixelCutoff)
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

                int globalMinX = int.MaxValue;
                int globalMaxX = int.MinValue;
                int globalMinY = int.MaxValue;
                int globalMaxY = int.MinValue;
                object sync = new();

                Parallel.For(
                    0,
                    compositeBufferHeight,
                    () =>
                        (
                            minX: int.MaxValue,
                            maxX: int.MinValue,
                            minY: int.MaxValue,
                            maxY: int.MinValue
                        ),
                    (y, _, local) =>
                    {
                        int baseIndex = y * compositeBufferWidth;
                        for (int x = 0; x < compositeBufferWidth; ++x)
                        {
                            if (bufferPixels[baseIndex + x].a < _pixelCutoff)
                            {
                                continue;
                            }

                            if (x < local.minX)
                            {
                                local.minX = x;
                            }

                            if (x > local.maxX)
                            {
                                local.maxX = x;
                            }

                            if (y < local.minY)
                            {
                                local.minY = y;
                            }

                            if (y > local.maxY)
                            {
                                local.maxY = y;
                            }
                        }
                        return local;
                    },
                    local =>
                    {
                        lock (sync)
                        {
                            if (local.minX < globalMinX)
                            {
                                globalMinX = local.minX;
                            }

                            if (local.maxX > globalMaxX)
                            {
                                globalMaxX = local.maxX;
                            }

                            if (local.minY < globalMinY)
                            {
                                globalMinY = local.minY;
                            }

                            if (local.maxY > globalMaxY)
                            {
                                globalMaxY = local.maxY;
                            }
                        }
                    }
                );

                if (globalMinX == int.MaxValue)
                {
                    yield return null;
                    continue;
                }

                int finalWidth = globalMaxX - globalMinX + 1;
                int finalHeight = globalMaxY - globalMinY + 1;

                Color[] finalPixels = new Color[finalWidth * finalHeight];

                Array.Fill(finalPixels, _backgroundColor);
                Parallel.For(
                    0,
                    finalHeight,
                    yFinal =>
                    {
                        for (int xFinal = 0; xFinal < finalWidth; ++xFinal)
                        {
                            int bufferX = globalMinX + xFinal;
                            int bufferY = globalMinY + yFinal;
                            Color pixelColor = bufferPixels[
                                bufferY * compositeBufferWidth + bufferX
                            ];

                            if (pixelColor.a >= _pixelCutoff)
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
