namespace WallstopStudios.UnityHelpers.Tests.Tools
{
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Editor;
    using WallstopStudios.UnityHelpers.Tests.Core;

    [TestFixture]
    public sealed class AnimationEventEditorSmokeTests : CommonTestBase
    {
        [Test]
        public void AnimationEventEditorOpensAndClosesWithoutAnimator()
        {
            AnimationEventEditor first = EditorWindow.CreateWindow<AnimationEventEditor>();
            try
            {
                first.Show();
                first.Repaint();
            }
            finally
            {
                first.Close();
                Object.DestroyImmediate(first);
            }

            AnimationEventEditor second = EditorWindow.CreateWindow<AnimationEventEditor>();
            try
            {
                second.Show();
                second.Repaint();
            }
            finally
            {
                second.Close();
                Object.DestroyImmediate(second);
            }
        }
    }
}
