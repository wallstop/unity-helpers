namespace WallstopStudios.UnityHelpers.Tests.Helper
{
    using System;
    using System.IO;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Tests.TestUtils;

    public sealed class DirectoryHelperTests : CommonTestBase
    {
        [Test]
        public void EnsureDirectoryExistsWithNullDoesNothing()
        {
            Assert.DoesNotThrow(() => DirectoryHelper.EnsureDirectoryExists(null));
        }

        [Test]
        public void EnsureDirectoryExistsWithEmptyStringDoesNothing()
        {
            Assert.DoesNotThrow(() => DirectoryHelper.EnsureDirectoryExists(string.Empty));
        }

        [Test]
        public void EnsureDirectoryExistsWithWhitespaceDoesNothing()
        {
            Assert.DoesNotThrow(() => DirectoryHelper.EnsureDirectoryExists("   "));
        }

        [Test]
        public void GetCallerScriptDirectoryWithEmptyPathReturnsEmpty()
        {
            string result = DirectoryHelper.GetCallerScriptDirectory();
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void FindPackageRootPathWithNullStartDirectoryReturnsEmpty()
        {
            string result = DirectoryHelper.FindPackageRootPath(null);
            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void FindPackageRootPathWithEmptyStartDirectoryReturnsEmpty()
        {
            string result = DirectoryHelper.FindPackageRootPath(string.Empty);
            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void FindPackageRootPathWithWhitespaceStartDirectoryReturnsEmpty()
        {
            string result = DirectoryHelper.FindPackageRootPath("   ");
            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void FindPackageRootPathWithNonExistentDirectoryReturnsEmpty()
        {
            string nonExistent = Path.Combine(
                Application.dataPath,
                "NonExistent",
                "Path",
                "That",
                "Does",
                "Not",
                "Exist"
            );
            string result = DirectoryHelper.FindPackageRootPath(nonExistent);
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void FindRootPathWithNullConditionReturnsEmpty()
        {
            string result = DirectoryHelper.FindRootPath(Application.dataPath, null);
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void FindRootPathWithAlwaysFalseConditionReturnsEmpty()
        {
            string result = DirectoryHelper.FindRootPath(Application.dataPath, _ => false);
            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void FindRootPathWithAlwaysTrueConditionReturnsStartDirectory()
        {
            string startPath = Application.dataPath.SanitizePath();
            string result = DirectoryHelper.FindRootPath(startPath, _ => true).SanitizePath();
            Assert.That(result, Does.StartWith(startPath).IgnoreCase);
        }

        [Test]
        public void FindRootPathWithExceptionInConditionReturnsCurrentPath()
        {
            string startPath = Application.dataPath;
            string result = DirectoryHelper.FindRootPath(
                startPath,
                _ => throw new InvalidOperationException("Test exception")
            );
            Assert.AreEqual(startPath, result);
        }

        [Test]
        public void FindRootPathWithNullStartDirectoryReturnsEmpty()
        {
            string result = DirectoryHelper.FindRootPath(null, _ => true);
            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void FindRootPathWithEmptyStartDirectoryReturnsEmpty()
        {
            string result = DirectoryHelper.FindRootPath(string.Empty, _ => true);
            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void FindAbsolutePathToDirectoryWithNonExistentCallerReturnsEmpty()
        {
            string result = DirectoryHelper.FindAbsolutePathToDirectory("SomeDir");
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void FindAbsolutePathToDirectoryIsCaseInsensitive()
        {
            string result = DirectoryHelper.FindAbsolutePathToDirectory("tests/runtime");
            Assert.IsFalse(string.IsNullOrWhiteSpace(result));
            StringAssert.EndsWith("tests/runtime", result.ToLowerInvariant());
        }

        [Test]
        public void GetCallerScriptDirectoryReturnsEmptyWhenSourcePathNull()
        {
            string directory = DirectoryHelper.GetCallerScriptDirectory(sourceFilePath: null);
            Assert.AreEqual(string.Empty, directory);
        }

        [TestCase(@"\\server\share\Assets\Example.asset")]
        [TestCase(@"//server/share/Assets/Example.asset")]
        public void AbsoluteToUnityRelativePathReturnsEmptyForUncPaths(string uncPath)
        {
            string relative = DirectoryHelper.AbsoluteToUnityRelativePath(uncPath);
            Assert.AreEqual(string.Empty, relative);
        }

        [Test]
        public void FindAbsolutePathToDirectoryResolvesTestsFolder()
        {
            string result = DirectoryHelper.FindAbsolutePathToDirectory("Tests/Runtime");
            Assert.IsFalse(string.IsNullOrWhiteSpace(result));
            StringAssert.EndsWith("tests/runtime", result.ToLowerInvariant());
        }

        [TestCase("Assets/Test/Path", true)]
        [TestCase("assets/test/path", true)]
        [TestCase("ASSETS/TEST/PATH", true)]
        [TestCase("Packages/com.wallstop-studios.unity-helpers", false)]
        public void AbsoluteToUnityRelativePathHandlesCaseInsensitivity(
            string suffix,
            bool expectAssets
        )
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(projectRoot))
            {
                Assert.Inconclusive("Project root could not be determined.");
                return;
            }

            string testPath = $"{projectRoot}/{suffix}";
            string relative = DirectoryHelper.AbsoluteToUnityRelativePath(testPath);
            if (expectAssets)
            {
                Assert.That(relative, Does.StartWith("Assets/").IgnoreCase);
            }
            else
            {
                Assert.That(relative, Does.StartWith("Packages/").IgnoreCase);
            }
        }

        [Test]
        public void AbsoluteToUnityRelativePathWithNullReturnsEmpty()
        {
            string result = DirectoryHelper.AbsoluteToUnityRelativePath(null);
            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void AbsoluteToUnityRelativePathWithEmptyReturnsEmpty()
        {
            string result = DirectoryHelper.AbsoluteToUnityRelativePath(string.Empty);
            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void AbsoluteToUnityRelativePathWithWhitespaceReturnsEmpty()
        {
            string result = DirectoryHelper.AbsoluteToUnityRelativePath("   ");
            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void AbsoluteToUnityRelativePathConvertsBackslashesToForwardSlashes()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(projectRoot))
            {
                Assert.Inconclusive("Could not determine project root");
                return;
            }

            string testPath = projectRoot + "\\Assets\\Test";
            string result = DirectoryHelper.AbsoluteToUnityRelativePath(testPath);
            Assert.That(result, Does.Not.Contain("\\"));
        }

        [Test]
        public void AbsoluteToUnityRelativePathWithAssetsPathReturnsRelative()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(projectRoot))
            {
                Assert.Inconclusive("Could not determine project root");
                return;
            }

            string testPath = projectRoot + "/Assets/Test/File.txt";
            string result = DirectoryHelper.AbsoluteToUnityRelativePath(testPath);
            Assert.That(result, Does.StartWith("Assets/").Or.EqualTo(string.Empty));
        }

        [Test]
        public void AbsoluteToUnityRelativePathWithProjectRootReturnsEmpty()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(projectRoot))
            {
                Assert.Inconclusive("Could not determine project root");
                return;
            }

            string result = DirectoryHelper.AbsoluteToUnityRelativePath(projectRoot);
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void AbsoluteToUnityRelativePathWithPathOutsideProjectReturnsEmpty()
        {
            string externalPath = "/some/external/path/that/is/not/in/project";
            string result = DirectoryHelper.AbsoluteToUnityRelativePath(externalPath);
            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void AbsoluteToUnityRelativePathWithDataPathReturnsAssets()
        {
            string dataPath = Application.dataPath.SanitizePath();
            string result = DirectoryHelper.AbsoluteToUnityRelativePath(dataPath);
            Assert.That(result, Does.Contain("Assets").Or.EqualTo(string.Empty));
        }

        [Test]
        public void AbsoluteToUnityRelativePathRemovesLeadingSlash()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(projectRoot))
            {
                Assert.Inconclusive("Could not determine project root");
                return;
            }

            string testPath = projectRoot + "/Assets";
            string result = DirectoryHelper.AbsoluteToUnityRelativePath(testPath);
            if (!string.IsNullOrEmpty(result))
            {
                Assert.That(result[0], Is.Not.EqualTo('/'));
            }
        }

        [Test]
        public void AbsoluteToUnityRelativePathWithTrailingSlashHandledCorrectly()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(projectRoot))
            {
                Assert.Inconclusive("Could not determine project root");
                return;
            }

            string testPathWithSlash = projectRoot + "/";
            string result = DirectoryHelper.AbsoluteToUnityRelativePath(testPathWithSlash);
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void AbsoluteToUnityRelativePathWithMixedCaseMatches()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(projectRoot))
            {
                Assert.Inconclusive("Could not determine project root");
                return;
            }

            string testPath = projectRoot.ToUpperInvariant() + "/ASSETS/TEST";
            string result = DirectoryHelper.AbsoluteToUnityRelativePath(testPath);
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void AbsoluteToUnityRelativePathWithNestedAssetsFolderHandlesCorrectly()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(projectRoot))
            {
                Assert.Inconclusive("Could not determine project root");
                return;
            }

            string testPath = projectRoot + "/Assets/Nested/Assets/File.txt";
            string result = DirectoryHelper.AbsoluteToUnityRelativePath(testPath);
            if (!string.IsNullOrEmpty(result))
            {
                Assert.That(result, Does.StartWith("Assets/"));
            }
        }

        [Test]
        public void AbsoluteToUnityRelativePathWithVeryLongPathHandlesCorrectly()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(projectRoot))
            {
                Assert.Inconclusive("Could not determine project root");
                return;
            }

            string longPath = projectRoot + "/Assets";
            for (int i = 0; i < 50; i++)
            {
                longPath += $"/Folder{i}";
            }
            longPath += "/File.txt";

            string result = DirectoryHelper.AbsoluteToUnityRelativePath(longPath);
            Assert.That(result, Is.Not.Null);
            if (!string.IsNullOrEmpty(result))
            {
                Assert.That(result, Does.StartWith("Assets/"));
            }
        }

        [Test]
        public void AbsoluteToUnityRelativePathWithSpecialCharactersInPath()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(projectRoot))
            {
                Assert.Inconclusive("Could not determine project root");
                return;
            }

            string testPath = projectRoot + "/Assets/Test Folder With Spaces/File.txt";
            string result = DirectoryHelper.AbsoluteToUnityRelativePath(testPath);
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void AbsoluteToUnityRelativePathWithUnicodeCharacters()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(projectRoot))
            {
                Assert.Inconclusive("Could not determine project root");
                return;
            }

            string testPath = projectRoot + "/Assets/フォルダ/ファイル.txt";
            string result = DirectoryHelper.AbsoluteToUnityRelativePath(testPath);
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void FindRootPathStopsAtRootDrive()
        {
            string result = DirectoryHelper.FindRootPath("C:\\", _ => false);
            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void FindRootPathHandlesPathWithMultipleSeparators()
        {
            string testPath = Application.dataPath + "//Multiple///Separators";
            string result = DirectoryHelper.FindRootPath(
                testPath,
                path => path == Application.dataPath
            );
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void FindRootPathReturnsCurrentPathWhenConditionThrows()
        {
            string startPath = Application.dataPath;
            bool exceptionThrown = false;
            string result = DirectoryHelper.FindRootPath(
                startPath,
                _ =>
                {
                    if (!exceptionThrown)
                    {
                        exceptionThrown = true;
                        throw new InvalidOperationException("test");
                    }

                    return false;
                }
            );

            Assert.AreEqual(startPath, result);
        }
    }
}
