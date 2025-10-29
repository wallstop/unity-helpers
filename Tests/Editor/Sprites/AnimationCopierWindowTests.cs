namespace WallstopStudios.UnityHelpers.Tests.Editor.Sprites
{
#if UNITY_EDITOR
    using System.IO;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Editor.Sprites;
    using WallstopStudios.UnityHelpers.Tests.Editor.Utils;

    public sealed class AnimationCopierWindowTests : CommonTestBase
    {
        private const string SrcRoot = "Assets/Temp/AnimationCopierTests/Src";
        private const string DstRoot = "Assets/Temp/AnimationCopierTests/Dst";
        private bool _prevPrompt;

        [SetUp]
        public void SetUp()
        {
            EnsureFolder(SrcRoot);
            EnsureFolder(DstRoot);
            _prevPrompt = AnimationCopierWindow.SuppressUserPrompts;
            AnimationCopierWindow.SuppressUserPrompts = true;
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            AssetDatabase.DeleteAsset("Assets/Temp/AnimationCopierTests");
            AssetDatabase.Refresh();
            AnimationCopierWindow.SuppressUserPrompts = _prevPrompt;
        }

        [Test]
        public void AnalyzeDetectsNewChangedUnchangedAndOrphans()
        {
            string srcA = Path.Combine(SrcRoot, "A.anim").SanitizePath();
            CreateEmptyClip(srcA);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

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
            AssetDatabase.Refresh();

            // Modify source so it becomes changed vs. destination
            ModifyClip(srcA);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Add orphan in destination
            string dstB = Path.Combine(DstRoot, "B.anim").SanitizePath();
            CreateEmptyClip(dstB);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

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
            AssetDatabase.Refresh();
            Assert.IsTrue(AssetDatabase.CopyAsset(srcA, dstA));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string guidBefore = AssetDatabase.AssetPathToGUID(dstA);
            // Modify source to force change
            ModifyClip(srcA);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

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
            AssetDatabase.Refresh();

            AnimationCopierWindow window = CreateWindow();
            window.AnimationSourcePathRelative = SrcRoot;
            window.AnimationDestinationPathRelative = DstRoot;
            window.DryRun = false;

            window.AnalyzeAnimations();

            // Ensure orphan exists
            Assert.Greater(window.OrphansCount, 0);

            window.MirrorDeleteDestinationAnimations();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Assert.IsFalse(
                File.Exists(ToFull(dstB)) || AssetDatabase.LoadMainAssetAtPath(dstB) != null,
                "Orphan should be deleted"
            );
        }

        // Helpers
        private static void EnsureFolder(string relPath)
        {
            string[] parts = relPath.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(cur, parts[i]);
                }
                cur = next;
            }
        }

        private static string ToFull(string rel) =>
            Path.Combine(
                    Application.dataPath.Substring(
                        0,
                        Application.dataPath.Length - "Assets".Length
                    ),
                    rel
                )
                .Replace('\\', '/');

        private static void CreateEmptyClip(string relPath)
        {
            string dir = Path.GetDirectoryName(relPath).Replace('\\', '/');
            EnsureFolder(dir);
            AnimationClip clip = new();
            AssetDatabase.CreateAsset(clip, relPath);
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
