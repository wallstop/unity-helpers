// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Tools
{
    using NUnit.Framework;
    using UnityEditor;
    using WallstopStudios.UnityHelpers.Editor;
    using WallstopStudios.UnityHelpers.Tests.Core;

    [TestFixture]
    public sealed class AnimationEventEditorSmokeTests : CommonTestBase
    {
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
