// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Editor;

    public sealed class PrefabCheckerReportTests
    {
        [Test]
        public void ScanReportConstructorCopiesFolders()
        {
            PrefabChecker.ScanReport report = new(new[] { "A", "B" });
            string[] folders = report.folders;
            CollectionAssert.AreEqual(new[] { "A", "B" }, folders);
        }

        [Test]
        public void ScanReportAddCopiesMessages()
        {
            PrefabChecker.ScanReport report = new(Array.Empty<string>());
            report.Add("path.prefab", new List<string> { "m1", "m2" });
            Assert.AreEqual(1, report.items.Count);
            PrefabChecker.ScanReport.Item first = report.items[0];
            Assert.AreEqual("path.prefab", first.path);
            string[] messages = first.messages;
            CollectionAssert.AreEqual(new[] { "m1", "m2" }, messages);
        }
    }
#endif
}
