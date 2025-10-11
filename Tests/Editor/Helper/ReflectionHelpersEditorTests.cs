namespace WallstopStudios.UnityHelpers.Tests.Editor.Helper
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;

    [TestFixture]
    public sealed class ReflectionHelpersEditorTests
    {
        [Test]
        public void EditorGetTypesDerivedFromUsesTypeCacheFallback()
        {
            IEnumerable<Type> derived = ReflectionHelpers.GetTypesDerivedFrom(
                typeof(Component),
                includeAbstract: false
            );
            bool found = derived.Any(t =>
                t == typeof(RelationalComponentInitializerTests.PrewarmTesterComponent)
            );
            Assert.IsTrue(
                found,
                "Expected PrewarmTesterComponent in editor type cache derived set."
            );
        }
    }
#endif
}
