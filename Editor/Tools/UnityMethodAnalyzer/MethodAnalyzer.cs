// ReSharper disable ArrangeRedundantParentheses
namespace WallstopStudios.UnityHelpers.Editor.Tools.UnityMethodAnalyzer
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Analyzes C# files for Unity MonoBehaviour method issues.
    /// Uses regex-based parsing since Roslyn is not available in Unity Editor.
    /// </summary>
    public sealed class MethodAnalyzer
    {
        private readonly ConcurrentDictionary<string, AnalyzerClassInfo> _classes = new();
        private readonly ConcurrentBag<AnalyzerIssue> _issues = new();

        public IReadOnlyList<AnalyzerIssue> Issues => _issues.ToList();
        public IReadOnlyDictionary<string, AnalyzerClassInfo> Classes => _classes;

        private static readonly Regex ClassRegex = new(
            @"(?:(?<abstract>abstract)\s+)?(?:(?<sealed>sealed)\s+)?(?:(?<partial>partial)\s+)?class\s+(?<name>\w+)(?:\s*<[^>]+>)?(?:\s*:\s*(?<base>[^{]+))?",
            RegexOptions.Compiled
        );

        private static readonly Regex MethodRegex = new(
            @"(?<modifiers>(?:(?:public|private|protected|internal|static|virtual|override|new|sealed|abstract|async|ref|readonly|unsafe|extern|partial)\s+)*)(?<return>[\w<>\[\],\.\?\*]+(?:[ \t]+[\w<>\[\],\.\?\*]+)*)\s+(?<name>\w+)\s*\((?<params>[^)]*)\)",
            RegexOptions.Compiled
        );

        private static readonly Regex NamespaceRegex = new(
            @"namespace\s+([\w\.]+)",
            RegexOptions.Compiled
        );

        /// <summary>
        /// Clears all cached data from a previous analysis.
        /// </summary>
        public void Clear()
        {
            _classes.Clear();
            while (_issues.TryTake(out _)) { }
        }

        /// <summary>
        /// Analyzes all C# files in the specified directories.
        /// </summary>
        public async Task AnalyzeAsync(
            string rootPath,
            IEnumerable<string> directories,
            IProgress<float> progress = null,
            CancellationToken cancellationToken = default
        )
        {
            Clear();

            List<string> csFiles = new();
            foreach (string dir in directories)
            {
                string fullPath = Path.IsPathRooted(dir) ? dir : Path.Combine(rootPath, dir);
                if (Directory.Exists(fullPath))
                {
                    csFiles.AddRange(
                        Directory.EnumerateFiles(fullPath, "*.cs", SearchOption.AllDirectories)
                    );
                }
            }

            if (csFiles.Count == 0)
            {
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            await ProcessFilesAsync(csFiles, rootPath, progress, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            await Task.Run(() => AnalyzeInheritance(), cancellationToken);
            progress?.Report(1f);
        }

        /// <summary>
        /// Synchronous analysis for simpler use cases.
        /// </summary>
        public void Analyze(string rootPath, IEnumerable<string> directories)
        {
            Clear();

            List<string> csFiles = new();
            foreach (string dir in directories)
            {
                string fullPath = Path.IsPathRooted(dir) ? dir : Path.Combine(rootPath, dir);
                if (Directory.Exists(fullPath))
                {
                    csFiles.AddRange(
                        Directory.EnumerateFiles(fullPath, "*.cs", SearchOption.AllDirectories)
                    );
                }
            }

            foreach (string file in csFiles)
            {
                ParseFile(file, rootPath);
            }

            AnalyzeInheritance();
        }

        private async Task ProcessFilesAsync(
            IReadOnlyList<string> csFiles,
            string rootPath,
            IProgress<float> progress,
            CancellationToken cancellationToken
        )
        {
            int total = csFiles.Count;
            int completed = 0;
            int maxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount);

            using SemaphoreSlim semaphore = new(maxDegreeOfParallelism);
            List<Task> tasks = new();

            try
            {
                foreach (string file in csFiles)
                {
                    await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                    Task task = Task.Run(
                        () =>
                        {
                            try
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                ParseFile(file, rootPath);
                                cancellationToken.ThrowIfCancellationRequested();

                                int current = Interlocked.Increment(ref completed);
                                progress?.Report((float)current / total * 0.5f);
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        },
                        cancellationToken
                    );

                    tasks.Add(task);
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                try
                {
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
                catch
                {
                    // Ensure all tasks have observed cancellation before rethrowing.
                }

                throw;
            }
        }

        private Task ParseFileAsync(string filePath, string rootPath)
        {
            ParseFile(filePath, rootPath);
            return Task.CompletedTask;
        }

        private void ParseFile(string filePath, string rootPath)
        {
            try
            {
                string code = File.ReadAllText(filePath);
                string relativePath = GetRelativePath(rootPath, filePath);

                string currentNamespace = ExtractNamespace(code);
                List<(int start, int end, string content)> classBlocks = ExtractClassBlocks(code);

                foreach ((int start, int end, string content) classBlock in classBlocks)
                {
                    AnalyzerClassInfo classInfo = ExtractClassInfo(
                        classBlock.content,
                        relativePath,
                        currentNamespace,
                        code,
                        classBlock.start
                    );
                    if (classInfo != null)
                    {
                        string key = classInfo.FullName;
                        _classes.TryAdd(key, classInfo);
                    }
                }
            }
            catch (Exception)
            {
                // Skip files that can't be parsed
            }
        }

        private static string GetRelativePath(string rootPath, string fullPath)
        {
            if (string.IsNullOrEmpty(rootPath))
            {
                return fullPath;
            }

            string normalizedRoot = Path.GetFullPath(rootPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string normalizedFull = Path.GetFullPath(fullPath);

            if (
                normalizedFull.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase)
                && normalizedFull.Length > normalizedRoot.Length
            )
            {
                return normalizedFull
                    .Substring(normalizedRoot.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }

            return fullPath;
        }

        private static string ExtractNamespace(string code)
        {
            Match match = NamespaceRegex.Match(code);
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        private static List<(int start, int end, string content)> ExtractClassBlocks(string code)
        {
            List<(int start, int end, string content)> blocks = new();
            MatchCollection classMatches = ClassRegex.Matches(code);

            foreach (Match classMatch in classMatches)
            {
                int classStart = classMatch.Index;
                int braceStart = code.IndexOf('{', classMatch.Index + classMatch.Length);
                if (braceStart < 0)
                {
                    continue;
                }

                int braceCount = 1;
                int braceEnd = braceStart + 1;
                while (braceEnd < code.Length && braceCount > 0)
                {
                    char c = code[braceEnd];
                    if (c == '{')
                    {
                        braceCount++;
                    }
                    else if (c == '}')
                    {
                        braceCount--;
                    }

                    braceEnd++;
                }

                if (braceCount == 0)
                {
                    string content = code.Substring(classStart, braceEnd - classStart);
                    blocks.Add((classStart, braceEnd, content));
                }
            }

            return blocks;
        }

        private static AnalyzerClassInfo ExtractClassInfo(
            string classContent,
            string filePath,
            string namespaceName,
            string fullCode,
            int classStartIndex
        )
        {
            Match classMatch = ClassRegex.Match(classContent);
            if (!classMatch.Success)
            {
                return null;
            }

            string className = classMatch.Groups["name"].Value;
            string baseList = classMatch.Groups["base"].Value.Trim();
            string baseClassName = null;

            if (!string.IsNullOrEmpty(baseList))
            {
                string[] baseTypes = baseList.Split(',');
                if (baseTypes.Length > 0)
                {
                    string firstBase = baseTypes[0].Trim();
                    int genericIndex = firstBase.IndexOf('<');
                    if (genericIndex > 0)
                    {
                        firstBase = firstBase.Substring(0, genericIndex).Trim();
                    }

                    if (
                        !string.IsNullOrEmpty(firstBase)
                        && !firstBase.StartsWith("I", StringComparison.Ordinal)
                    )
                    {
                        baseClassName = firstBase;
                    }
                    else if (baseTypes.Length > 1)
                    {
                        for (int i = 1; i < baseTypes.Length; i++)
                        {
                            string candidate = baseTypes[i].Trim();
                            int gi = candidate.IndexOf('<');
                            if (gi > 0)
                            {
                                candidate = candidate.Substring(0, gi).Trim();
                            }

                            if (
                                !string.IsNullOrEmpty(candidate)
                                && !candidate.StartsWith("I", StringComparison.Ordinal)
                            )
                            {
                                baseClassName = candidate;
                                break;
                            }
                        }
                    }
                }
            }

            string fullName = string.IsNullOrEmpty(namespaceName)
                ? className
                : $"{namespaceName}.{className}";

            int lineNumber = CountLines(fullCode, classStartIndex);

            Dictionary<string, List<AnalyzerMethodInfo>> methods = ExtractMethods(
                classContent,
                fullCode,
                classStartIndex
            );

            return new AnalyzerClassInfo
            {
                Name = className,
                FullName = fullName,
                BaseClassName = baseClassName,
                FilePath = filePath,
                Methods = methods,
                LineNumber = lineNumber,
            };
        }

        private static Dictionary<string, List<AnalyzerMethodInfo>> ExtractMethods(
            string classContent,
            string fullCode,
            int classStartOffset
        )
        {
            Dictionary<string, List<AnalyzerMethodInfo>> methods = new();
            MatchCollection methodMatches = MethodRegex.Matches(classContent);

            foreach (Match match in methodMatches)
            {
                string modifiers = match.Groups["modifiers"].Value;
                string returnType = match.Groups["return"].Value.Trim();
                string methodName = match.Groups["name"].Value;
                string paramsStr = match.Groups["params"].Value;

                if (string.Equals(returnType, "if", StringComparison.Ordinal))
                {
                    continue;
                }

                if (string.Equals(returnType, "while", StringComparison.Ordinal))
                {
                    continue;
                }

                if (string.Equals(returnType, "for", StringComparison.Ordinal))
                {
                    continue;
                }

                if (string.Equals(returnType, "foreach", StringComparison.Ordinal))
                {
                    continue;
                }

                if (string.Equals(returnType, "switch", StringComparison.Ordinal))
                {
                    continue;
                }

                if (string.Equals(returnType, "catch", StringComparison.Ordinal))
                {
                    continue;
                }

                if (string.Equals(returnType, "using", StringComparison.Ordinal))
                {
                    continue;
                }

                if (string.Equals(returnType, "lock", StringComparison.Ordinal))
                {
                    continue;
                }

                if (string.Equals(returnType, "else", StringComparison.Ordinal))
                {
                    continue;
                }

                if (string.Equals(returnType, "return", StringComparison.Ordinal))
                {
                    continue;
                }

                List<string> parameters = new();
                List<string> parameterTypes = new();

                if (!string.IsNullOrWhiteSpace(paramsStr))
                {
                    string[] paramParts = paramsStr.Split(',');
                    foreach (string param in paramParts)
                    {
                        string trimmed = param.Trim();
                        if (string.IsNullOrWhiteSpace(trimmed))
                        {
                            continue;
                        }

                        parameters.Add(trimmed);
                        int lastSpace = trimmed.LastIndexOf(' ');
                        if (lastSpace > 0)
                        {
                            string typeStr = trimmed.Substring(0, lastSpace).Trim();
                            if (typeStr.StartsWith("this ", StringComparison.Ordinal))
                            {
                                typeStr = typeStr.Substring(5).Trim();
                            }

                            if (typeStr.StartsWith("params ", StringComparison.Ordinal))
                            {
                                typeStr = typeStr.Substring(7).Trim();
                            }

                            if (typeStr.StartsWith("ref ", StringComparison.Ordinal))
                            {
                                typeStr = "ref " + typeStr.Substring(4).Trim();
                            }

                            if (typeStr.StartsWith("out ", StringComparison.Ordinal))
                            {
                                typeStr = "out " + typeStr.Substring(4).Trim();
                            }

                            if (typeStr.StartsWith("in ", StringComparison.Ordinal))
                            {
                                typeStr = "in " + typeStr.Substring(3).Trim();
                            }

                            parameterTypes.Add(typeStr);
                        }
                        else
                        {
                            parameterTypes.Add(trimmed);
                        }
                    }
                }

                string signature = $"{returnType} {methodName}({string.Join(", ", parameters)})";
                int lineNumber = CountLines(fullCode, classStartOffset + match.Index);

                AnalyzerMethodInfo methodInfo = new()
                {
                    Name = methodName,
                    Signature = signature,
                    IsVirtual = modifiers.Contains("virtual"),
                    IsOverride = modifiers.Contains("override"),
                    IsNew = modifiers.Contains("new"),
                    IsAbstract = modifiers.Contains("abstract"),
                    IsSealed = modifiers.Contains("sealed"),
                    IsPrivate =
                        modifiers.Contains("private")
                        || (
                            !modifiers.Contains("public")
                            && !modifiers.Contains("protected")
                            && !modifiers.Contains("internal")
                        ),
                    IsProtected = modifiers.Contains("protected"),
                    IsPublic = modifiers.Contains("public"),
                    IsInternal = modifiers.Contains("internal"),
                    IsStatic = modifiers.Contains("static"),
                    LineNumber = lineNumber,
                    ReturnType = returnType,
                    Parameters = parameters,
                    ParameterTypes = parameterTypes,
                };

                if (!methods.ContainsKey(methodName))
                {
                    methods[methodName] = new List<AnalyzerMethodInfo>();
                }

                methods[methodName].Add(methodInfo);
            }

            return methods;
        }

        private static int CountLines(string text, int upToIndex)
        {
            int lineNumber = 1;
            for (int i = 0; i < upToIndex && i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    lineNumber++;
                }
            }

            return lineNumber;
        }

        private void AnalyzeInheritance()
        {
            foreach (AnalyzerClassInfo classInfo in _classes.Values)
            {
                bool isUnityClass = IsUnityDerivedClass(classInfo);

                AnalyzerClassInfo baseClass = null;
                if (classInfo.BaseClassName != null)
                {
                    baseClass = FindBaseClass(classInfo.BaseClassName);
                }

                foreach (KeyValuePair<string, List<AnalyzerMethodInfo>> kvp in classInfo.Methods)
                {
                    string methodName = kvp.Key;
                    List<AnalyzerMethodInfo> methods = kvp.Value;

                    foreach (AnalyzerMethodInfo method in methods)
                    {
                        if (isUnityClass && UnityMethods.LifecycleMethods.Contains(methodName))
                        {
                            AnalyzeUnityLifecycleMethod(classInfo, method, baseClass);
                        }

                        if (baseClass != null)
                        {
                            AnalyzeMethodAgainstBase(classInfo, method, baseClass, isUnityClass);
                        }
                    }
                }

                if (baseClass != null)
                {
                    AnalyzeAgainstAncestors(classInfo, baseClass, isUnityClass);
                }
            }
        }

        private AnalyzerClassInfo FindBaseClass(string baseClassName)
        {
            AnalyzerClassInfo match = _classes.Values.FirstOrDefault(c =>
                string.Equals(c.Name, baseClassName, StringComparison.Ordinal)
                || string.Equals(c.FullName, baseClassName, StringComparison.Ordinal)
            );
            if (match != null)
            {
                return match;
            }

            return _classes.Values.FirstOrDefault(c =>
                c.FullName.EndsWith("." + baseClassName, StringComparison.Ordinal)
            );
        }

        private bool IsUnityDerivedClass(AnalyzerClassInfo classInfo)
        {
            HashSet<string> visited = new();
            AnalyzerClassInfo current = classInfo;

            while (current != null)
            {
                if (!visited.Add(current.FullName))
                {
                    break;
                }

                if (
                    current.BaseClassName != null
                    && UnityMethods.MonoBehaviourBaseClasses.Contains(current.BaseClassName)
                )
                {
                    return true;
                }

                if (current.BaseClassName == null)
                {
                    break;
                }

                current = FindBaseClass(current.BaseClassName);
            }

            return false;
        }

        private void AnalyzeUnityLifecycleMethod(
            AnalyzerClassInfo classInfo,
            AnalyzerMethodInfo method,
            AnalyzerClassInfo baseClass
        )
        {
            if (method.IsPrivate && baseClass != null)
            {
                if (
                    baseClass.Methods.TryGetValue(
                        method.Name,
                        out List<AnalyzerMethodInfo> baseMethods
                    )
                )
                {
                    foreach (AnalyzerMethodInfo baseMethod in baseMethods)
                    {
                        if (baseMethod.IsPrivate)
                        {
                            string derivedTypeStr = string.Join(", ", method.ParameterTypes);
                            string baseTypeStr = string.Join(", ", baseMethod.ParameterTypes);

                            if (
                                string.Equals(derivedTypeStr, baseTypeStr, StringComparison.Ordinal)
                            )
                            {
                                _issues.Add(
                                    new AnalyzerIssue(
                                        classInfo.FilePath,
                                        classInfo.Name,
                                        method.Name,
                                        "UnityPrivateMethodShadowing",
                                        $"Both '{classInfo.Name}' and base class '{baseClass.Name}' have private '{method.Name}' methods with matching parameters. Unity will call both methods, but only the derived class method will be invoked by Unity's lifecycle system for the derived type.",
                                        IssueSeverity.Critical,
                                        $"Make the base class method 'protected virtual' and use 'protected override' in the derived class, calling base.{method.Name}() as needed.",
                                        method.LineNumber,
                                        IssueCategory.UnityLifecycle,
                                        baseClass.Name,
                                        baseMethod.Signature,
                                        method.Signature
                                    )
                                );
                            }
                        }
                    }
                }
            }

            if (
                baseClass != null
                && baseClass.Methods.TryGetValue(
                    method.Name,
                    out List<AnalyzerMethodInfo> baseMethodsForReturnCheck
                )
            )
            {
                foreach (AnalyzerMethodInfo baseMethod in baseMethodsForReturnCheck)
                {
                    string derivedTypeStr = string.Join(", ", method.ParameterTypes);
                    string baseTypeStr = string.Join(", ", baseMethod.ParameterTypes);

                    if (
                        string.Equals(derivedTypeStr, baseTypeStr, StringComparison.Ordinal)
                        && !string.Equals(
                            method.ReturnType,
                            baseMethod.ReturnType,
                            StringComparison.Ordinal
                        )
                    )
                    {
                        bool bothAreValid =
                            UnityMethods.IsValidUnityLifecycleReturnType(
                                method.Name,
                                method.ReturnType
                            )
                            && UnityMethods.IsValidUnityLifecycleReturnType(
                                method.Name,
                                baseMethod.ReturnType
                            );

                        if (bothAreValid)
                        {
                            _issues.Add(
                                new AnalyzerIssue(
                                    classInfo.FilePath,
                                    classInfo.Name,
                                    method.Name,
                                    "UnityLifecycleReturnTypeMismatch",
                                    $"Unity lifecycle method '{method.Name}' in '{classInfo.Name}' has return type '{method.ReturnType}' but base class '{baseClass.Name}' has '{baseMethod.ReturnType}'. Both are valid Unity signatures, so Unity will call BOTH methods independently.",
                                    IssueSeverity.High,
                                    "If you intend to override, make the base method 'protected virtual' and use 'override' in the derived class.",
                                    method.LineNumber,
                                    IssueCategory.UnityLifecycle,
                                    baseClass.Name,
                                    baseMethod.Signature,
                                    method.Signature
                                )
                            );
                        }
                        else if (
                            !UnityMethods.IsValidUnityLifecycleReturnType(
                                method.Name,
                                method.ReturnType
                            )
                        )
                        {
                            _issues.Add(
                                new AnalyzerIssue(
                                    classInfo.FilePath,
                                    classInfo.Name,
                                    method.Name,
                                    "InvalidUnityLifecycleReturnType",
                                    $"Unity lifecycle method '{method.Name}' in '{classInfo.Name}' has return type '{method.ReturnType}' which Unity will not recognize. Unity will only call the base class version.",
                                    IssueSeverity.Critical,
                                    "Change the return type to match the expected Unity signature (void, or IEnumerator for Start).",
                                    method.LineNumber,
                                    IssueCategory.UnityLifecycle,
                                    baseClass.Name,
                                    baseMethod.Signature,
                                    method.Signature
                                )
                            );
                        }
                    }
                }
            }

            // Only flag unexpected parameters if this is not an override (overrides
            // are legitimately following a base class signature, e.g., PropertyDrawer.OnGUI)
            if (
                method.Parameters.Count > 0
                && !UnityMethods.MethodsWithParameters.Contains(method.Name)
                && !method.IsOverride
            )
            {
                _issues.Add(
                    new AnalyzerIssue(
                        classInfo.FilePath,
                        classInfo.Name,
                        method.Name,
                        "UnexpectedParameters",
                        $"Unity lifecycle method '{method.Name}' has {method.Parameters.Count} parameters but should have none. Unity will not call this method.",
                        IssueSeverity.Critical,
                        $"Remove the parameters from '{method.Name}' or rename the method if it's not intended to be a Unity callback.",
                        method.LineNumber,
                        IssueCategory.UnityLifecycle,
                        derivedMethodSignature: method.Signature
                    )
                );
            }

            if (method.IsStatic)
            {
                _issues.Add(
                    new AnalyzerIssue(
                        classInfo.FilePath,
                        classInfo.Name,
                        method.Name,
                        "StaticLifecycleMethod",
                        $"Unity lifecycle method '{method.Name}' is declared as static. Unity will not call static lifecycle methods.",
                        IssueSeverity.Critical,
                        $"Remove the 'static' modifier from '{method.Name}'.",
                        method.LineNumber,
                        IssueCategory.UnityLifecycle,
                        derivedMethodSignature: method.Signature
                    )
                );
            }
        }

        private void AnalyzeMethodAgainstBase(
            AnalyzerClassInfo classInfo,
            AnalyzerMethodInfo method,
            AnalyzerClassInfo baseClass,
            bool isUnityClass
        )
        {
            if (
                !baseClass.Methods.TryGetValue(
                    method.Name,
                    out List<AnalyzerMethodInfo> baseMethods
                )
            )
            {
                return;
            }

            bool isUnityMethod = UnityMethods.LifecycleMethods.Contains(method.Name);
            IssueCategory GetCategory() =>
                isUnityClass
                    ? (
                        isUnityMethod
                            ? IssueCategory.UnityLifecycle
                            : IssueCategory.UnityInheritance
                    )
                    : IssueCategory.GeneralInheritance;

            foreach (AnalyzerMethodInfo baseMethod in baseMethods)
            {
                if (baseMethod.IsPrivate && method.IsPrivate)
                {
                    if (isUnityClass && isUnityMethod)
                    {
                        continue;
                    }

                    string derivedTypeStr = string.Join(", ", method.ParameterTypes);
                    string baseTypeStr = string.Join(", ", baseMethod.ParameterTypes);

                    if (string.Equals(derivedTypeStr, baseTypeStr, StringComparison.Ordinal))
                    {
                        _issues.Add(
                            new AnalyzerIssue(
                                classInfo.FilePath,
                                classInfo.Name,
                                method.Name,
                                "PrivateMethodShadowing",
                                $"Both '{classInfo.Name}' and base class '{baseClass.Name}' have private methods named '{method.Name}' with matching parameters. The base class method is inaccessible.",
                                IssueSeverity.High,
                                "Consider making the base method protected virtual if you need polymorphic behavior.",
                                method.LineNumber,
                                GetCategory(),
                                baseClass.Name,
                                baseMethod.Signature,
                                method.Signature
                            )
                        );
                    }
                }

                if (!baseMethod.IsVirtual && !baseMethod.IsAbstract)
                {
                    if (!method.IsNew && !method.IsOverride && !method.IsPrivate)
                    {
                        string derivedTypeStr = string.Join(", ", method.ParameterTypes);
                        string baseTypeStr = string.Join(", ", baseMethod.ParameterTypes);

                        if (string.Equals(derivedTypeStr, baseTypeStr, StringComparison.Ordinal))
                        {
                            IssueSeverity severity = isUnityMethod
                                ? IssueSeverity.Critical
                                : IssueSeverity.High;

                            _issues.Add(
                                new AnalyzerIssue(
                                    classInfo.FilePath,
                                    classInfo.Name,
                                    method.Name,
                                    "HidingNonVirtualMethod",
                                    $"Method '{method.Name}' in '{classInfo.Name}' hides non-virtual method in base class '{baseClass.Name}' without using 'new' keyword.",
                                    severity,
                                    "Either: (1) Add 'new' keyword if hiding is intentional, (2) Make base method 'virtual' and use 'override', or (3) Rename the derived method.",
                                    method.LineNumber,
                                    GetCategory(),
                                    baseClass.Name,
                                    baseMethod.Signature,
                                    method.Signature
                                )
                            );
                        }
                    }
                }

                if ((baseMethod.IsVirtual || baseMethod.IsAbstract) && !baseMethod.IsSealed)
                {
                    string derivedTypeStr = string.Join(", ", method.ParameterTypes);
                    string baseTypeStr = string.Join(", ", baseMethod.ParameterTypes);

                    if (
                        string.Equals(derivedTypeStr, baseTypeStr, StringComparison.Ordinal)
                        && string.Equals(
                            method.ReturnType,
                            baseMethod.ReturnType,
                            StringComparison.Ordinal
                        )
                    )
                    {
                        if (!method.IsOverride && !method.IsNew && !method.IsPrivate)
                        {
                            IssueSeverity severity = isUnityMethod
                                ? IssueSeverity.High
                                : IssueSeverity.Medium;

                            _issues.Add(
                                new AnalyzerIssue(
                                    classInfo.FilePath,
                                    classInfo.Name,
                                    method.Name,
                                    "MissingOverride",
                                    $"Method '{method.Name}' in '{classInfo.Name}' hides virtual method in base class '{baseClass.Name}'. Missing 'override' keyword.",
                                    severity,
                                    "Add 'override' keyword to properly override the base method, or add 'new' if hiding is intentional.",
                                    method.LineNumber,
                                    GetCategory(),
                                    baseClass.Name,
                                    baseMethod.Signature,
                                    method.Signature
                                )
                            );
                        }
                        else if (method.IsNew)
                        {
                            IssueSeverity severity = isUnityMethod
                                ? IssueSeverity.High
                                : IssueSeverity.Medium;

                            _issues.Add(
                                new AnalyzerIssue(
                                    classInfo.FilePath,
                                    classInfo.Name,
                                    method.Name,
                                    "UsingNewOnVirtual",
                                    $"Method '{method.Name}' in '{classInfo.Name}' uses 'new' to hide virtual method in base class '{baseClass.Name}'. Consider using 'override' instead.",
                                    severity,
                                    "Replace 'new' with 'override' unless you specifically need to hide the base implementation.",
                                    method.LineNumber,
                                    GetCategory(),
                                    baseClass.Name,
                                    baseMethod.Signature,
                                    method.Signature
                                )
                            );
                        }
                    }
                }

                if (baseMethod.IsProtected && method.IsPrivate && method.IsOverride)
                {
                    _issues.Add(
                        new AnalyzerIssue(
                            classInfo.FilePath,
                            classInfo.Name,
                            method.Name,
                            "AccessibilityReduction",
                            $"Method '{method.Name}' in '{classInfo.Name}' reduces accessibility from protected (in '{baseClass.Name}') to private.",
                            IssueSeverity.Medium,
                            "Keep the method protected to maintain the inheritance contract.",
                            method.LineNumber,
                            GetCategory(),
                            baseClass.Name,
                            baseMethod.Signature,
                            method.Signature
                        )
                    );
                }

                if (baseMethod.IsPublic && !method.IsPublic && method.IsOverride)
                {
                    string accessLevel =
                        method.IsProtected ? "protected"
                        : method.IsInternal ? "internal"
                        : "private";

                    _issues.Add(
                        new AnalyzerIssue(
                            classInfo.FilePath,
                            classInfo.Name,
                            method.Name,
                            "AccessibilityReduction",
                            $"Method '{method.Name}' in '{classInfo.Name}' reduces accessibility from public (in '{baseClass.Name}') to {accessLevel}.",
                            IssueSeverity.Medium,
                            "Keep the method public to maintain the inheritance contract.",
                            method.LineNumber,
                            GetCategory(),
                            baseClass.Name,
                            baseMethod.Signature,
                            method.Signature
                        )
                    );
                }
            }

            if (method.IsOverride || method.IsNew)
            {
                string derivedTypeStr = string.Join(", ", method.ParameterTypes);
                AnalyzerMethodInfo matchingMethod = baseMethods.FirstOrDefault(bm =>
                    string.Equals(
                        string.Join(", ", bm.ParameterTypes),
                        derivedTypeStr,
                        StringComparison.Ordinal
                    )
                );

                if (matchingMethod == null)
                {
                    AnalyzerMethodInfo closestMethod = baseMethods
                        .Where(bm => !bm.IsPrivate)
                        .OrderByDescending(bm =>
                            GetParameterSimilarity(method.ParameterTypes, bm.ParameterTypes)
                        )
                        .FirstOrDefault();

                    if (closestMethod != null)
                    {
                        _issues.Add(
                            new AnalyzerIssue(
                                classInfo.FilePath,
                                classInfo.Name,
                                method.Name,
                                "SignatureMismatch",
                                $"Method '{method.Name}' in '{classInfo.Name}' has {(method.IsOverride ? "'override'" : "'new'")} but parameters don't match any base method in '{baseClass.Name}'.",
                                IssueSeverity.High,
                                "Ensure the derived method signature matches one of the base method overloads.",
                                method.LineNumber,
                                GetCategory(),
                                baseClass.Name,
                                closestMethod.Signature,
                                method.Signature
                            )
                        );
                    }
                }
                else if (
                    !string.Equals(
                        method.ReturnType,
                        matchingMethod.ReturnType,
                        StringComparison.Ordinal
                    )
                )
                {
                    _issues.Add(
                        new AnalyzerIssue(
                            classInfo.FilePath,
                            classInfo.Name,
                            method.Name,
                            "ReturnTypeMismatch",
                            $"Method '{method.Name}' in '{classInfo.Name}' has different return type ({method.ReturnType}) than base class '{baseClass.Name}' ({matchingMethod.ReturnType}).",
                            IssueSeverity.High,
                            "Change the return type to match the base method.",
                            method.LineNumber,
                            GetCategory(),
                            baseClass.Name,
                            matchingMethod.Signature,
                            method.Signature
                        )
                    );
                }
            }
        }

        private void AnalyzeAgainstAncestors(
            AnalyzerClassInfo classInfo,
            AnalyzerClassInfo directBase,
            bool isUnityClass
        )
        {
            HashSet<string> visited = new() { classInfo.FullName, directBase.FullName };
            AnalyzerClassInfo currentAncestor =
                directBase.BaseClassName != null ? FindBaseClass(directBase.BaseClassName) : null;

            while (currentAncestor != null)
            {
                if (!visited.Add(currentAncestor.FullName))
                {
                    break;
                }

                foreach (KeyValuePair<string, List<AnalyzerMethodInfo>> kvp in classInfo.Methods)
                {
                    List<AnalyzerMethodInfo> methods = kvp.Value;

                    foreach (AnalyzerMethodInfo method in methods)
                    {
                        if (method.IsOverride || method.IsNew)
                        {
                            continue;
                        }

                        AnalyzeMethodAgainstAncestor(
                            classInfo,
                            method,
                            currentAncestor,
                            isUnityClass
                        );
                    }
                }

                currentAncestor =
                    currentAncestor.BaseClassName != null
                        ? FindBaseClass(currentAncestor.BaseClassName)
                        : null;
            }
        }

        private void AnalyzeMethodAgainstAncestor(
            AnalyzerClassInfo classInfo,
            AnalyzerMethodInfo method,
            AnalyzerClassInfo ancestor,
            bool isUnityClass
        )
        {
            if (
                !ancestor.Methods.TryGetValue(
                    method.Name,
                    out List<AnalyzerMethodInfo> ancestorMethods
                )
            )
            {
                return;
            }

            foreach (AnalyzerMethodInfo ancestorMethod in ancestorMethods)
            {
                if (
                    (ancestorMethod.IsVirtual || ancestorMethod.IsAbstract)
                    && !ancestorMethod.IsSealed
                    && !ancestorMethod.IsPrivate
                )
                {
                    string derivedTypeStr = string.Join(", ", method.ParameterTypes);
                    string ancestorTypeStr = string.Join(", ", ancestorMethod.ParameterTypes);

                    if (
                        string.Equals(derivedTypeStr, ancestorTypeStr, StringComparison.Ordinal)
                        && string.Equals(
                            method.ReturnType,
                            ancestorMethod.ReturnType,
                            StringComparison.Ordinal
                        )
                    )
                    {
                        bool isUnityMethod = UnityMethods.LifecycleMethods.Contains(method.Name);
                        IssueCategory category = isUnityClass
                            ? (
                                isUnityMethod
                                    ? IssueCategory.UnityLifecycle
                                    : IssueCategory.UnityInheritance
                            )
                            : IssueCategory.GeneralInheritance;
                        IssueSeverity severity = isUnityMethod
                            ? IssueSeverity.High
                            : IssueSeverity.Medium;

                        _issues.Add(
                            new AnalyzerIssue(
                                classInfo.FilePath,
                                classInfo.Name,
                                method.Name,
                                "MissingOverrideFromAncestor",
                                $"Method '{method.Name}' in '{classInfo.Name}' hides virtual method from ancestor class '{ancestor.Name}'. Missing 'override' keyword may cause unexpected behavior.",
                                severity,
                                "Add 'override' keyword to properly override the ancestor method, or add 'new' if hiding is intentional.",
                                method.LineNumber,
                                category,
                                ancestor.Name,
                                ancestorMethod.Signature,
                                method.Signature
                            )
                        );
                    }
                }
            }
        }

        private static int GetParameterSimilarity(List<string> types1, List<string> types2)
        {
            int score = 0;

            if (types1.Count == types2.Count)
            {
                score += 10;
            }

            int minCount = Math.Min(types1.Count, types2.Count);
            for (int i = 0; i < minCount; i++)
            {
                if (string.Equals(types1[i], types2[i], StringComparison.Ordinal))
                {
                    score += 5;
                }
            }

            return score;
        }
    }
#endif
}
