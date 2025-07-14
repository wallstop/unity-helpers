namespace WallstopStudios.UnityHelpers.Visuals
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Extension;
    using WallstopStudios.UnityHelpers.Core.Helper;

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
                if (Debug.isDebugBuild)
                {
                    Debug.Assert(
                        perFramePixelOffsets.Length == frames.Length,
                        $"Expected {frames.Length} sprite frames to match {perFramePixelOffsets.Length} offsets after processing."
                    );
                }
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
}
