namespace WallstopStudios.UnityHelpers.Tests.Editor
{
    using System;
    using System.IO;
    using System.Linq;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Visuals.UIToolkit;

    public sealed class MultiFileSelectorElementTests
    {
        private string _baseRel;
        private string _baseAbs;

        [SetUp]
        public void SetUp()
        {
            _baseRel = "Assets/TempMultiFileSelectorTests";
            _baseAbs = Path.Combine(Application.dataPath, "TempMultiFileSelectorTests");
            if (Directory.Exists(_baseAbs))
            {
                Directory.Delete(_baseAbs, recursive: true);
            }

            Directory.CreateDirectory(_baseAbs);
            Directory.CreateDirectory(Path.Combine(_baseAbs, "DirA"));
            Directory.CreateDirectory(Path.Combine(_baseAbs, "DirB"));

            File.WriteAllText(Path.Combine(_baseAbs, "a.txt"), "a");
            File.WriteAllText(Path.Combine(_baseAbs, "image.bytes"), "img");
            File.WriteAllText(Path.Combine(_baseAbs, "foo.txt"), "foo");
            File.WriteAllText(Path.Combine(_baseAbs, "bar.txt"), "bar");
            File.WriteAllText(Path.Combine(_baseAbs, "DirA/nested.txt"), "nested");

            AssetDatabase.Refresh();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean persisted keys used in tests
            EditorPrefs.DeleteKey("WallstopStudios.MultiFileSelector.lastSearch.TestScope1");
            EditorPrefs.DeleteKey("WallstopStudios.MultiFileSelector.lastSearch.KeyA");
            EditorPrefs.DeleteKey("WallstopStudios.MultiFileSelector.lastSearch.KeyB");
            EditorPrefs.DeleteKey("WallstopStudios.MultiFileSelector.lastSearch.SelScope");
            EditorPrefs.DeleteKey("WallstopStudios.MultiFileSelector.lastDirectory.DirKey");
            EditorPrefs.DeleteKey("WallstopStudios.MultiFileSelector.lastDirectory.DirTestA");
            EditorPrefs.DeleteKey("WallstopStudios.MultiFileSelector.lastUsed.KeyA");
            EditorPrefs.DeleteKey("WallstopStudios.MultiFileSelector.lastUsed.KeyB");
            EditorPrefs.DeleteKey("WallstopStudios.MultiFileSelector.scopes");

            AssetDatabase.DeleteAsset(_baseRel);
            AssetDatabase.Refresh();
        }

        [Test]
        public void FolderFirstAndSorting()
        {
            var selector = new MultiFileSelectorElement(_baseRel, null, "TestScope1");
            var names = selector.DebugGetVisibleEntryNames();

            Assert.That(
                names.Count,
                Is.GreaterThanOrEqualTo(5),
                "Expected directories and files to be listed."
            );
            Assert.That(
                names[0],
                Is.EqualTo("DirA"),
                "Folders should be sorted and appear before files."
            );
            Assert.That(names[1], Is.EqualTo("DirB"), "Second folder should be DirB.");
        }

        [Test]
        public void ExtensionFilterNormalizationWorks()
        {
            var selector = new MultiFileSelectorElement(_baseRel, new[] { "txt" }, "TestScopeExt");
            var names = selector.DebugGetVisibleEntryNames();

            CollectionAssert.Contains(names, "a.txt");
            CollectionAssert.Contains(names, "foo.txt");
            CollectionAssert.Contains(names, "bar.txt");
            CollectionAssert.DoesNotContain(names, "image.bytes");
        }

        [Test]
        public void SearchPersistenceIsScoped()
        {
            EditorPrefs.SetString("WallstopStudios.MultiFileSelector.lastSearch.KeyA", "foo");
            EditorPrefs.SetString("WallstopStudios.MultiFileSelector.lastSearch.KeyB", "bar");

            var selA = new MultiFileSelectorElement(_baseRel, null, "KeyA");
            var selB = new MultiFileSelectorElement(_baseRel, null, "KeyB");

            var namesA = selA.DebugGetVisibleEntryNames();
            var namesB = selB.DebugGetVisibleEntryNames();

            Assert.That(
                namesA.Any(n => n.Equals("foo.txt", StringComparison.OrdinalIgnoreCase)),
                "Selector A should include foo.txt by search."
            );
            Assert.False(
                namesA.Any(n => n.Equals("bar.txt", StringComparison.OrdinalIgnoreCase)),
                "Selector A should not include bar.txt by search."
            );

            Assert.That(
                namesB.Any(n => n.Equals("bar.txt", StringComparison.OrdinalIgnoreCase)),
                "Selector B should include bar.txt by search."
            );
            Assert.False(
                namesB.Any(n => n.Equals("foo.txt", StringComparison.OrdinalIgnoreCase)),
                "Selector B should not include foo.txt by search."
            );
        }

        [Test]
        public void NoPersistenceWhenNoKey()
        {
            EditorPrefs.SetString("WallstopStudios.MultiFileSelector.lastSearch", "foo");
            var selector = new MultiFileSelectorElement(_baseRel, null);
            var names = selector.DebugGetVisibleEntryNames();

            // With no key, search should not be loaded; both foo and bar files should be present
            Assert.That(
                names.Any(n => n.Equals("foo.txt", StringComparison.OrdinalIgnoreCase)),
                "Expected foo.txt to be visible without scoped persistence."
            );
            Assert.That(
                names.Any(n => n.Equals("bar.txt", StringComparison.OrdinalIgnoreCase)),
                "Expected bar.txt to be visible without scoped persistence."
            );
        }

