// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Sprites
{
#if UNITY_EDITOR
    using System.IO;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Editor.Sprites;
    using WallstopStudios.UnityHelpers.Editor.Utils;
    using WallstopStudios.UnityHelpers.Tests.Core;

    public sealed class AnimationCopierWindowTests : CommonTestBase
    {
        private const string SrcRoot = "Assets/Temp/AnimationCopierTests/Src";
        private const string DstRoot = "Assets/Temp/AnimationCopierTests/Dst";
        private bool _prevPrompt;
        private bool _previousEditorUiSuppress;

        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            _previousEditorUiSuppress = EditorUi.Suppress;
            EditorUi.Suppress = true;
            EnsureFolder(SrcRoot);
            EnsureFolder(DstRoot);
            _prevPrompt = AnimationCopierWindow.SuppressUserPrompts;
            AnimationCopierWindow.SuppressUserPrompts = true;
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            // Clean up only tracked folders/assets that this test created
            CleanupTrackedFoldersAndAssets();
            AnimationCopierWindow.SuppressUserPrompts = _prevPrompt;
            EditorUi.Suppress = _previousEditorUiSuppress;
        }

        private static void ImportAssetIfExists(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            if (
                AssetDatabase.IsValidFolder(assetPath)
                || AssetDatabase.LoadAssetAtPath<Object>(assetPath) != null
            )
            {
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            }
        }

        [Test]
        public void AnalyzeDetectsNewChangedUnchangedAndOrphans()
        {
            string srcA = Path.Combine(SrcRoot, "A.anim").SanitizePath();
            CreateEmptyClip(srcA);
            AssetDatabase.SaveAssets();
            ImportAssetIfExists(srcA);

            AnimationCopierWindow window = CreateWindow();
            window.AnimationSourcePathRelative = SrcRoot;
            window.AnimationDestinationPathRelative = DstRoot;
            window.AnalyzeAnimations();

            int newCount = window.NewCount;
            int changedCount = window.ChangedCount;
            int unchangedCount = window.UnchangedCount;
            int orphansCount = window.OrphansCount;
            Assert.AreEqual(1, newCount);
            Assert.AreEqual(0, changedCount);
            Assert.AreEqual(0, unchangedCount);
            Assert.AreEqual(0, orphansCount);

            // Create destination copy to become unchanged
            string dstA = Path.Combine(DstRoot, "A.anim").SanitizePath();
            Assert.IsTrue(AssetDatabase.CopyAsset(srcA, dstA));
            AssetDatabase.SaveAssets();
            ImportAssetIfExists(dstA);

            // Modify source so it becomes changed vs. destination
            ModifyClip(srcA);
            AssetDatabase.SaveAssets();
            ImportAssetIfExists(srcA);

            // Add orphan in destination
            string dstB = Path.Combine(DstRoot, "B.anim").SanitizePath();
            CreateEmptyClip(dstB);
            AssetDatabase.SaveAssets();
            ImportAssetIfExists(dstB);

            window.AnalyzeAnimations();

            newCount = window.NewCount;
            changedCount = window.ChangedCount;
            unchangedCount = window.UnchangedCount;
            orphansCount = window.OrphansCount;

            Assert.AreEqual(0, newCount);
            Assert.AreEqual(1, changedCount);
            Assert.AreEqual(0, unchangedCount);
            Assert.AreEqual(1, orphansCount);
        }

        [Test]
        public void CopyChangedPreservesGuidAndOverwrites()
        {
            string srcA = Path.Combine(SrcRoot, "A.anim").SanitizePath();
            string dstA = Path.Combine(DstRoot, "A.anim").SanitizePath();
            CreateEmptyClip(srcA);
            AssetDatabase.SaveAssets();
            ImportAssetIfExists(srcA);
            Assert.IsTrue(AssetDatabase.CopyAsset(srcA, dstA));
            AssetDatabase.SaveAssets();
            ImportAssetIfExists(dstA);

            string guidBefore = AssetDatabase.AssetPathToGUID(dstA);
            // Modify source to force change
            ModifyClip(srcA);
            AssetDatabase.SaveAssets();
            ImportAssetIfExists(srcA);

            AnimationCopierWindow window = CreateWindow();
            window.AnimationSourcePathRelative = SrcRoot;
            window.AnimationDestinationPathRelative = DstRoot;
            window.DryRun = false;

            window.AnalyzeAnimations();
            window.CopyChanged();

            string guidAfter = AssetDatabase.AssetPathToGUID(dstA);
            Assert.AreEqual(
                guidBefore,
                guidAfter,
                "Destination GUID should be preserved after overwrite."
            );
        }

        [Test]
        public void MirrorDeleteRemovesOrphansWhenNotDryRun()
        {
            string srcA = Path.Combine(SrcRoot, "A.anim").SanitizePath();
            string dstB = Path.Combine(DstRoot, "B.anim").SanitizePath();
            CreateEmptyClip(srcA);
            CreateEmptyClip(dstB);
            AssetDatabase.SaveAssets();
            ImportAssetIfExists(srcA);
            ImportAssetIfExists(dstB);

            AnimationCopierWindow window = CreateWindow();
            window.AnimationSourcePathRelative = SrcRoot;
            window.AnimationDestinationPathRelative = DstRoot;
            window.DryRun = false;

            window.AnalyzeAnimations();

            // Ensure orphan exists
            Assert.Greater(window.OrphansCount, 0);

            window.MirrorDeleteDestinationAnimations();
            AssetDatabase.SaveAssets();
            ImportAssetIfExists(DstRoot);

            Assert.IsFalse(
                File.Exists(ToFull(dstB)) || AssetDatabase.LoadMainAssetAtPath(dstB) != null,
                "Orphan should be deleted"
            );
        }

        // Helpers
        private static string ToFull(string rel) =>
            Path.Combine(
                    Application.dataPath.Substring(
                        0,
                        Application.dataPath.Length - "Assets".Length
                    ),
                    rel
                )
                .SanitizePath();

        private void CreateEmptyClip(string relPath)
        {
            string dir = Path.GetDirectoryName(relPath).SanitizePath();
            EnsureFolder(dir);
            AnimationClip clip = new();
            AssetDatabase.CreateAsset(clip, relPath);
            TrackAssetPath(relPath);
        }

        private static void ModifyClip(string relPath)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(relPath);
            Assert.IsTrue(clip != null);
            clip.frameRate = clip.frameRate + 1f;
            EditorUtility.SetDirty(clip);
        }

        private AnimationCopierWindow CreateWindow()
        {
            return Track(ScriptableObject.CreateInstance<AnimationCopierWindow>());
        }
    }
#endif
}
