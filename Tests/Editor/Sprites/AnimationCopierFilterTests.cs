namespace WallstopStudios.UnityHelpers.Tests.Editor.Sprites
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Editor.Sprites;

    public sealed class AnimationCopierFilterTests
    {
        private static AnimationCopierWindow.AnimationFileInfo NewFileInfo(string name)
        {
            return new AnimationCopierWindow.AnimationFileInfo { FileName = name };
        }

        [Test]
        public void SortsAscendingAndDescending()
        {
            AnimationCopierWindow wnd = ScriptableObject.CreateInstance<AnimationCopierWindow>();
            var a = NewFileInfo("zeta.anim");
            var b = NewFileInfo("alpha.anim");
            var c = NewFileInfo("beta.anim");
            var items = new List<AnimationCopierWindow.AnimationFileInfo> { a, b, c };

            wnd._filterText = string.Empty;
            wnd._filterUseRegex = false;
            wnd._sortAscending = true;
            var asc = wnd.ApplyFilterAndSort(items).ToList();
            string[] ascNames = asc.Select(o => o.FileName).ToArray();
            CollectionAssert.AreEqual(new[] { "alpha.anim", "beta.anim", "zeta.anim" }, ascNames);

            wnd._sortAscending = false;
            var desc = wnd.ApplyFilterAndSort(items).ToList();
            string[] descNames = desc.Select(o => o.FileName).ToArray();
            CollectionAssert.AreEqual(new[] { "zeta.anim", "beta.anim", "alpha.anim" }, descNames);
        }

        [Test]
        public void FiltersBySubstringAndRegex()
        {
            AnimationCopierWindow wnd = ScriptableObject.CreateInstance<AnimationCopierWindow>();
            var a = NewFileInfo("walk.anim");
            var b = NewFileInfo("attack.anim");
            var c = NewFileInfo("idle.anim");
            var items = new List<AnimationCopierWindow.AnimationFileInfo> { a, b, c };

            wnd._filterText = "ta";
            wnd._filterUseRegex = false;
            wnd._sortAscending = true;
            var sub = wnd.ApplyFilterAndSort(items).ToList();
            string[] subNames = sub.Select(o => o.FileName).ToArray();
            CollectionAssert.AreEquivalent(new[] { "attack.anim" }, subNames);

            wnd._filterText = "^i";
            wnd._filterUseRegex = true;
            var rx = wnd.ApplyFilterAndSort(items).ToList();
            string[] rxNames = rx.Select(o => o.FileName).ToArray();
            CollectionAssert.AreEquivalent(new[] { "idle.anim" }, rxNames);
        }
    }
#endif
}
