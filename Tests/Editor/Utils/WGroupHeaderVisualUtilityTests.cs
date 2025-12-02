namespace WallstopStudios.UnityHelpers.Tests.Utils
{
#if UNITY_EDITOR
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Editor.Utils.WGroup;

    [TestFixture]
    public sealed class WGroupHeaderVisualUtilityTests
    {
        [Test]
        public void GetContentRect_ShiftsWhenFoldoutSpaceRequested()
        {
            Rect headerRect = new Rect(0f, 0f, 200f, 24f);

            Rect withoutFoldout = WGroupHeaderVisualUtility.GetContentRect(headerRect, 0f, 0f);

            Rect withFoldout = WGroupHeaderVisualUtility.GetContentRect(headerRect, 0f, 0f, true);

            Assert.That(withFoldout.xMin, Is.GreaterThan(withoutFoldout.xMin));
            Assert.That(withFoldout.xMax, Is.EqualTo(withoutFoldout.xMax).Within(0.0001f));
        }
    }
#endif
}
