namespace WallstopStudios.UnityHelpers.Tests.Helper
{
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Tests.TestUtils;

    public sealed class PathHelperTests : CommonTestBase
    {
        [Test]
        public void SanitizePathWithNullReturnsNull()
        {
            string result = PathHelper.SanitizePath(null);
            Assert.IsNull(result);
        }

        [Test]
        public void SanitizePathWithEmptyStringReturnsEmpty()
        {
            string result = PathHelper.SanitizePath(string.Empty);
            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void SanitizePathWithForwardSlashesReturnsUnchanged()
        {
            string path = "Assets/Scripts/Test.cs";
            string result = PathHelper.SanitizePath(path);
            Assert.AreEqual(path, result);
        }

        [Test]
        public void SanitizePathConvertsBackslashesToForwardSlashes()
        {
            string path = "Assets\\Scripts\\Test.cs";
            string result = PathHelper.SanitizePath(path);
            Assert.AreEqual("Assets/Scripts/Test.cs", result);
        }

        [Test]
        public void SanitizePathHandlesMixedSlashes()
        {
            string path = "Assets\\Scripts/Test\\File.cs";
            string result = PathHelper.SanitizePath(path);
            Assert.AreEqual("Assets/Scripts/Test/File.cs", result);
        }

        [Test]
        public void SanitizePathWithMultipleBackslashesConvertsAll()
        {
            string path = "C:\\\\Users\\\\Test\\\\Documents\\\\File.txt";
            string result = PathHelper.SanitizePath(path);
            Assert.That(result, Does.Not.Contain("\\"));
            Assert.That(result, Does.Contain("/"));
        }

        [Test]
        public void SanitizePathWithOnlyBackslashesConvertsAll()
        {
            string path = "\\\\\\\\";
            string result = PathHelper.SanitizePath(path);
            Assert.AreEqual("////", result);
        }

        [Test]
        public void SanitizePathWithTrailingBackslashConverts()
        {
            string path = "Assets\\Scripts\\";
            string result = PathHelper.SanitizePath(path);
            Assert.AreEqual("Assets/Scripts/", result);
        }

        [Test]
        public void SanitizePathWithLeadingBackslashConverts()
        {
            string path = "\\Assets\\Scripts\\Test.cs";
            string result = PathHelper.SanitizePath(path);
            Assert.AreEqual("/Assets/Scripts/Test.cs", result);
        }

        [Test]
        public void SanitizePathWithSingleBackslashConverts()
        {
            string path = "\\";
            string result = PathHelper.SanitizePath(path);
            Assert.AreEqual("/", result);
        }

        [Test]
        public void SanitizePathWithNoSlashesReturnsUnchanged()
        {
            string path = "Test.cs";
            string result = PathHelper.SanitizePath(path);
            Assert.AreEqual(path, result);
        }

        [Test]
        public void SanitizePathWithWindowsDriveLetterConvertsSlashes()
        {
            string path = "C:\\Windows\\System32";
            string result = PathHelper.SanitizePath(path);
            Assert.AreEqual("C:/Windows/System32", result);
        }

        [Test]
        public void SanitizePathWithUncPathConvertsSlashes()
        {
            string path = "\\\\Server\\Share\\Folder\\File.txt";
            string result = PathHelper.SanitizePath(path);
            Assert.AreEqual("//Server/Share/Folder/File.txt", result);
        }

        [Test]
        public void SanitizePathWithSpecialCharactersPreservesThemButConvertsSlashes()
        {
            string path = "Assets\\Test (Copy)\\File [1].cs";
            string result = PathHelper.SanitizePath(path);
            Assert.AreEqual("Assets/Test (Copy)/File [1].cs", result);
        }

        [Test]
        public void SanitizePathWithWhitespacePreservesItButConvertsSlashes()
        {
            string path = "Assets\\Folder With Spaces\\File.cs";
            string result = PathHelper.SanitizePath(path);
            Assert.AreEqual("Assets/Folder With Spaces/File.cs", result);
        }

        [Test]
        public void SanitizePathWithUnicodeCharactersPreservesThemButConvertsSlashes()
        {
            string path = "Assets\\日本語\\ファイル.txt";
            string result = PathHelper.SanitizePath(path);
            Assert.AreEqual("Assets/日本語/ファイル.txt", result);
        }

        [Test]
        public void SanitizePathWithVeryLongPathConvertsAllBackslashes()
        {
            string longPath = "C:\\";
            for (int i = 0; i < 100; i++)
            {
                longPath += $"Folder{i}\\";
            }
            longPath += "File.txt";

            string result = PathHelper.SanitizePath(longPath);
            Assert.That(result, Does.Not.Contain("\\"));
            Assert.That(result, Does.StartWith("C:/"));
        }

        [Test]
        public void SanitizePathWithRelativePathConvertsBackslashes()
        {
            string path = "..\\..\\Assets\\Test.cs";
            string result = PathHelper.SanitizePath(path);
            Assert.AreEqual("../../Assets/Test.cs", result);
        }

        [Test]
        public void SanitizePathWithCurrentDirectoryReferenceConvertsBackslashes()
        {
            string path = ".\\Assets\\Test.cs";
            string result = PathHelper.SanitizePath(path);
            Assert.AreEqual("./Assets/Test.cs", result);
        }

        [Test]
        public void SanitizePathIdempotent()
        {
            string path = "Assets\\Scripts\\Test.cs";
            string result1 = PathHelper.SanitizePath(path);
            string result2 = PathHelper.SanitizePath(result1);
            Assert.AreEqual(result1, result2);
        }

        [Test]
        public void SanitizePathWithOnlyForwardSlashesReturnsIdentical()
        {
            string path = "Assets/Scripts/Test.cs";
            string result = PathHelper.SanitizePath(path);
            Assert.AreSame(path, result);
        }
    }
}
