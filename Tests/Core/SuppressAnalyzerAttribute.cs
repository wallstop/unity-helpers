namespace WallstopStudios.UnityHelpers.Tests.Core
{
    using System;

    /// <summary>
    /// Marks a class or method as intentionally containing code that would trigger analyzer warnings.
    /// This attribute is used in test files to indicate that the code is deliberately written
    /// to test the analyzer's detection capabilities, and should be ignored during analysis.
    /// </summary>
    /// <remarks>
    /// This attribute is only available in test assemblies and should not be used in production code.
    /// The <see cref="WallstopStudios.UnityHelpers.Editor.Tools.UnityMethodAnalyzer.MethodAnalyzer"/>
    /// will skip any class or method marked with this attribute.
    /// </remarks>
    /// <example>
    /// <code><![CDATA[
    /// // Mark an entire class as containing intentional analyzer issues
    /// [SuppressAnalyzer("Testing return type mismatch detection")]
    /// public class TestClassWithIntentionalIssues : BaseClass
    /// {
    ///     public override string GetValue() => ""; // Intentional mismatch
    /// }
    ///
    /// // Or mark a specific method
    /// public class TestClass : BaseClass
    /// {
    ///     [SuppressAnalyzer("Testing signature mismatch")]
    ///     public override int GetValue(string extra) => 0;
    /// }
    /// ]]></code>
    /// </example>
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Struct,
        AllowMultiple = false,
        Inherited = false
    )]
    public sealed class SuppressAnalyzerAttribute : Attribute
    {
        /// <summary>
        /// Gets the reason why this code is marked for analyzer suppression.
        /// </summary>
        public string Reason { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SuppressAnalyzerAttribute"/> class.
        /// </summary>
        /// <param name="reason">A description of why the analyzer should ignore this code.
        /// This helps document the intentional nature of the code for future maintainers.</param>
        public SuppressAnalyzerAttribute(string reason)
        {
            Reason = reason ?? string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SuppressAnalyzerAttribute"/> class
        /// without a specific reason.
        /// </summary>
        public SuppressAnalyzerAttribute()
        {
            Reason = string.Empty;
        }
    }
}
