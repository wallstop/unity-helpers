namespace WallstopStudios.UnityHelpers.Tests.Editor.Sprites
{
#if UNITY_EDITOR
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Editor.Sprites;

    public sealed class AnimationViewerWindowTests
    {
        [Test]
        public void EditorLayerDataBuildsSpriteListFromClip()
        {
            // Build a test clip with 3 sprite keyframes
            AnimationClip clip = new AnimationClip();
            Texture2D tex = new Texture2D(2, 2);
            Sprite s1 = Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.zero);
            Sprite s2 = Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.zero);
            Sprite s3 = Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.zero);

            EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(
                string.Empty,
                typeof(SpriteRenderer),
                "m_Sprite"
            );
            ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[3];
            keys[0] = new ObjectReferenceKeyframe { time = 0f, value = s1 };
            keys[1] = new ObjectReferenceKeyframe { time = 0.1f, value = s2 };
            keys[2] = new ObjectReferenceKeyframe { time = 0.2f, value = s3 };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            var instance = new AnimationViewerWindow.EditorLayerData(clip);
            Assert.NotNull(instance);
            Assert.AreEqual(3, instance.Sprites.Count);
        }
    }
#endif
}
