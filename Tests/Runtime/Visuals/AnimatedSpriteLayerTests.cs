namespace WallstopStudios.UnityHelpers.Tests.Visuals
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Visuals;

    public sealed class AnimatedSpriteLayerTests
    {
        private readonly List<Object> _tracked = new();

        [TearDown]
        public void Cleanup()
        {
            VisualsTestHelpers.DestroyTracked(_tracked);
        }

        [Test]
        public void ConstructingFromEnumerablePreservesFrameOrder()
        {
            Sprite first = VisualsTestHelpers.CreateSprite(
                _tracked,
                2,
                2,
                (_, _) => new Color(1f, 0f, 0f, 1f)
            );
            Sprite second = VisualsTestHelpers.CreateSprite(
                _tracked,
                2,
                2,
                (_, _) => new Color(0f, 1f, 0f, 1f)
            );

            AnimatedSpriteLayer layer = new(YieldSprites(first, second), alpha: 0.5f);

            Assert.That(layer.frames, Has.Length.EqualTo(2));
            Assert.That(layer.frames[0], Is.EqualTo(first));
            Assert.That(layer.frames[1], Is.EqualTo(second));
            Assert.That(layer.perFramePixelOffsets, Is.Null);
            Assert.That(layer.alpha, Is.EqualTo(0.5f));
        }

        [Test]
        public void ConstructingWithOffsetsScalesByPixelsPerUnit()
        {
            Sprite first = VisualsTestHelpers.CreateSprite(
                _tracked,
                2,
                2,
                (_, _) => new Color(1f, 1f, 1f, 1f),
                pixelsPerUnit: 50f,
                pivot: Vector2.zero
            );
            Sprite second = VisualsTestHelpers.CreateSprite(
                _tracked,
                2,
                2,
                (_, _) => new Color(0f, 0f, 1f, 1f),
                pixelsPerUnit: 50f,
                pivot: Vector2.zero
            );

            AnimatedSpriteLayer layer = new(
                new[] { first, second },
                new[] { new Vector2(0.5f, -0.25f), new Vector2(-0.5f, 0.25f) }
            );

            Assert.That(layer.perFramePixelOffsets, Is.Not.Null);
            Assert.That(layer.perFramePixelOffsets, Has.Length.EqualTo(2));
            Assert.IsTrue(
                layer
                    .perFramePixelOffsets[0]
                    .Approximately(
                        new Vector2(25f, -12.5f),
                        mode: WallMath.VectorApproximationMode.Components
                    )
            );
            Assert.IsTrue(
                layer
                    .perFramePixelOffsets[1]
                    .Approximately(
                        new Vector2(-25f, 12.5f),
                        mode: WallMath.VectorApproximationMode.Components
                    )
            );
        }

        [Test]
        public void ConstructingWithMoreOffsetsThanFramesTrimsOffsets()
        {
            Sprite frame = VisualsTestHelpers.CreateSprite(
                _tracked,
                1,
                1,
                (_, _) => new Color(1f, 1f, 1f, 1f),
                pixelsPerUnit: 10f,
                pivot: Vector2.zero
            );

            AnimatedSpriteLayer layer = new(
                new[] { frame },
                new[] { new Vector2(0.1f, 0f), new Vector2(0.2f, 0f), new Vector2(0.3f, 0f) }
            );

            Assert.That(layer.perFramePixelOffsets, Is.Not.Null);
            Assert.That(layer.perFramePixelOffsets, Has.Length.EqualTo(1));
            Assert.IsTrue(
                layer
                    .perFramePixelOffsets[0]
                    .Approximately(
                        new Vector2(1f, 0f),
                        mode: WallMath.VectorApproximationMode.Components
                    )
            );
        }

        [Test]
        public void AlphaIsClampedToValidRange()
        {
            Sprite frame = VisualsTestHelpers.CreateSprite(
                _tracked,
                1,
                1,
                (_, _) => new Color(1f, 0f, 0f, 1f)
            );

            AnimatedSpriteLayer high = new(new[] { frame }, alpha: 3f);
            AnimatedSpriteLayer low = new(new[] { frame }, alpha: -1f);

            Assert.That(high.alpha, Is.EqualTo(1f));
            Assert.That(low.alpha, Is.EqualTo(0f));
        }

        [Test]
        public void ConstructingWithNullSpritesProducesEmptyLayer()
        {
            AnimatedSpriteLayer layer = new((IEnumerable<Sprite>)null);

            Assert.That(layer.frames, Is.Empty);
            Assert.That(layer.perFramePixelOffsets, Is.Null);
            Assert.That(layer.alpha, Is.EqualTo(1f));
        }

        [Test]
        public void EqualsComparesFramesOffsetsAndAlpha()
        {
            Sprite frameA = VisualsTestHelpers.CreateSprite(
                _tracked,
                1,
                1,
                (_, _) => new Color(0f, 1f, 0f, 1f)
            );
            Sprite frameB = VisualsTestHelpers.CreateSprite(
                _tracked,
                1,
                1,
                (_, _) => new Color(0f, 0f, 1f, 1f)
            );

            AnimatedSpriteLayer left = new(new[] { frameA, frameB }, alpha: 0.75f);
            AnimatedSpriteLayer right = new(new[] { frameA, frameB }, alpha: 0.75f);
            AnimatedSpriteLayer differentAlpha = new(new[] { frameA, frameB }, alpha: 0.25f);
            AnimatedSpriteLayer differentFrames = new(new[] { frameA }, alpha: 0.75f);

            Assert.IsTrue(left.Equals(right));
            Assert.IsTrue(left == right);
            Assert.IsFalse(left.Equals(differentAlpha));
            Assert.IsFalse(left.Equals(differentFrames));
            Assert.IsFalse(left == differentAlpha);
            Assert.IsTrue(left != differentFrames);
        }

        private static IEnumerable<Sprite> YieldSprites(params Sprite[] sprites)
        {
            foreach (Sprite sprite in sprites)
            {
                yield return sprite;
            }
        }
    }
}