        [Test]
        public void SelectionHelpersAffectSelection()
        {
            var selector = new MultiFileSelectorElement(_baseRel, new[] { ".txt" }, "SelScope");

            var methodSelectAll = typeof(MultiFileSelectorElement).GetMethod(
                "SelectAllInView",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
            );
            var methodInvert = typeof(MultiFileSelectorElement).GetMethod(
                "InvertSelectionInView",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
            );
            var methodClear = typeof(MultiFileSelectorElement).GetMethod(
                "ClearSelectionInView",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
            );

            methodSelectAll.Invoke(selector, null);
            var selected = selector.DebugGetSelectedFilePaths();
            Assert.That(
                selected.Count,
                Is.EqualTo(3),
                "Select All should select all visible .txt files."
            );

            methodInvert.Invoke(selector, null);
            selected = selector.DebugGetSelectedFilePaths();
            Assert.That(
                selected.Count,
                Is.EqualTo(0),
                "Invert on fully selected should deselect all in view."
            );

            methodClear.Invoke(selector, null);
            selected = selector.DebugGetSelectedFilePaths();
            Assert.That(selected.Count, Is.EqualTo(0), "Clear should result in zero selection.");

            methodInvert.Invoke(selector, null);
            selected = selector.DebugGetSelectedFilePaths();
            Assert.That(
                selected.Count,
                Is.EqualTo(3),
                "Invert on empty should select all in view."
            );
        }

        [Test]
        public void DirectoryPersistenceIsScoped()
        {
            var selector = new MultiFileSelectorElement(_baseRel, null, "DirKey");
            // Navigate to DirA via reflection (private method)
            var navigate = typeof(MultiFileSelectorElement).GetMethod(
                "NavigateTo",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
            );
            navigate.Invoke(selector, new object[] { _baseRel + "/DirA" });

            // New selector with same scope should open in DirA
            var selector2 = new MultiFileSelectorElement(_baseRel, null, "DirKey");
            var names = selector2.DebugGetVisibleEntryNames();
            CollectionAssert.Contains(
                names,
                "nested.txt",
                "Expected to be inside DirA due to scoped directory persistence."
            );
        }

        [Test]
        public void DisabledPersistenceDoesNotLoadOrSave()
        {
            // No key provided implies no persistence; pre-set a global key should not affect behavior
            EditorPrefs.SetString("WallstopStudios.MultiFileSelector.lastSearch", "foo");
            var selector = new MultiFileSelectorElement(_baseRel, null);
            var names = selector.DebugGetVisibleEntryNames();
            Assert.That(
                names.Any(n => n.Equals("bar.txt", StringComparison.OrdinalIgnoreCase)),
                "No key means no scoped loading; do not filter by previous value."
            );
        }

        [Test]
        public void CleanupRemovesStaleScopes()
        {
            // Create two scopes, mark one stale
            EditorPrefs.SetString("WallstopStudios.MultiFileSelector.scopes", "OldScope;NewScope");
            long oldTicks = DateTime.UtcNow.AddDays(-90).Ticks;
            long newTicks = DateTime.UtcNow.Ticks;
            EditorPrefs.SetString(
                "WallstopStudios.MultiFileSelector.lastUsed.OldScope",
                oldTicks.ToString()
            );
            EditorPrefs.SetString("WallstopStudios.MultiFileSelector.lastSearch.OldScope", "foo");
            EditorPrefs.SetString(
                "WallstopStudios.MultiFileSelector.lastDirectory.OldScope",
                "Assets"
            );

            EditorPrefs.SetString(
                "WallstopStudios.MultiFileSelector.lastUsed.NewScope",
                newTicks.ToString()
            );
            EditorPrefs.SetString("WallstopStudios.MultiFileSelector.lastSearch.NewScope", "bar");
            EditorPrefs.SetString(
                "WallstopStudios.MultiFileSelector.lastDirectory.NewScope",
                "Assets"
            );

            MultiFileSelectorElement.CleanupStalePersistenceEntries(TimeSpan.FromDays(30));

            Assert.False(
                EditorPrefs.HasKey("WallstopStudios.MultiFileSelector.lastUsed.OldScope"),
                "Old scope lastUsed should be removed."
            );
            Assert.False(
                EditorPrefs.HasKey("WallstopStudios.MultiFileSelector.lastSearch.OldScope"),
                "Old scope search should be removed."
            );
            Assert.False(
                EditorPrefs.HasKey("WallstopStudios.MultiFileSelector.lastDirectory.OldScope"),
                "Old scope directory should be removed."
            );

            Assert.True(
                EditorPrefs.HasKey("WallstopStudios.MultiFileSelector.lastUsed.NewScope"),
                "New scope lastUsed should be retained."
            );
            Assert.True(
                EditorPrefs.HasKey("WallstopStudios.MultiFileSelector.lastSearch.NewScope"),
                "New scope search should be retained."
            );
            Assert.True(
                EditorPrefs.HasKey("WallstopStudios.MultiFileSelector.lastDirectory.NewScope"),
                "New scope directory should be retained."
            );

            string scopes = EditorPrefs.GetString("WallstopStudios.MultiFileSelector.scopes", "");
            Assert.That(
                scopes,
                Is.EqualTo("NewScope"),
                "Scopes index should be pruned to survivors."
            );
        }
    }
}
