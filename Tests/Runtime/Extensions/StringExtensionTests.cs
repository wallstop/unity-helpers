namespace UnityHelpers.Tests.Extensions
{
    using Core.Extension;
    using NUnit.Framework;

    public sealed class StringExtensionTests
    {
        [Test]
        public void ToPascalCaseNominal()
        {
            Assert.AreEqual("PascalCase", "PascalCase".ToPascalCase());
            Assert.AreEqual("PascalCase", "pascalCase".ToPascalCase());
            Assert.AreEqual("PascalCase", "_pascalCase".ToPascalCase());
            Assert.AreEqual("PascalCase", "_PascalCase".ToPascalCase());
            Assert.AreEqual("PascalCase", "pascal case".ToPascalCase());
            Assert.AreEqual("PascalCase", "  __Pascal____   ___Case__ ".ToPascalCase());
        }

        [Test]
        public void ToPascalCaseCustomSeparator()
        {
            const string separator = ".";
            Assert.AreEqual("Pascal.Case", "PascalCase".ToPascalCase(separator));
            Assert.AreEqual("Pascal.Case", "pascalCase".ToPascalCase(separator));
            Assert.AreEqual("Pascal.Case", "_pascalCase".ToPascalCase(separator));
            Assert.AreEqual("Pascal.Case", "_PascalCase".ToPascalCase(separator));
            Assert.AreEqual("Pascal.Case", "pascal case".ToPascalCase(separator));
            Assert.AreEqual("Pascal.Case", "  __Pascal____   ___Case__ ".ToPascalCase(separator));
        }
    }
}