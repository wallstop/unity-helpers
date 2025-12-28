// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Sprites
{
#if UNITY_EDITOR
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Editor.Sprites;
    using WallstopStudios.UnityHelpers.Tests.Core;

    public sealed class AnimationCopierFilterTests : CommonTestBase
    {
        private static AnimationCopierWindow.AnimationFileInfo NewFileInfo(string name)
        {
            return new AnimationCopierWindow.AnimationFileInfo { FileName = name };
        }

        [Test]
        public void SortsAscendingAndDescending()
        {
            AnimationCopierWindow wnd = Track(
                ScriptableObject.CreateInstance<AnimationCopierWindow>()
            );
            AnimationCopierWindow.AnimationFileInfo a = NewFileInfo("zeta.anim");
            AnimationCopierWindow.AnimationFileInfo b = NewFileInfo("alpha.anim");
            AnimationCopierWindow.AnimationFileInfo c = NewFileInfo("beta.anim");
            List<AnimationCopierWindow.AnimationFileInfo> items = new() { a, b, c };

            wnd._filterText = string.Empty;
            wnd._filterUseRegex = false;
            wnd._sortAscending = true;
            List<AnimationCopierWindow.AnimationFileInfo> asc = wnd.ApplyFilterAndSort(items)
                .ToList();
            string[] ascNames = asc.Select(o => o.FileName).ToArray();
            CollectionAssert.AreEqual(new[] { "alpha.anim", "beta.anim", "zeta.anim" }, ascNames);

            wnd._sortAscending = false;
            List<AnimationCopierWindow.AnimationFileInfo> desc = wnd.ApplyFilterAndSort(items)
                .ToList();
            string[] descNames = desc.Select(o => o.FileName).ToArray();
            CollectionAssert.AreEqual(new[] { "zeta.anim", "beta.anim", "alpha.anim" }, descNames);
        }

        [Test]
        public void FiltersBySubstringAndRegex()
        {
            AnimationCopierWindow wnd = Track(
                ScriptableObject.CreateInstance<AnimationCopierWindow>()
            );
            AnimationCopierWindow.AnimationFileInfo a = NewFileInfo("walk.anim");
            AnimationCopierWindow.AnimationFileInfo b = NewFileInfo("attack.anim");
            AnimationCopierWindow.AnimationFileInfo c = NewFileInfo("idle.anim");
            List<AnimationCopierWindow.AnimationFileInfo> items = new() { a, b, c };

            wnd._filterText = "ta";
            wnd._filterUseRegex = false;
            wnd._sortAscending = true;
            List<AnimationCopierWindow.AnimationFileInfo> sub = wnd.ApplyFilterAndSort(items)
                .ToList();
            string[] subNames = sub.Select(o => o.FileName).ToArray();
            CollectionAssert.AreEquivalent(new[] { "attack.anim" }, subNames);

            wnd._filterText = "^i";
            wnd._filterUseRegex = true;
            List<AnimationCopierWindow.AnimationFileInfo> rx = wnd.ApplyFilterAndSort(items)
                .ToList();
            string[] rxNames = rx.Select(o => o.FileName).ToArray();
            CollectionAssert.AreEquivalent(new[] { "idle.anim" }, rxNames);
        }
    }
#endif
}
