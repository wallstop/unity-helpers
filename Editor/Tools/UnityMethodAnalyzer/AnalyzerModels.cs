namespace WallstopStudios.UnityHelpers.Editor.Tools.UnityMethodAnalyzer
{
#if UNITY_EDITOR
    using System.Collections.Generic;

    /// <summary>
    /// Represents the severity level of a detected issue.
    /// </summary>
    public enum IssueSeverity
    {
        Critical = 1,
        High = 2,
        Medium = 3,
        Low = 4,
        Info = 5,
    }

    /// <summary>
    /// Categorizes issues by their context.
    /// </summary>
    public enum IssueCategory
    {
        UnityLifecycle,
        UnityInheritance,
        GeneralInheritance,
    }

    /// <summary>
    /// Represents a detected issue in the codebase.
    /// </summary>
    public sealed class AnalyzerIssue
    {
        public string FilePath { get; set; }
        public string ClassName { get; set; }
        public string MethodName { get; set; }
        public string IssueType { get; set; }
        public string Description { get; set; }
        public IssueSeverity Severity { get; set; }
        public string RecommendedFix { get; set; }
        public int LineNumber { get; set; }
        public IssueCategory Category { get; set; }
        public string BaseClassName { get; set; }
        public string BaseMethodSignature { get; set; }
        public string DerivedMethodSignature { get; set; }

        public AnalyzerIssue(
            string filePath,
            string className,
            string methodName,
            string issueType,
            string description,
            IssueSeverity severity,
            string recommendedFix,
            int lineNumber,
            IssueCategory category = IssueCategory.GeneralInheritance,
            string baseClassName = null,
            string baseMethodSignature = null,
            string derivedMethodSignature = null
        )
        {
            FilePath = filePath;
            ClassName = className;
            MethodName = methodName;
            IssueType = issueType;
            Description = description;
            Severity = severity;
            RecommendedFix = recommendedFix;
            LineNumber = lineNumber;
            Category = category;
            BaseClassName = baseClassName;
            BaseMethodSignature = baseMethodSignature;
            DerivedMethodSignature = derivedMethodSignature;
        }
    }

    /// <summary>
    /// Contains information about a method declaration.
    /// </summary>
    public sealed class AnalyzerMethodInfo
    {
        public string Name { get; set; }
        public string Signature { get; set; }
        public bool IsVirtual { get; set; }
        public bool IsOverride { get; set; }
        public bool IsNew { get; set; }
        public bool IsAbstract { get; set; }
        public bool IsSealed { get; set; }
        public bool IsPrivate { get; set; }
        public bool IsProtected { get; set; }
        public bool IsPublic { get; set; }
        public bool IsInternal { get; set; }
        public bool IsStatic { get; set; }
        public int LineNumber { get; set; }
        public string ReturnType { get; set; }
        public List<string> Parameters { get; set; }
        public List<string> ParameterTypes { get; set; }

        public AnalyzerMethodInfo()
        {
            Parameters = new List<string>();
            ParameterTypes = new List<string>();
        }
    }

    /// <summary>
    /// Contains information about a class declaration.
    /// </summary>
    public sealed class AnalyzerClassInfo
    {
        public string Name { get; set; }
        public string FullName { get; set; }
        public string BaseClassName { get; set; }
        public string FilePath { get; set; }
        public Dictionary<string, List<AnalyzerMethodInfo>> Methods { get; set; }
        public int LineNumber { get; set; }

        public AnalyzerClassInfo()
        {
            Methods = new Dictionary<string, List<AnalyzerMethodInfo>>();
        }
    }
#endif
}
