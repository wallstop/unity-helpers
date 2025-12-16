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
    using WallstopStudios.UnityHelpers.Core.Extension;

    /// <summary>
    /// Analyzes C# files for Unity MonoBehaviour method issues.
    /// Uses regex-based parsing since Roslyn is not available in Unity Editor.
    /// </summary>
    public sealed class MethodAnalyzer
    {
        private readonly ConcurrentDictionary<string, AnalyzerClassInfo> _classes = new();
        private readonly ConcurrentBag<AnalyzerIssue> _issues = new();

        /// <summary>
        /// Lookup dictionary for finding classes by their simple name (without namespace).
        /// Maps simple class name to list of matching classes (handles same name in different namespaces).
        /// </summary>
        private ConcurrentDictionary<string, List<AnalyzerClassInfo>> _classNameLookup = new();

        public IReadOnlyList<AnalyzerIssue> Issues => _issues.ToList();
        public IReadOnlyDictionary<string, AnalyzerClassInfo> Classes => _classes;

        private static readonly Regex ClassRegex = new(
            @"(?:(?<abstract>abstract)\s+)?(?:(?<sealed>sealed)\s+)?(?:(?<partial>partial)\s+)?class\s+(?<name>\w+)(?:\s*<(?<typeParams>[^>]+)>)?(?:\s*:\s*(?<base>[^{]+))?",
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

        private static readonly Regex SingleLineCommentRegex = new(
            @"//[^\r\n]*",
            RegexOptions.Compiled
        );

        private static readonly Regex MultiLineCommentRegex = new(
            @"/\*[\s\S]*?\*/",
            RegexOptions.Compiled
        );

        private static readonly Regex StringLiteralRegex = new(
            @"@""(?:[^""]|"""")*""|""(?:[^""\\]|\\.)*""",
            RegexOptions.Compiled
        );

        private static readonly Regex NewExpressionRegex = new(
            @"new\s+(?<type>[\w<>\[\],\.]+)\s*\(",
            RegexOptions.Compiled
        );

        private static readonly Regex GenericArgsRegex = new(
            @"<(?<args>[^<>]+(?:<[^<>]+>[^<>]*)*)>",
            RegexOptions.Compiled
        );

        private static readonly Regex PreprocessorDirectiveRegex = new(
            @"^\s*#(?:if|else|elif|endif|define|undef|pragma|region|endregion|warning|error|line|nullable)[^\r\n]*",
            RegexOptions.Compiled | RegexOptions.Multiline
        );

        private static readonly Regex SuppressAnalyzerAttributeRegex = new(
            @"\[\s*SuppressAnalyzer(?:Attribute)?\s*(?:\([^)]*\))?\s*\]",
            RegexOptions.Compiled
        );

        /// <summary>
        /// Clears all cached data from a previous analysis.
        /// </summary>
        public void Clear()
        {
            _classes.Clear();
            _classNameLookup.Clear();
            while (_issues.TryTake(out _)) { }
        }

        /// <summary>
        /// Builds the class name lookup dictionary for O(1) base class resolution.
        /// Should be called after all files have been parsed.
        /// </summary>
        private void BuildClassNameLookup()
        {
            _classNameLookup = new ConcurrentDictionary<string, List<AnalyzerClassInfo>>();

            foreach (AnalyzerClassInfo classInfo in _classes.Values)
            {
                _classNameLookup.AddOrUpdate(
                    classInfo.Name,
                    _ => new List<AnalyzerClassInfo> { classInfo },
                    (_, existing) =>
                    {
                        lock (existing)
                        {
                            existing.Add(classInfo);
                        }

                        return existing;
                    }
                );
            }
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

            // Build lookup dictionary before inheritance analysis for O(1) class resolution
            BuildClassNameLookup();

            await Task.Run(() => AnalyzeInheritanceParallel(cancellationToken), cancellationToken);
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

            // Build lookup dictionary before inheritance analysis for O(1) class resolution
            BuildClassNameLookup();

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
                        async () =>
                        {
                            try
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                await ParseFileAsync(file, rootPath).ConfigureAwait(false);
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

        private void ParseFile(string filePath, string rootPath)
        {
            try
            {
                string code = File.ReadAllText(filePath);
                ParseFileContent(filePath, rootPath, code);
            }
            catch (Exception)
            {
                // Skip files that can't be parsed
            }
        }

        private async Task ParseFileAsync(string filePath, string rootPath)
        {
            try
            {
                string code = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
                ParseFileContent(filePath, rootPath, code);
            }
            catch (Exception)
            {
                // Skip files that can't be parsed
            }
        }

        private void ParseFileContent(string filePath, string rootPath, string code)
        {
            string relativePath = GetRelativePath(rootPath, filePath);

            // Strip comments, strings, and preprocessor directives before extracting classes
            // This prevents false positives from code examples in string literals
            string strippedCode = StripCommentsAndStrings(code);

            string currentNamespace = ExtractNamespace(code);
            List<(int start, int end, string content)> classBlocks = ExtractClassBlocks(
                strippedCode
            );

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

        /// <summary>
        /// Strips comments, string literals, and preprocessor directives from code to avoid false positives in method detection.
        /// Returns a version of the code with these elements replaced by spaces (preserving line numbers).
        /// </summary>
        private static string StripCommentsAndStrings(string code)
        {
            // Replace string literals first (they might contain // or /* which aren't comments)
            string result = StringLiteralRegex.Replace(code, m => new string(' ', m.Length));

            // Replace multi-line comments
            result = MultiLineCommentRegex.Replace(
                result,
                m =>
                {
                    // Preserve newlines to keep line numbers correct
                    int newlines = m.Value.Count(c => c == '\n');
                    return new string('\n', newlines) + new string(' ', m.Length - newlines);
                }
            );

            // Replace single-line comments
            result = SingleLineCommentRegex.Replace(result, m => new string(' ', m.Length));

            // Replace preprocessor directives (e.g., #if, #endif, etc.)
            result = PreprocessorDirectiveRegex.Replace(result, m => new string(' ', m.Length));

            return result;
        }

        /// <summary>
        /// Checks if a method match is actually a 'new' expression (constructor call) rather than a method declaration.
        /// This handles various patterns:
        /// - Direct: new Vector3(...)
        /// - Array initializer: new[] { new Vector3(...), new Vector3(...) }
        /// - Collection initializer: new List&lt;Vector3&gt; { new Vector3(...) }
        /// - Inline after comma: { new Vector3(0, 0, 0), new Vector3(1, 0, 0) }
        /// - Named parameter: new(center: new Vector3(...), size: new Vector3(...))
        /// - After close paren: ), new Vector3(...)
        /// </summary>
        private static bool IsNewExpression(string code, Match methodMatch)
        {
            int startPos = methodMatch.Index;
            int searchStart = Math.Max(0, startPos - 100);
            string prefix = code.Substring(searchStart, startPos - searchStart);

            // Get the matched "return type" which for a new expression is often
            // a type name that looks like a return type but isn't
            string returnType = methodMatch.Groups["return"].Value.Trim();
            string methodName = methodMatch.Groups["name"].Value;

            // Pattern 1: Direct "new TypeName(" where TypeName matches the method name
            if (
                Regex.IsMatch(
                    prefix,
                    @"new\s+" + Regex.Escape(methodName) + @"\s*$",
                    RegexOptions.RightToLeft
                )
            )
            {
                return true;
            }

            // Pattern 2: Check if we're inside a collection/array initializer context
            // by looking for patterns like ", new" or "{ new" before the type name
            // The "return type" would be "new" in this case since the regex captures it
            if (string.Equals(returnType, "new", StringComparison.Ordinal))
            {
                return true;
            }

            // Pattern 3: Return type contains "new" preceded by punctuation (e.g., ", new", "( new", ": new")
            // This happens when the regex captures context like ", new" or "{ new" as the return type
            if (
                returnType.EndsWith(" new", StringComparison.Ordinal)
                || returnType.EndsWith("\tnew", StringComparison.Ordinal)
            )
            {
                return true;
            }

            // Pattern 4: Return type starts with punctuation that wouldn't be valid in a real return type
            // This catches cases like ", new" where the comma gets captured
            if (returnType.Length > 0 && !char.IsLetter(returnType[0]) && returnType[0] != '_')
            {
                return true;
            }

            // Pattern 5: Array initializer with generic types - "new Vector3(" preceded by ", " or "{ "
            // This catches cases like: new[] { new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 0f) }
            // where the second Vector3 appears after a comma
            if (
                Regex.IsMatch(
                    prefix,
                    @"[,{]\s*new\s+" + Regex.Escape(methodName) + @"(?:\s*<[^>]+>)?\s*$",
                    RegexOptions.RightToLeft
                )
            )
            {
                return true;
            }

            // Pattern 6: After close paren with new - like ), new Vector3(...)
            // This can occur in method call chains or nested expressions
            if (
                Regex.IsMatch(
                    prefix,
                    @"\)\s*,\s*new\s+" + Regex.Escape(methodName) + @"(?:\s*<[^>]+>)?\s*$",
                    RegexOptions.RightToLeft
                )
            )
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Extracts generic type arguments from a type declaration like "WDropdownSelectorBase&lt;int&gt;".
        /// Properly handles nested generics like "Dictionary&lt;string, List&lt;int&gt;&gt;".
        /// </summary>
        private static List<string> ExtractGenericArguments(string typeDeclaration)
        {
            List<string> args = new();
            Match match = GenericArgsRegex.Match(typeDeclaration);
            if (match.Success)
            {
                string argsStr = match.Groups["args"].Value;
                args.AddRange(SplitByCommaRespectingGenerics(argsStr));
            }

            return args;
        }

        /// <summary>
        /// Splits a string by commas, but respects nested angle brackets.
        /// For example, "A, B&lt;C, D&gt;, E" becomes ["A", "B&lt;C, D&gt;", "E"].
        /// </summary>
        private static List<string> SplitByCommaRespectingGenerics(string input)
        {
            List<string> results = new();
            int depth = 0;
            int start = 0;

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (c == '<')
                {
                    depth++;
                }
                else if (c == '>')
                {
                    depth--;
                }
                else if (c == ',' && depth == 0)
                {
                    string part = input.Substring(start, i - start).Trim();
                    if (!string.IsNullOrEmpty(part))
                    {
                        results.Add(part);
                    }

                    start = i + 1;
                }
            }

            // Add the last part
            if (start < input.Length)
            {
                string part = input.Substring(start).Trim();
                if (!string.IsNullOrEmpty(part))
                {
                    results.Add(part);
                }
            }

            return results;
        }

        /// <summary>
        /// Extracts generic type parameters from a class declaration like "class MyClass&lt;TKey, TValue&gt;".
        /// </summary>
        private static List<string> ExtractGenericTypeParameters(string typeParamsStr)
        {
            if (string.IsNullOrWhiteSpace(typeParamsStr))
            {
                return new List<string>();
            }

            return typeParamsStr
                .Split(',')
                .Select(p => p.Trim().Split(' ')[0]) // Handle "T : IComparable" style constraints
                .Where(p => !string.IsNullOrEmpty(p))
                .ToList();
        }

        /// <summary>
        /// Resolves a type by substituting generic type parameters with their concrete arguments.
        /// </summary>
        private static string ResolveGenericType(
            string type,
            IReadOnlyList<string> baseTypeParams,
            IReadOnlyList<string> derivedTypeArgs
        )
        {
            if (
                baseTypeParams == null
                || derivedTypeArgs == null
                || baseTypeParams.Count == 0
                || derivedTypeArgs.Count == 0
            )
            {
                return type;
            }

            string resolved = type;
            int minCount = Math.Min(baseTypeParams.Count, derivedTypeArgs.Count);
            for (int i = 0; i < minCount; i++)
            {
                string param = baseTypeParams[i];
                string arg = derivedTypeArgs[i];

                // Only replace whole words (type parameters)
                resolved = Regex.Replace(resolved, @"\b" + Regex.Escape(param) + @"\b", arg);
            }

            return resolved;
        }

        /// <summary>
        /// Checks if two types are equivalent, accounting for generic type resolution.
        /// </summary>
        private bool TypesAreEquivalent(
            string derivedType,
            string baseType,
            AnalyzerClassInfo derivedClass,
            AnalyzerClassInfo baseClass
        )
        {
            // Direct match
            if (string.Equals(derivedType, baseType, StringComparison.Ordinal))
            {
                return true;
            }

            // Try resolving generics from derived class to base class
            if (
                derivedClass.BaseClassTypeArguments != null
                && derivedClass.BaseClassTypeArguments.Count > 0
                && baseClass.GenericTypeParameters != null
                && baseClass.GenericTypeParameters.Count > 0
            )
            {
                string resolvedBaseType = ResolveGenericType(
                    baseType,
                    baseClass.GenericTypeParameters,
                    derivedClass.BaseClassTypeArguments
                );

                if (string.Equals(derivedType, resolvedBaseType, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
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
            string typeParamsStr = classMatch.Groups["typeParams"].Value;
            string baseList = classMatch.Groups["base"].Value.Trim();
            string baseClassName = null;
            string baseClassFullDeclaration = null;
            List<string> baseClassTypeArgs = new();
            List<string> implementedInterfaces = new();

            // Check if the class has [SuppressAnalyzer] attribute
            // Look at the content before the class keyword for attributes
            string contentBeforeClass = classContent.Substring(0, classMatch.Index);
            bool classSuppressed = SuppressAnalyzerAttributeRegex.IsMatch(contentBeforeClass);

            if (!string.IsNullOrEmpty(baseList))
            {
                // Remove 'where' constraint clause if present
                int whereIndex = baseList.IndexOf(" where ", StringComparison.Ordinal);
                if (whereIndex < 0)
                {
                    whereIndex = baseList.IndexOf("\twhere ", StringComparison.Ordinal);
                }

                if (whereIndex < 0)
                {
                    whereIndex = baseList.IndexOf("\nwhere ", StringComparison.Ordinal);
                }

                if (whereIndex < 0)
                {
                    whereIndex = baseList.IndexOf("\rwhere ", StringComparison.Ordinal);
                }

                if (whereIndex >= 0)
                {
                    baseList = baseList.Substring(0, whereIndex).Trim();
                }

                // Use comma-respecting split to handle generic type arguments
                List<string> baseTypes = SplitByCommaRespectingGenerics(baseList);
                foreach (string bt in baseTypes)
                {
                    string trimmed = bt.Trim();
                    if (string.IsNullOrEmpty(trimmed))
                    {
                        continue;
                    }

                    // Extract the base name without generics
                    int genericIndex = trimmed.IndexOf('<');
                    string baseName =
                        genericIndex > 0 ? trimmed.Substring(0, genericIndex).Trim() : trimmed;

                    // Check if it's an interface
                    // Handle nested interface types like "OuterClass.IInterface" or "OuterClass<T>.IInterface"
                    bool isInterface = IsInterfaceType(trimmed);

                    if (isInterface)
                    {
                        implementedInterfaces.Add(baseName);
                    }
                    else if (baseClassName == null)
                    {
                        // First non-interface is the base class
                        baseClassName = baseName;
                        baseClassFullDeclaration = trimmed;

                        // Extract generic arguments from the base class
                        if (genericIndex > 0)
                        {
                            baseClassTypeArgs = ExtractGenericArguments(trimmed);
                        }
                    }
                }
            }

            string fullName = string.IsNullOrEmpty(namespaceName)
                ? className
                : $"{namespaceName}.{className}";

            int lineNumber = CountLines(fullCode, classStartIndex);

            // Strip comments before extracting methods to avoid false positives
            string strippedContent = StripCommentsAndStrings(classContent);

            Dictionary<string, List<AnalyzerMethodInfo>> methods = ExtractMethods(
                classContent,
                strippedContent,
                fullCode,
                classStartIndex,
                classSuppressed
            );

            return new AnalyzerClassInfo
            {
                Name = className,
                FullName = fullName,
                BaseClassName = baseClassName,
                BaseClassFullDeclaration = baseClassFullDeclaration,
                BaseClassTypeArguments = baseClassTypeArgs,
                GenericTypeParameters = ExtractGenericTypeParameters(typeParamsStr),
                ImplementedInterfaces = implementedInterfaces,
                FilePath = filePath,
                Methods = methods,
                LineNumber = lineNumber,
                IsSuppressed = classSuppressed,
            };
        }

        /// <summary>
        /// Determines if a type declaration represents an interface.
        /// Handles simple interfaces (IFoo), nested interfaces (Outer.IFoo), and generic interfaces (Outer&lt;T&gt;.IFoo).
        /// </summary>
        private static bool IsInterfaceType(string typeDeclaration)
        {
            if (string.IsNullOrEmpty(typeDeclaration))
            {
                return false;
            }

            // Remove generic arguments for analysis
            string cleaned = RemoveGenericArguments(typeDeclaration);

            // Check for nested interface: look for .I followed by uppercase letter
            int dotIndex = cleaned.LastIndexOf('.');
            if (dotIndex >= 0 && dotIndex < cleaned.Length - 2)
            {
                string afterDot = cleaned.Substring(dotIndex + 1);
                if (afterDot.Length > 1 && afterDot[0] == 'I' && char.IsUpper(afterDot[1]))
                {
                    return true;
                }
            }

            // Check for simple interface: starts with I and next char is uppercase
            if (cleaned.Length > 1 && cleaned[0] == 'I' && char.IsUpper(cleaned[1]))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes generic arguments from a type string for simpler analysis.
        /// E.g., "List&lt;int&gt;" becomes "List", "Dict&lt;K,V&gt;.Inner&lt;T&gt;" becomes "Dict.Inner"
        /// </summary>
        private static string RemoveGenericArguments(string type)
        {
            if (string.IsNullOrEmpty(type))
            {
                return type;
            }

            System.Text.StringBuilder result = new();
            int depth = 0;
            foreach (char c in type)
            {
                if (c == '<')
                {
                    depth++;
                }
                else if (c == '>')
                {
                    depth--;
                }
                else if (depth == 0)
                {
                    result.Append(c);
                }
            }

            return result.ToString();
        }

        private static Dictionary<string, List<AnalyzerMethodInfo>> ExtractMethods(
            string classContent,
            string strippedContent,
            string fullCode,
            int classStartOffset,
            bool classSuppressed
        )
        {
            Dictionary<string, List<AnalyzerMethodInfo>> methods = new();

            // Use stripped content for matching to avoid false positives in comments
            MatchCollection methodMatches = MethodRegex.Matches(strippedContent);

            foreach (Match match in methodMatches)
            {
                string modifiers = match.Groups["modifiers"].Value;
                string returnType = match.Groups["return"].Value.Trim();
                string methodName = match.Groups["name"].Value;
                string paramsStr = match.Groups["params"].Value;

                // Skip control flow statements that regex might incorrectly match
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

                // Skip 'new' expressions (constructor calls) that regex might match as methods
                // Check if this looks like "new TypeName(" by examining what comes before
                if (IsNewExpression(strippedContent, match))
                {
                    continue;
                }

                // Skip if return type is 'new' (indicates this is a "new SomeType(" expression)
                if (string.Equals(returnType, "new", StringComparison.Ordinal))
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

                // Check if the method has [SuppressAnalyzer] attribute
                // Look at the original (non-stripped) content before the method for attributes
                int methodPosInClass = match.Index;
                int lookbackStart = Math.Max(0, methodPosInClass - 200);
                string contentBeforeMethod = classContent.Substring(
                    lookbackStart,
                    methodPosInClass - lookbackStart
                );
                bool methodSuppressed =
                    classSuppressed || SuppressAnalyzerAttributeRegex.IsMatch(contentBeforeMethod);

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
                    IsSuppressed = methodSuppressed,
                };

                methods.GetOrAdd(methodName).Add(methodInfo);
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
                AnalyzeClassInheritance(classInfo);
            }
        }

        /// <summary>
        /// Parallel version of inheritance analysis that processes classes concurrently.
        /// </summary>
        private void AnalyzeInheritanceParallel(CancellationToken cancellationToken)
        {
            ParallelOptions parallelOptions = new()
            {
                MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount),
                CancellationToken = cancellationToken,
            };

            Parallel.ForEach(_classes.Values, parallelOptions, AnalyzeClassInheritance);
        }

        /// <summary>
        /// Analyzes a single class for inheritance issues. Thread-safe for parallel execution.
        /// </summary>
        private void AnalyzeClassInheritance(AnalyzerClassInfo classInfo)
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

        /// <summary>
        /// Finds a base class by name using the O(1) lookup dictionary.
        /// Falls back to linear search for full name matches.
        /// </summary>
        private AnalyzerClassInfo FindBaseClass(string baseClassName)
        {
            // First, check if it's a full name match (contains namespace)
            if (_classes.TryGetValue(baseClassName, out AnalyzerClassInfo directMatch))
            {
                return directMatch;
            }

            // Use the lookup dictionary for O(1) simple name lookup
            if (_classNameLookup.TryGetValue(baseClassName, out List<AnalyzerClassInfo> candidates))
            {
                // If only one match, return it
                if (candidates.Count == 1)
                {
                    return candidates[0];
                }

                // Multiple matches - prefer exact name match over namespace suffix match
                lock (candidates)
                {
                    foreach (AnalyzerClassInfo candidate in candidates)
                    {
                        if (string.Equals(candidate.Name, baseClassName, StringComparison.Ordinal))
                        {
                            return candidate;
                        }
                    }

                    // Return first candidate if no exact match
                    return candidates.Count > 0 ? candidates[0] : null;
                }
            }

            // Fallback: Check for namespace suffix match (e.g., "Namespace.ClassName")
            string suffix = "." + baseClassName;
            foreach (AnalyzerClassInfo classInfo in _classes.Values)
            {
                if (classInfo.FullName.EndsWith(suffix, StringComparison.Ordinal))
                {
                    return classInfo;
                }
            }

            return null;
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
            // Skip analysis for suppressed classes or methods
            if (classInfo.IsSuppressed || method.IsSuppressed)
            {
                return;
            }

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
            // Skip analysis for suppressed classes or methods
            if (classInfo.IsSuppressed || method.IsSuppressed)
            {
                return;
            }

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
                    // Check parameter types match, accounting for generics
                    bool paramsMatch = ParameterTypesMatchWithGenerics(
                        method.ParameterTypes,
                        baseMethod.ParameterTypes,
                        classInfo,
                        baseClass
                    );

                    // Check return type matches, accounting for generics
                    bool returnTypeMatches = TypesAreEquivalent(
                        method.ReturnType,
                        baseMethod.ReturnType,
                        classInfo,
                        baseClass
                    );

                    if (paramsMatch && returnTypeMatches)
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
                // Use generic-aware parameter matching to find the base method
                AnalyzerMethodInfo matchingMethod = baseMethods.FirstOrDefault(bm =>
                    ParameterTypesMatchWithGenerics(
                        method.ParameterTypes,
                        bm.ParameterTypes,
                        classInfo,
                        baseClass
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
                        // Check if parameter types match when considering generics
                        bool paramsMatch = ParameterTypesMatchWithGenerics(
                            method.ParameterTypes,
                            closestMethod.ParameterTypes,
                            classInfo,
                            baseClass
                        );

                        if (!paramsMatch)
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
                }
                else if (
                    !TypesAreEquivalent(
                        method.ReturnType,
                        matchingMethod.ReturnType,
                        classInfo,
                        baseClass
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

        /// <summary>
        /// Checks if parameter type lists match, accounting for generic type resolution.
        /// </summary>
        private bool ParameterTypesMatchWithGenerics(
            IReadOnlyList<string> derivedTypes,
            IReadOnlyList<string> baseTypes,
            AnalyzerClassInfo derivedClass,
            AnalyzerClassInfo baseClass
        )
        {
            if (derivedTypes.Count != baseTypes.Count)
            {
                return false;
            }

            for (int i = 0; i < derivedTypes.Count; i++)
            {
                if (!TypesAreEquivalent(derivedTypes[i], baseTypes[i], derivedClass, baseClass))
                {
                    return false;
                }
            }

            return true;
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
