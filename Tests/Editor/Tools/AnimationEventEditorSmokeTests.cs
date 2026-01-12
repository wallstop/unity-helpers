// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Tools
{
    using NUnit.Framework;
    using UnityEditor;
    using WallstopStudios.UnityHelpers.Editor;
    using WallstopStudios.UnityHelpers.Tests.Core;

    [TestFixture]
    [NUnit.Framework.Category("Slow")]
    [NUnit.Framework.Category("Integration")]
    public sealed class AnimationEventEditorSmokeTests : BatchedEditorTestBase
    {
        [SetUp]
        public void SetUp()
        {
            base.BaseSetUp();
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
        }

        [Test]
        public void AnimationEventEditorOpensAndClosesWithoutAnimator()
        {
            AnimationEventEditor first = Track(EditorWindow.CreateWindow<AnimationEventEditor>());
            first.Show();
            first.Repaint();
            first.Close();

            AnimationEventEditor second = Track(EditorWindow.CreateWindow<AnimationEventEditor>());
            second.Show();
            second.Repaint();
            second.Close();
        }
    }
}
