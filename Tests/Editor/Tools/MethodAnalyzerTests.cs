#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.Editor.Tools
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Editor.Tools.UnityMethodAnalyzer;

    /// <summary>
    /// Tests for the MethodAnalyzer to ensure it correctly identifies issues
    /// and avoids false positives.
    /// </summary>
    [TestFixture]
    public sealed class MethodAnalyzerTests
    {
        private MethodAnalyzer _analyzer;
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _analyzer = new MethodAnalyzer();
            _tempDir = Path.Combine(
                Path.GetTempPath(),
                "MethodAnalyzerTests_" + Path.GetRandomFileName()
            );
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }

        private void WriteTestFile(string filename, string content)
        {
            File.WriteAllText(Path.Combine(_tempDir, filename), content);
        }

        private void AnalyzeTestFiles()
        {
            _analyzer.Analyze(_tempDir, new[] { _tempDir });
        }

        [Test]
        public void GenericTypeResolutionDoesNotFlagMatchingOverrides()
        {
            WriteTestFile(
                "GenericBase.cs",
                @"
namespace TestNs
{
    public abstract class GenericBase<TValue>
    {
        protected abstract TValue GetValue();
        protected abstract void SetValue(TValue value);
    }

    public class ConcreteInt : GenericBase<int>
    {
        protected override int GetValue() => 0;
        protected override void SetValue(int value) { }
    }

    public class ConcreteString : GenericBase<string>
    {
        protected override string GetValue() => string.Empty;
        protected override void SetValue(string value) { }
    }
}
"
            );

            AnalyzeTestFiles();

            IReadOnlyList<AnalyzerIssue> issues = _analyzer.Issues;
            List<AnalyzerIssue> returnTypeMismatches = issues
                .Where(i => i.IssueType == "ReturnTypeMismatch")
                .ToList();

            Assert.That(
                returnTypeMismatches.Count,
                Is.EqualTo(0),
                $"Should not flag return type mismatch for correctly resolved generics. Found: {string.Join(", ", returnTypeMismatches.Select(i => $"{i.ClassName}.{i.MethodName}"))}"
            );
        }

        [Test]
        public void GenericTypeResolutionWithMultipleTypeParametersWorks()
        {
            WriteTestFile(
                "MultiGenericBase.cs",
                @"
namespace TestNs
{
    public abstract class DictionaryBase<TKey, TValue>
    {
        protected abstract TValue GetValue(TKey key);
        protected abstract TKey GetKey(int index);
    }

    public class StringIntDict : DictionaryBase<string, int>
    {
        protected override int GetValue(string key) => 0;
        protected override string GetKey(int index) => string.Empty;
    }
}
"
            );

            AnalyzeTestFiles();

            IReadOnlyList<AnalyzerIssue> issues = _analyzer.Issues;
            List<AnalyzerIssue> returnTypeMismatches = issues
                .Where(i => i.IssueType == "ReturnTypeMismatch")
                .ToList();

            Assert.That(
                returnTypeMismatches.Count,
                Is.EqualTo(0),
                $"Should resolve multiple generic type parameters. Found: {string.Join(", ", returnTypeMismatches.Select(i => $"{i.ClassName}.{i.MethodName}"))}"
            );
        }

        [Test]
        public void NewExpressionsAreNotFlaggedAsMethods()
        {
            WriteTestFile(
                "NewExpressions.cs",
                @"
namespace TestNs
{
    using System.Collections.Generic;

    public class TestClass
    {
        private List<int> _list = new List<int>();

        public void TestMethod()
        {
            var a = new Vector3(1, 2, 3);
            var b = new Bounds(Vector3.zero, Vector3.one);
            var arr = new[] { new Vector3(0, 0, 0), new Vector3(1, 1, 1) };
        }
    }

    public struct Vector3 { public Vector3(float x, float y, float z) { } public static Vector3 zero; public static Vector3 one; }
    public struct Bounds { public Bounds(Vector3 center, Vector3 size) { } }
}
"
            );

            AnalyzeTestFiles();

            IReadOnlyList<AnalyzerIssue> issues = _analyzer.Issues;

            // Should not have any issues flagging Vector3 or Bounds as methods
            List<AnalyzerIssue> falsePositives = issues
                .Where(i => i.MethodName == "Vector3" || i.MethodName == "Bounds")
                .ToList();

            Assert.That(
                falsePositives.Count,
                Is.EqualTo(0),
                $"Should not flag new expressions as methods. Found: {string.Join(", ", falsePositives.Select(i => $"{i.ClassName}.{i.MethodName}: {i.IssueType}"))}"
            );
        }

        [Test]
        public void CommentsDoNotCreateFalsePositives()
        {
            WriteTestFile(
                "Comments.cs",
                @"
namespace TestNs
{
    /// <summary>
    /// This method DoSomething(int value) is documented here.
    /// </summary>
    public class TestClass
    {
        // Single line comment: void FakeMethod(int x)
        /* Multi-line comment:
           void AnotherFakeMethod(string s)
           protected override int GetValue() */
        public void RealMethod() { }
    }
}
"
            );

            AnalyzeTestFiles();

            IReadOnlyList<AnalyzerIssue> issues = _analyzer.Issues;

            // Should not flag methods found in comments
            List<AnalyzerIssue> commentFalsePositives = issues
                .Where(i =>
                    i.MethodName == "FakeMethod"
                    || i.MethodName == "AnotherFakeMethod"
                    || i.MethodName == "DoSomething"
                )
                .ToList();

            Assert.That(
                commentFalsePositives.Count,
                Is.EqualTo(0),
                $"Should not flag methods in comments. Found: {string.Join(", ", commentFalsePositives.Select(i => i.MethodName))}"
            );
        }

        [Test]
        public void InterfaceImplementationIsNotFlaggedAsMethodHiding()
        {
            WriteTestFile(
                "InterfaceImpl.cs",
                @"
namespace TestNs
{
    public interface ILogger
    {
        void Log(string message);
        void OnInitialized();
    }

    public class ConsoleLogger : ILogger
    {
        public void Log(string message) { }
        public void OnInitialized() { }
    }
}
"
            );

            AnalyzeTestFiles();

            IReadOnlyList<AnalyzerIssue> issues = _analyzer.Issues;

            // Should not flag interface implementations
            List<AnalyzerIssue> hidingIssues = issues
                .Where(i =>
                    i.IssueType == "HidingNonVirtualMethod" && i.ClassName == "ConsoleLogger"
                )
                .ToList();

            Assert.That(
                hidingIssues.Count,
                Is.EqualTo(0),
                $"Should not flag interface implementations as hiding. Found: {string.Join(", ", hidingIssues.Select(i => i.MethodName))}"
            );
        }

        [Test]
        public void NestedInterfaceImplementationIsDetected()
        {
            WriteTestFile(
                "NestedInterface.cs",
                @"
namespace TestNs
{
    public class OuterClass<T>
    {
        public interface INestedInterface
        {
            void DoWork();
        }
    }

    public class Implementor : OuterClass<int>.INestedInterface
    {
        public void DoWork() { }
    }
}
"
            );

            AnalyzeTestFiles();

            IReadOnlyList<AnalyzerIssue> issues = _analyzer.Issues;

            // Should detect that Implementor implements a nested interface, not inherits from OuterClass
            List<AnalyzerIssue> hidingIssues = issues
                .Where(i => i.IssueType == "HidingNonVirtualMethod" && i.ClassName == "Implementor")
                .ToList();

            Assert.That(
                hidingIssues.Count,
                Is.EqualTo(0),
                $"Should recognize nested interface implementation. Found: {string.Join(", ", hidingIssues.Select(i => i.MethodName))}"
            );
        }

        [Test]
        public void ActualReturnTypeMismatchIsFlagged()
        {
            WriteTestFile(
                "ActualMismatch.cs",
                @"
namespace TestNs
{
    public class BaseClass
    {
        public virtual int GetValue() => 0;
    }

    public class DerivedClass : BaseClass
    {
        // This is a real mismatch - can't change return type on override
        public override string GetValue() => string.Empty;
    }
}
"
            );

            AnalyzeTestFiles();

            IReadOnlyList<AnalyzerIssue> issues = _analyzer.Issues;
            List<AnalyzerIssue> returnTypeMismatches = issues
                .Where(i =>
                    i.IssueType == "ReturnTypeMismatch"
                    && i.ClassName == "DerivedClass"
                    && i.MethodName == "GetValue"
                )
                .ToList();

            Assert.That(
                returnTypeMismatches.Count,
                Is.EqualTo(1),
                "Should flag actual return type mismatch (int vs string)"
            );
        }

        [Test]
        public void PrivateMethodShadowingInBaseAndDerivedIsFlagged()
        {
            WriteTestFile(
                "PrivateShadowing.cs",
                @"
namespace TestNs
{
    public class BaseClass
    {
        private void HelperMethod() { }
    }

    public class DerivedClass : BaseClass
    {
        private void HelperMethod() { }
    }
}
"
            );

            AnalyzeTestFiles();

            IReadOnlyList<AnalyzerIssue> issues = _analyzer.Issues;
            List<AnalyzerIssue> shadowingIssues = issues
                .Where(i =>
                    i.IssueType == "PrivateMethodShadowing"
                    && i.ClassName == "DerivedClass"
                    && i.MethodName == "HelperMethod"
                )
                .ToList();

            Assert.That(
                shadowingIssues.Count,
                Is.EqualTo(1),
                "Should flag private method shadowing between base and derived"
            );
        }

        [Test]
        public void StringLiteralsDoNotCreateFalsePositives()
        {
            WriteTestFile(
                "StringLiterals.cs",
                @"
namespace TestNs
{
    public class TestClass
    {
        private string _message = ""void FakeMethod(int x) is not a method"";
        private string _verbatim = @""protected override int GetValue() is in a string"";

        public void RealMethod()
        {
            var s = ""new Vector3(1, 2, 3)"";
        }
    }
}
"
            );

            AnalyzeTestFiles();

            IReadOnlyList<AnalyzerIssue> issues = _analyzer.Issues;

            // Should not flag content from string literals
            List<AnalyzerIssue> stringFalsePositives = issues
                .Where(i => i.MethodName == "FakeMethod" || i.MethodName == "GetValue")
                .ToList();

            Assert.That(
                stringFalsePositives.Count,
                Is.EqualTo(0),
                $"Should not flag methods in string literals. Found: {string.Join(", ", stringFalsePositives.Select(i => i.MethodName))}"
            );
        }

        [Test]
        public void ControlFlowKeywordsAreNotFlaggedAsMethods()
        {
            WriteTestFile(
                "ControlFlow.cs",
                @"
namespace TestNs
{
    public class TestClass
    {
        public void TestMethod()
        {
            if (true) { }
            while (false) { }
            for (int i = 0; i < 10; i++) { }
            foreach (var x in new int[0]) { }
            switch (0) { default: break; }
            using (var d = new System.IO.MemoryStream()) { }
            lock (this) { }
        }
    }
}
"
            );

            AnalyzeTestFiles();

            IReadOnlyList<AnalyzerIssue> issues = _analyzer.Issues;

            // Should not flag control flow keywords as methods
            string[] keywords = { "if", "while", "for", "foreach", "switch", "using", "lock" };
            List<AnalyzerIssue> keywordFalsePositives = issues
                .Where(i => keywords.Contains(i.MethodName))
                .ToList();

            Assert.That(
                keywordFalsePositives.Count,
                Is.EqualTo(0),
                $"Should not flag control flow keywords. Found: {string.Join(", ", keywordFalsePositives.Select(i => i.MethodName))}"
            );
        }

        [Test]
        public void GenericTypeParameterPassThroughIsResolved()
        {
            // Test case where derived class passes its own type parameter to base
            WriteTestFile(
                "GenericPassThrough.cs",
                @"
namespace TestNs
{
    public abstract class BaseCache<TValueCache>
    {
        protected abstract TValueCache GetValue(int index);
    }

    // TValue is passed directly as TValueCache
    public class DirectCache<TValue> : BaseCache<TValue>
    {
        protected override TValue GetValue(int index) => default;
    }
}
"
            );

            AnalyzeTestFiles();

            IReadOnlyList<AnalyzerIssue> issues = _analyzer.Issues;
            List<AnalyzerIssue> returnTypeMismatches = issues
                .Where(i => i.IssueType == "ReturnTypeMismatch" && i.ClassName == "DirectCache")
                .ToList();

            Assert.That(
                returnTypeMismatches.Count,
                Is.EqualTo(0),
                $"Should recognize type parameter pass-through. Found: {string.Join(", ", returnTypeMismatches.Select(i => $"{i.MethodName}: base={i.BaseMethodSignature}, derived={i.DerivedMethodSignature}"))}"
            );
        }

        [Test]
        public void SuppressAnalyzerAttributeOnClassSkipsAllIssues()
        {
            WriteTestFile(
                "SuppressedClass.cs",
                @"
namespace TestNs
{
    public class BaseClass
    {
        public virtual int GetValue() => 0;
    }

    [SuppressAnalyzer(""Testing analyzer suppression"")]
    public class SuppressedClass : BaseClass
    {
        // This would normally be flagged as return type mismatch
        public override string GetValue() => string.Empty;
    }
}
"
            );

            AnalyzeTestFiles();

            IReadOnlyList<AnalyzerIssue> issues = _analyzer.Issues;
            List<AnalyzerIssue> suppressedClassIssues = issues
                .Where(i => i.ClassName == "SuppressedClass")
                .ToList();

            Assert.That(
                suppressedClassIssues.Count,
                Is.EqualTo(0),
                $"Should skip issues for classes with [SuppressAnalyzer]. Found: {string.Join(", ", suppressedClassIssues.Select(i => $"{i.MethodName}: {i.IssueType}"))}"
            );
        }

        [Test]
        public void SuppressAnalyzerAttributeOnMethodSkipsSpecificMethod()
        {
            WriteTestFile(
                "SuppressedMethod.cs",
                @"
namespace TestNs
{
    public class BaseClass
    {
        public virtual int GetValue() => 0;
        public virtual int GetOther() => 0;
    }

    public class DerivedClass : BaseClass
    {
        [SuppressAnalyzer]
        public override string GetValue() => string.Empty;

        // This should still be flagged
        public override string GetOther() => string.Empty;
    }
}
"
            );

            AnalyzeTestFiles();

            IReadOnlyList<AnalyzerIssue> issues = _analyzer.Issues;

            // GetValue should be suppressed
            List<AnalyzerIssue> getValueIssues = issues
                .Where(i => i.ClassName == "DerivedClass" && i.MethodName == "GetValue")
                .ToList();

            Assert.That(
                getValueIssues.Count,
                Is.EqualTo(0),
                $"Should skip issues for methods with [SuppressAnalyzer]. Found: {string.Join(", ", getValueIssues.Select(i => i.IssueType))}"
            );

            // GetOther should still be flagged
            List<AnalyzerIssue> getOtherIssues = issues
                .Where(i => i.ClassName == "DerivedClass" && i.MethodName == "GetOther")
                .ToList();

            Assert.That(
                getOtherIssues.Count,
                Is.EqualTo(1),
                "Should still flag methods without [SuppressAnalyzer]"
            );
        }

        [Test]
        public void SuppressAnalyzerAttributeWithAttributeSuffixWorks()
        {
            WriteTestFile(
                "SuppressedWithSuffix.cs",
                @"
namespace TestNs
{
    public class BaseClass
    {
        public virtual int GetValue() => 0;
    }

    [SuppressAnalyzerAttribute(""With Attribute suffix"")]
    public class SuppressedClass : BaseClass
    {
        public override string GetValue() => string.Empty;
    }
}
"
            );

            AnalyzeTestFiles();

            IReadOnlyList<AnalyzerIssue> issues = _analyzer.Issues;
            List<AnalyzerIssue> suppressedClassIssues = issues
                .Where(i => i.ClassName == "SuppressedClass")
                .ToList();

            Assert.That(
                suppressedClassIssues.Count,
                Is.EqualTo(0),
                $"Should recognize [SuppressAnalyzerAttribute] variant. Found: {string.Join(", ", suppressedClassIssues.Select(i => $"{i.MethodName}: {i.IssueType}"))}"
            );
        }

        [Test]
        public void NewExpressionsInCollectionInitializersAreNotFlaggedAsMethods()
        {
            WriteTestFile(
                "CollectionInitializers.cs",
                @"
namespace TestNs
{
    using System.Collections.Generic;

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
    }

    public class TestClass
    {
        public void TestMethod()
        {
            // Multiple new expressions after commas - this was causing false positives
            List<Vector3> points = new() { new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 0f) };
            var arr = new Vector3[] { new Vector3(1, 2, 3), new Vector3(4, 5, 6) };
        }
    }
}
"
            );

            AnalyzeTestFiles();

            IReadOnlyList<AnalyzerIssue> issues = _analyzer.Issues;
            List<AnalyzerIssue> falsePositives = issues
                .Where(i => i.MethodName == "Vector3")
                .ToList();

            Assert.That(
                falsePositives.Count,
                Is.EqualTo(0),
                $"Should not flag new expressions in collection initializers. Found: {string.Join(", ", falsePositives.Select(i => $"{i.ClassName}.{i.MethodName}: {i.IssueType}"))}"
            );
        }

        [Test]
        public void NewExpressionsWithNamedParametersAreNotFlaggedAsMethods()
        {
            WriteTestFile(
                "NamedParameters.cs",
                @"
namespace TestNs
{
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
    }

    public struct Bounds
    {
        public Vector3 center, size;
        public Bounds(Vector3 center, Vector3 size) { this.center = center; this.size = size; }
    }

    public class TestClass
    {
        public void TestMethod()
        {
            // Named parameters with new expressions
            Bounds bounds = new(center: new Vector3(0.5f, 0f, 0f), size: new Vector3(1f, 0.1f, 0.1f));
        }
    }
}
"
            );

            AnalyzeTestFiles();

            IReadOnlyList<AnalyzerIssue> issues = _analyzer.Issues;
            List<AnalyzerIssue> falsePositives = issues
                .Where(i => i.MethodName == "Vector3" || i.MethodName == "Bounds")
                .ToList();

            Assert.That(
                falsePositives.Count,
                Is.EqualTo(0),
                $"Should not flag new expressions with named parameters. Found: {string.Join(", ", falsePositives.Select(i => $"{i.ClassName}.{i.MethodName}: {i.IssueType}"))}"
            );
        }

        [Test]
        public void NewExpressionsInMethodArgumentsAreNotFlaggedAsMethods()
        {
            WriteTestFile(
                "MethodArguments.cs",
                @"
namespace TestNs
{
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
    }

    public class TestClass
    {
        public void ProcessVectors(Vector3 a, Vector3 b) { }

        public void TestMethod()
        {
            // New expressions as method arguments
            ProcessVectors(new Vector3(1, 2, 3), new Vector3(4, 5, 6));
        }
    }
}
"
            );

            AnalyzeTestFiles();

            IReadOnlyList<AnalyzerIssue> issues = _analyzer.Issues;
            List<AnalyzerIssue> falsePositives = issues
                .Where(i => i.MethodName == "Vector3")
                .ToList();

            Assert.That(
                falsePositives.Count,
                Is.EqualTo(0),
                $"Should not flag new expressions in method arguments. Found: {string.Join(", ", falsePositives.Select(i => $"{i.ClassName}.{i.MethodName}: {i.IssueType}"))}"
            );
        }

        [Test]
        public void GenericBaseClassWithThreeTypeParametersResolvesCorrectly()
        {
            // This test case mirrors SerializableSortedDictionary's inheritance pattern
            WriteTestFile(
                "ThreeTypeParams.cs",
                @"
namespace TestNs
{
    public abstract class DictionaryBase<TKey, TValue, TValueCache>
    {
        protected abstract TValue GetValue(TValueCache[] cache, int index);
        protected abstract void SetValue(TValueCache[] cache, int index, TValue value);
    }

    // TValue is passed as TValueCache (third type parameter)
    public class SimpleDictionary<TKey, TValue> : DictionaryBase<TKey, TValue, TValue>
    {
        protected override TValue GetValue(TValue[] cache, int index) => cache[index];
        protected override void SetValue(TValue[] cache, int index, TValue value) { cache[index] = value; }
    }
}
"
            );

            AnalyzeTestFiles();

            IReadOnlyList<AnalyzerIssue> issues = _analyzer.Issues;
            List<AnalyzerIssue> signatureMismatches = issues
                .Where(i => i.IssueType == "SignatureMismatch" && i.ClassName == "SimpleDictionary")
                .ToList();

            Assert.That(
                signatureMismatches.Count,
                Is.EqualTo(0),
                $"Should resolve type parameter substitution (TValueCache -> TValue). Found: {string.Join(", ", signatureMismatches.Select(i => $"{i.MethodName}: base={i.BaseMethodSignature}, derived={i.DerivedMethodSignature}"))}"
            );
        }

        [Test]
        public void GenericBaseClassWithWhereConstraintResolvesCorrectly()
        {
            WriteTestFile(
                "WhereConstraint.cs",
                @"
namespace TestNs
{
    using System;

    public abstract class ComparableBase<TKey, TValue, TValueCache>
        where TKey : IComparable<TKey>
    {
        protected abstract TValue GetValue(TValueCache[] cache, int index);
    }

    public class ComparableDict<TKey, TValue> : ComparableBase<TKey, TValue, TValue>
        where TKey : IComparable<TKey>
    {
        protected override TValue GetValue(TValue[] cache, int index) => cache[index];
    }
}
"
            );

            AnalyzeTestFiles();

            IReadOnlyList<AnalyzerIssue> issues = _analyzer.Issues;
            List<AnalyzerIssue> signatureMismatches = issues
                .Where(i => i.IssueType == "SignatureMismatch" && i.ClassName == "ComparableDict")
                .ToList();

            Assert.That(
                signatureMismatches.Count,
                Is.EqualTo(0),
                $"Should handle where constraints when parsing base class. Found: {string.Join(", ", signatureMismatches.Select(i => $"{i.MethodName}: base={i.BaseMethodSignature}, derived={i.DerivedMethodSignature}"))}"
            );
        }

        [Test]
        public void GenericBaseClassWithNestedGenericTypesResolvesCorrectly()
        {
            WriteTestFile(
                "NestedGenerics.cs",
                @"
namespace TestNs
{
    using System.Collections.Generic;

    public abstract class CollectionBase<TItem, TCollection>
    {
        protected abstract TCollection GetCollection();
    }

    public class ListWrapper<T> : CollectionBase<T, List<T>>
    {
        protected override List<T> GetCollection() => new List<T>();
    }

    public class DictWrapper<TKey, TValue> : CollectionBase<TValue, Dictionary<TKey, TValue>>
    {
        protected override Dictionary<TKey, TValue> GetCollection() => new Dictionary<TKey, TValue>();
    }
}
"
            );

            AnalyzeTestFiles();

            IReadOnlyList<AnalyzerIssue> issues = _analyzer.Issues;
            List<AnalyzerIssue> returnTypeMismatches = issues
                .Where(i =>
                    i.IssueType == "ReturnTypeMismatch"
                    && (i.ClassName == "ListWrapper" || i.ClassName == "DictWrapper")
                )
                .ToList();

            Assert.That(
                returnTypeMismatches.Count,
                Is.EqualTo(0),
                $"Should resolve nested generic types in return type. Found: {string.Join(", ", returnTypeMismatches.Select(i => $"{i.ClassName}.{i.MethodName}: base={i.BaseMethodSignature}, derived={i.DerivedMethodSignature}"))}"
            );
        }

        [Test]
        public void MultipleInterfacesWithGenericBaseClassAreHandledCorrectly()
        {
            WriteTestFile(
                "MultipleInterfaces.cs",
                @"
namespace TestNs
{
    using System;

    public interface IDisposable { void Dispose(); }
    public interface IComparable<T> { int CompareTo(T other); }

    public abstract class BaseWithInterfaces<TKey, TValue, TCache>
        : IDisposable, IComparable<BaseWithInterfaces<TKey, TValue, TCache>>
    {
        protected abstract TValue GetValue(TCache[] cache);
        public abstract void Dispose();
        public abstract int CompareTo(BaseWithInterfaces<TKey, TValue, TCache> other);
    }

    public class ConcreteWithInterfaces<TKey, TValue>
        : BaseWithInterfaces<TKey, TValue, TValue>
    {
        protected override TValue GetValue(TValue[] cache) => cache[0];
        public override void Dispose() { }
        public override int CompareTo(BaseWithInterfaces<TKey, TValue, TValue> other) => 0;
    }
}
"
            );

            AnalyzeTestFiles();

            IReadOnlyList<AnalyzerIssue> issues = _analyzer.Issues;
            List<AnalyzerIssue> signatureMismatches = issues
                .Where(i =>
                    i.IssueType == "SignatureMismatch" && i.ClassName == "ConcreteWithInterfaces"
                )
                .ToList();

            Assert.That(
                signatureMismatches.Count,
                Is.EqualTo(0),
                $"Should parse base class correctly when interfaces are also implemented. Found: {string.Join(", ", signatureMismatches.Select(i => $"{i.MethodName}: base={i.BaseMethodSignature}, derived={i.DerivedMethodSignature}"))}"
            );
        }

        [Test]
        public void PrivateMethodShadowingIsNotReportedForConstructorCalls()
        {
            WriteTestFile(
                "NoFalsePrivateShadowing.cs",
                @"
namespace TestNs
{
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
    }

    public class BaseClass
    {
        private void Helper() { }
    }

    public class DerivedClass : BaseClass
    {
        public void TestMethod()
        {
            // These constructor calls should NOT be flagged as private method shadowing
            var a = new Vector3(1, 2, 3);
            var b = new Vector3(4, 5, 6);
        }
    }
}
"
            );

            AnalyzeTestFiles();

            IReadOnlyList<AnalyzerIssue> issues = _analyzer.Issues;
            List<AnalyzerIssue> shadowingIssues = issues
                .Where(i => i.IssueType == "PrivateMethodShadowing" && i.MethodName == "Vector3")
                .ToList();

            Assert.That(
                shadowingIssues.Count,
                Is.EqualTo(0),
                $"Should not report private method shadowing for constructor calls. Found: {string.Join(", ", shadowingIssues.Select(i => $"{i.ClassName}.{i.MethodName}"))}"
            );
        }

        [Test]
        public void ArrayInitializerNewExpressionsAfterCommaAreNotFlaggedAsMethods()
        {
            WriteTestFile(
                "ArrayInitializer.cs",
                @"
namespace TestNs
{
    public struct Bounds
    {
        public Bounds(float x, float y) { }
    }

    public class TestClass
    {
        // This specific pattern was causing false positives: new(new Bounds(...), new Bounds(...))
        private Bounds[] _bounds = new Bounds[] { new Bounds(1, 2), new Bounds(3, 4) };

        public void TestMethod()
        {
            var arr = new[] { new Bounds(5, 6), new Bounds(7, 8) };
        }
    }
}
"
            );

            AnalyzeTestFiles();

            IReadOnlyList<AnalyzerIssue> issues = _analyzer.Issues;
            List<AnalyzerIssue> boundsIssues = issues.Where(i => i.MethodName == "Bounds").ToList();

            Assert.That(
                boundsIssues.Count,
                Is.EqualTo(0),
                $"Should not flag new expressions in array initializers. Found: {string.Join(", ", boundsIssues.Select(i => $"{i.ClassName}.{i.MethodName}: {i.IssueType}"))}"
            );
        }
    }
}
#endif
