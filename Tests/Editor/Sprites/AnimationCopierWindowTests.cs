namespace WallstopStudios.UnityHelpers.Tests.Editor.Sprites
{
#if UNITY_EDITOR
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;

    public sealed class AnimationCopierWindowTests
    {
        private const string SrcRoot = "Assets/Temp/AnimationCopierTests/Src";
        private const string DstRoot = "Assets/Temp/AnimationCopierTests/Dst";

        [SetUp]
        public void SetUp()
        {
            EnsureFolder(SrcRoot);
            EnsureFolder(DstRoot);
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset("Assets/Temp/AnimationCopierTests");
            AssetDatabase.Refresh();
        }

        [Test]
        public void AnalyzeDetectsNewChangedUnchangedAndOrphans()
        {
            string srcA = Path.Combine(SrcRoot, "A.anim").Replace('\\', '/');
            CreateEmptyClip(srcA);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var window = CreateWindow();
            SetStringField(window, "_animationSourcePathRelative", SrcRoot);
            SetStringField(window, "_animationDestinationPathRelative", DstRoot);

            Invoke(window, "ValidatePaths");
            Invoke(window, "AnalyzeAnimations");

            int newCount = GetListCount(window, "_newAnimations");
            int changedCount = GetListCount(window, "_changedAnimations");
            int unchangedCount = GetListCount(window, "_unchangedAnimations");
            int orphansCount = GetListCount(window, "_destinationOrphans");
            Assert.AreEqual(1, newCount);
            Assert.AreEqual(0, changedCount);
            Assert.AreEqual(0, unchangedCount);
            Assert.AreEqual(0, orphansCount);

            // Create destination copy to become unchanged
            string dstA = Path.Combine(DstRoot, "A.anim").Replace('\\', '/');
            Assert.IsTrue(AssetDatabase.CopyAsset(srcA, dstA));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Modify source so it becomes changed vs. destination
            ModifyClip(srcA);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Add orphan in destination
            string dstB = Path.Combine(DstRoot, "B.anim").Replace('\\', '/');
            CreateEmptyClip(dstB);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Invoke(window, "AnalyzeAnimations");

            newCount = GetListCount(window, "_newAnimations");
            changedCount = GetListCount(window, "_changedAnimations");
            unchangedCount = GetListCount(window, "_unchangedAnimations");
            orphansCount = GetListCount(window, "_destinationOrphans");

            Assert.AreEqual(0, newCount);
            Assert.AreEqual(1, changedCount);
            Assert.AreEqual(0, unchangedCount);
            Assert.AreEqual(1, orphansCount);
        }

        [Test]
        public void CopyChangedPreservesGuidAndOverwrites()
        {
            string srcA = Path.Combine(SrcRoot, "A.anim").Replace('\\', '/');
            string dstA = Path.Combine(DstRoot, "A.anim").Replace('\\', '/');
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

            var window = CreateWindow();
            SetStringField(window, "_animationSourcePathRelative", SrcRoot);
            SetStringField(window, "_animationDestinationPathRelative", DstRoot);
            SetBoolField(window, "_dryRun", false);

            Invoke(window, "ValidatePaths");
            Invoke(window, "AnalyzeAnimations");

            // Select only changed and run copy
            Invoke(window, "CopyAnimationsInternal", GetEnumValue(window, "CopyMode", "Changed"));

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
            string srcA = Path.Combine(SrcRoot, "A.anim").Replace('\\', '/');
            string dstB = Path.Combine(DstRoot, "B.anim").Replace('\\', '/');
            CreateEmptyClip(srcA);
            CreateEmptyClip(dstB);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var window = CreateWindow();
            SetStringField(window, "_animationSourcePathRelative", SrcRoot);
            SetStringField(window, "_animationDestinationPathRelative", DstRoot);
            SetBoolField(window, "_dryRun", false);

            Invoke(window, "ValidatePaths");
            Invoke(window, "AnalyzeAnimations");

            // Ensure orphan exists
            Assert.Greater(GetListCount(window, "_destinationOrphans"), 0);

            Invoke(window, "MirrorDeleteDestinationAnimations");
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
            AnimationClip clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, relPath);
        }

        private static void ModifyClip(string relPath)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(relPath);
            Assert.IsNotNull(clip);
            clip.frameRate = clip.frameRate + 1f;
            EditorUtility.SetDirty(clip);
        }

        private static ScriptableObject CreateWindow()
        {
            Type t = Type.GetType(
                "WallstopStudios.UnityHelpers.Editor.Sprites.AnimationCopierWindow, Assembly-CSharp-Editor"
            );
            Assert.IsNotNull(t, "AnimationCopierWindow type not found");
            return ScriptableObject.CreateInstance(t);
        }

        private static void SetStringField(object obj, string field, string value)
        {
            var f = obj.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, $"Field {field} not found");
            f.SetValue(obj, value);
        }

        private static void SetBoolField(object obj, string field, bool value)
        {
            var f = obj.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, $"Field {field} not found");
            f.SetValue(obj, value);
        }

        private static int GetListCount(object obj, string field)
        {
            var f = obj.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, $"Field {field} not found");
            var list = f.GetValue(obj) as System.Collections.ICollection;
            Assert.IsNotNull(list);
            return list.Count;
        }

        private static void Invoke(object obj, string method, params object[] args)
        {
            var m = obj.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(m, $"Method {method} not found");
            m.Invoke(obj, args);
        }

        private static object GetEnumValue(object obj, string enumName, string valueName)
        {
            var nested = obj.GetType().GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public);
            var et = nested.FirstOrDefault(n => n.Name == enumName);
            Assert.IsNotNull(et, $"Enum {enumName} not found");
            return Enum.Parse(et, valueName);
        }
    }
#endif
}
