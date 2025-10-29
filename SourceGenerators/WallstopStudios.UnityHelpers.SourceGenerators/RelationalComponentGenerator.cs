namespace WallstopStudios.UnityHelpers.SourceGenerators
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Text;

    [Generator]
    public sealed class RelationalComponentGenerator : IIncrementalGenerator
    {
        private const string SiblingAttributeMetadataName =
            "WallstopStudios.UnityHelpers.Core.Attributes.SiblingComponentAttribute";
        private const string ParentAttributeMetadataName =
            "WallstopStudios.UnityHelpers.Core.Attributes.ParentComponentAttribute";
        private const string ChildAttributeMetadataName =
            "WallstopStudios.UnityHelpers.Core.Attributes.ChildComponentAttribute";
        private const string PreferencePropertyName = "CodeGenPreference";
        private const string OptionalPropertyName = "Optional";
        private const string SkipIfAssignedPropertyName = "SkipIfAssigned";
        private const string IncludeInactivePropertyName = "IncludeInactive";
        private const string TagFilterPropertyName = "TagFilter";
        private const string NameFilterPropertyName = "NameFilter";
        private const string MaxCountPropertyName = "MaxCount";

        private const int PreferenceDisabledValue = 1;
        private const int PreferenceEnabledValue = 2;

        private static readonly DiagnosticDescriptor PartialTypeRequired = new DiagnosticDescriptor(
            id: "WHCG001",
            title: "Relational code generation requires partial type",
            messageFormat: "Type '{0}' must be declared partial to enable relational code generation",
            category: "RelationalCodeGen",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true
        );

        private static readonly DiagnosticDescriptor UnsupportedField = new DiagnosticDescriptor(
            id: "WHCG002",
            title: "Relational code generation fallback",
            messageFormat: "Field '{0}' cannot be code-generated: {1}",
            category: "RelationalCodeGen",
            DiagnosticSeverity.Info,
            isEnabledByDefault: true
        );

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<FieldGenerationInfo> fieldInfos = context
                .SyntaxProvider.CreateSyntaxProvider(
                    static (node, _) => IsCandidate(node),
                    static (syntaxContext, ct) => CollectFieldInfos(syntaxContext, ct)
                )
                .SelectMany(static (infos, _) => infos);

            IncrementalValueProvider<ImmutableArray<FieldGenerationInfo>> collectedInfos =
                fieldInfos.Collect();

            IncrementalValueProvider<GlobalPreferences> defaultsProvider = context
                .AdditionalTextsProvider.Where(static text =>
                    string.Equals(
                        Path.GetFileName(text.Path),
                        "relational_codegen_defaults.json",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                .Select(static (text, ct) => ParseDefaults(text, ct))
                .Collect()
                .Select<ImmutableArray<ImmutableArray<GlobalPreferences>>, GlobalPreferences>(
                    (defaults, _) =>
                        defaults.IsDefaultOrEmpty ? GlobalPreferences.Disabled
                        : defaults[0].IsDefaultOrEmpty ? GlobalPreferences.Disabled
                        : defaults[0][0]
                );

            context.RegisterSourceOutput(
                collectedInfos.Combine(defaultsProvider),
                static (productionContext, tuple) =>
                    GenerateSources(productionContext, tuple.Left, tuple.Right)
            );
        }

        private static bool IsCandidate(SyntaxNode node)
        {
            return node is FieldDeclarationSyntax { AttributeLists.Count: > 0 };
        }

        private static ImmutableArray<FieldGenerationInfo> CollectFieldInfos(
            GeneratorSyntaxContext context,
            CancellationToken cancellationToken
        )
        {
            var fieldDeclaration = (FieldDeclarationSyntax)context.Node;
            SemanticModel semanticModel = context.SemanticModel;
            Compilation compilation = semanticModel.Compilation;

            INamedTypeSymbol? siblingAttribute = compilation.GetTypeByMetadataName(
                SiblingAttributeMetadataName
            );
            INamedTypeSymbol? parentAttribute = compilation.GetTypeByMetadataName(
                ParentAttributeMetadataName
            );
            INamedTypeSymbol? childAttribute = compilation.GetTypeByMetadataName(
                ChildAttributeMetadataName
            );
            INamedTypeSymbol? componentSymbol = compilation.GetTypeByMetadataName(
                "UnityEngine.Component"
            );

            var builder = ImmutableArray.CreateBuilder<FieldGenerationInfo>();

            foreach (VariableDeclaratorSyntax variable in fieldDeclaration.Declaration.Variables)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (
                    semanticModel.GetDeclaredSymbol(variable, cancellationToken)
                    is not IFieldSymbol fieldSymbol
                )
                {
                    continue;
                }

                if (fieldSymbol.IsStatic)
                {
                    continue;
                }

                foreach (AttributeData attribute in fieldSymbol.GetAttributes())
                {
                    INamedTypeSymbol? attributeClass = attribute.AttributeClass;
                    if (attributeClass == null)
                    {
                        continue;
                    }

                    RelationalAttributeKind kind;
                    if (SymbolEqualityComparer.Default.Equals(attributeClass, siblingAttribute))
                    {
                        kind = RelationalAttributeKind.Sibling;
                    }
                    else if (SymbolEqualityComparer.Default.Equals(attributeClass, parentAttribute))
                    {
                        kind = RelationalAttributeKind.Parent;
                    }
                    else if (SymbolEqualityComparer.Default.Equals(attributeClass, childAttribute))
                    {
                        kind = RelationalAttributeKind.Child;
                    }
                    else
                    {
                        continue;
                    }

                    int preference = GetPreference(attribute);
                    if (preference == PreferenceDisabledValue)
                    {
                        continue;
                    }

                    // Currently we only generate for explicitly enabled attributes; inherited defaults are treated as disabled.
                    if (preference != PreferenceEnabledValue)
                    {
                        continue;
                    }

                    bool isPartial = IsPartial(fieldSymbol.ContainingType, cancellationToken);
                    if (!isPartial)
                    {
                        builder.Add(
                            FieldGenerationInfo.CreateUnsupported(
                                fieldSymbol,
                                fieldDeclaration,
                                kind,
                                "containing type must be partial"
                            )
                        );
                        continue;
                    }

                    if (componentSymbol == null)
                    {
                        builder.Add(
                            FieldGenerationInfo.CreateUnsupported(
                                fieldSymbol,
                                fieldDeclaration,
                                kind,
                                "UnityEngine.Component type not found"
                            )
                        );
                        continue;
                    }

                    bool optional = GetOptional(attribute);
                    bool skipIfAssigned = GetSkipIfAssigned(attribute);
                    bool includeInactive = GetBooleanProperty(
                        attribute,
                        IncludeInactivePropertyName,
                        defaultValue: true
                    );
                    string? tagFilter = GetStringProperty(attribute, TagFilterPropertyName);
                    string? nameFilter = GetStringProperty(attribute, NameFilterPropertyName);
                    int maxCount = GetIntProperty(attribute, MaxCountPropertyName, defaultValue: 0);

                    if (!IsSupportedSingleSibling(fieldSymbol, kind, componentSymbol))
                    {
                        builder.Add(
                            FieldGenerationInfo.CreateUnsupported(
                                fieldSymbol,
                                fieldDeclaration,
                                kind,
                                "field type is not a supported Component single-field scenario"
                            )
                        );
                        continue;
                    }

                    if (!includeInactive)
                    {
                        builder.Add(
                            FieldGenerationInfo.CreateUnsupported(
                                fieldSymbol,
                                fieldDeclaration,
                                kind,
                                "IncludeInactive = false is not supported"
                            )
                        );
                        continue;
                    }

                    if (!string.IsNullOrEmpty(tagFilter))
                    {
                        builder.Add(
                            FieldGenerationInfo.CreateUnsupported(
                                fieldSymbol,
                                fieldDeclaration,
                                kind,
                                "TagFilter requires runtime filtering"
                            )
                        );
                        continue;
                    }

                    if (!string.IsNullOrEmpty(nameFilter))
                    {
                        builder.Add(
                            FieldGenerationInfo.CreateUnsupported(
                                fieldSymbol,
                                fieldDeclaration,
                                kind,
                                "NameFilter requires runtime filtering"
                            )
                        );
                        continue;
                    }

                    if (maxCount > 0)
                    {
                        builder.Add(
                            FieldGenerationInfo.CreateUnsupported(
                                fieldSymbol,
                                fieldDeclaration,
                                kind,
                                "MaxCount > 0 is not supported"
                            )
                        );
                        continue;
                    }

                    builder.Add(
                        FieldGenerationInfo.CreateSupported(
                            fieldSymbol,
                            fieldDeclaration,
                            kind,
                            optional,
                            skipIfAssigned
                        )
                    );
                }
            }

            return builder.ToImmutable();
        }

        private static bool IsSupportedSingleSibling(
            IFieldSymbol fieldSymbol,
            RelationalAttributeKind kind,
            INamedTypeSymbol componentSymbol
        )
        {
            if (kind != RelationalAttributeKind.Sibling)
            {
                return false;
            }

            if (!IsComponentType(fieldSymbol.Type, componentSymbol))
            {
                return false;
            }

            if (fieldSymbol.Type is IArrayTypeSymbol or INamedTypeSymbol { Name: "List" })
            {
                return false;
            }

            return true;
        }

        private static bool IsComponentType(ITypeSymbol type, INamedTypeSymbol componentSymbol)
        {
            if (SymbolEqualityComparer.Default.Equals(type, componentSymbol))
            {
                return true;
            }

            ITypeSymbol? current = type;
            while (current != null)
            {
                if (SymbolEqualityComparer.Default.Equals(current, componentSymbol))
                {
                    return true;
                }

                current = current.BaseType;
            }

            return false;
        }

        private static int GetPreference(AttributeData attributeData)
        {
            foreach (KeyValuePair<string, TypedConstant> argument in attributeData.NamedArguments)
            {
                if (argument.Key == PreferencePropertyName)
                {
                    if (argument.Value.Value is int intValue)
                    {
                        return intValue;
                    }
                }
            }

            return 0; // Inherit
        }

        private static bool GetOptional(AttributeData attributeData)
        {
            if (
                TryGetNamedArgument(attributeData, OptionalPropertyName, out TypedConstant constant)
                && constant.Value is bool optional
            )
            {
                return optional;
            }

            return false;
        }

        private static bool GetSkipIfAssigned(AttributeData attributeData)
        {
            if (
                TryGetNamedArgument(
                    attributeData,
                    SkipIfAssignedPropertyName,
                    out TypedConstant constant
                ) && constant.Value is bool skip
            )
            {
                return skip;
            }

            return false;
        }

        private static bool GetBooleanProperty(
            AttributeData attributeData,
            string propertyName,
            bool defaultValue
        )
        {
            if (
                TryGetNamedArgument(attributeData, propertyName, out TypedConstant constant)
                && constant.Value is bool value
            )
            {
                return value;
            }

            return defaultValue;
        }

        private static int GetIntProperty(
            AttributeData attributeData,
            string propertyName,
            int defaultValue
        )
        {
            foreach (KeyValuePair<string, TypedConstant> argument in attributeData.NamedArguments)
            {
                if (argument.Key == propertyName && argument.Value.Value is int value)
                {
                    return value;
                }
            }

            return defaultValue;
        }

        private static string? GetStringProperty(AttributeData attributeData, string propertyName)
        {
            if (
                TryGetNamedArgument(attributeData, propertyName, out TypedConstant constant)
                && !constant.IsNull
            )
            {
                return constant.Value as string;
            }

            return null;
        }

        private static bool TryGetNamedArgument(
            AttributeData attributeData,
            string propertyName,
            out TypedConstant value
        )
        {
            foreach (KeyValuePair<string, TypedConstant> argument in attributeData.NamedArguments)
            {
                if (argument.Key == propertyName)
                {
                    value = argument.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

        private static bool IsPartial(INamedTypeSymbol type, CancellationToken cancellationToken)
        {
            foreach (SyntaxReference syntaxRef in type.DeclaringSyntaxReferences)
            {
                if (syntaxRef.GetSyntax(cancellationToken) is TypeDeclarationSyntax typeSyntax)
                {
                    if (
                        typeSyntax.Modifiers.Any(static token =>
                            token.IsKind(SyntaxKind.PartialKeyword)
                        )
                    )
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void GenerateSources(
            SourceProductionContext context,
            ImmutableArray<FieldGenerationInfo> infos,
            GlobalPreferences defaults
        )
        {
            if (infos.IsDefaultOrEmpty)
            {
                return;
            }

            var groups = infos.GroupBy<FieldGenerationInfo, INamedTypeSymbol?>(
                static info => info.FieldSymbol.ContainingType,
                SymbolEqualityComparer.Default
            );

            foreach (IGrouping<INamedTypeSymbol?, FieldGenerationInfo> group in groups)
            {
                if (group.Key is not INamedTypeSymbol containingType)
                {
                    continue;
                }

                if (group.Any(static info => !info.IsSupported))
                {
                    foreach (
                        FieldGenerationInfo info in group.Where(static info => !info.IsSupported)
                    )
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                info.FailureReason == "containing type must be partial"
                                    ? PartialTypeRequired
                                    : UnsupportedField,
                                info.Location,
                                containingType.ToDisplayString(),
                                info.FailureReason ?? string.Empty
                            )
                        );
                    }

                    continue;
                }

                List<FieldGenerationInfo> supportedFields = group
                    .Where(static info => info.IsSupported)
                    .ToList();
                if (supportedFields.Count == 0)
                {
                    continue;
                }

                string hintName = GetHintName(containingType);
                string source = BuildSource(containingType, supportedFields);
                context.AddSource(hintName, SourceText.From(source, Encoding.UTF8));
            }
        }

        private static string GetHintName(INamedTypeSymbol typeSymbol)
        {
            string name = typeSymbol
                .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                .Replace('<', '_')
                .Replace('>', '_')
                .Replace('.', '_');
            return $"{name}_RelationalCodeGen.g.cs";
        }

        private static string BuildSource(
            INamedTypeSymbol containingType,
            List<FieldGenerationInfo> fields
        )
        {
            string? namespaceName = containingType.ContainingNamespace
                is { IsGlobalNamespace: false }
                ? containingType.ContainingNamespace.ToDisplayString()
                : null;

            string typeName = containingType.Name;
            if (containingType.TypeArguments.Length > 0)
            {
                typeName = containingType.ToDisplayString(
                    SymbolDisplayFormat.MinimallyQualifiedFormat
                );
            }

            var builder = new StringBuilder();
            string siblingFieldNames = string.Join(
                ", ",
                fields.Select(field => $"\"{field.FieldSymbol.Name}\"")
            );
            builder.AppendLine("// <auto-generated/>");
            builder.AppendLine("#nullable enable");
            builder.AppendLine("using UnityEngine;");
            builder.AppendLine("using WallstopStudios.UnityHelpers.Core.CodeGen;");
            builder.AppendLine("using WallstopStudios.UnityHelpers.Core.Extension;");
            builder.AppendLine("#if UNITY_EDITOR");
            builder.AppendLine("using UnityEditor;");
            builder.AppendLine("#endif");
            builder.AppendLine();

            if (namespaceName != null)
            {
                builder.Append("namespace ").Append(namespaceName).AppendLine().AppendLine("{");
            }

            if (containingType.DeclaredAccessibility != Accessibility.Public)
            {
                builder.Append(
                    containingType.DeclaredAccessibility switch
                    {
                        Accessibility.Internal => "    internal ",
                        Accessibility.Protected => "    protected ",
                        Accessibility.ProtectedOrInternal => "    protected internal ",
                        Accessibility.Private => "    private ",
                        _ => "    ",
                    }
                );
            }
            else
            {
                builder.Append("    public ");
            }

            builder
                .Append("partial class ")
                .Append(containingType.Name)
                .AppendLine()
                .AppendLine("    {")
                .AppendLine("        private static bool __relationalCodeGenRegistered;")
                .AppendLine()
                .AppendLine("        [UnityEngine.Scripting.Preserve]")
                .AppendLine("        private static void __RegisterRelationalCodeGen()")
                .AppendLine("        {")
                .AppendLine("            if (__relationalCodeGenRegistered)")
                .AppendLine("            {")
                .AppendLine("                return;")
                .AppendLine("            }")
                .AppendLine("            __relationalCodeGenRegistered = true;")
                .AppendLine("            RelationalCodeGenRegistry.Register(")
                .AppendLine("                typeof(")
                .Append(containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                .AppendLine("),")
                .AppendLine("                new RelationalGeneratedHandlers(")
                .AppendLine("                    sibling: __AssignSiblingComponentsGenerated,")
                .AppendLine("                    parent: null,")
                .AppendLine("                    child: null,")
                .Append("                    siblingFields: new string[] { ")
                .Append(siblingFieldNames)
                .AppendLine(" },")
                .AppendLine("                    parentFields: null,")
                .AppendLine("                    childFields: null));")
                .AppendLine("        }")
                .AppendLine()
                .AppendLine("#if UNITY_EDITOR")
                .AppendLine("        [InitializeOnLoadMethod]")
                .AppendLine("        private static void __RelationalCodeGenInitEditor()")
                .AppendLine("        {")
                .AppendLine("            __RegisterRelationalCodeGen();")
                .AppendLine("        }")
                .AppendLine()
                .AppendLine("#endif")
                .AppendLine(
                    "        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]"
                )
                .AppendLine("        private static void __RelationalCodeGenInitRuntime()")
                .AppendLine("        {")
                .AppendLine("            __RegisterRelationalCodeGen();")
                .AppendLine("        }")
                .AppendLine()
                .AppendLine(
                    "        private static bool __AssignSiblingComponentsGenerated(Component component)"
                )
                .AppendLine("        {")
                .AppendLine("            if (component is not ")
                .Append(containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                .AppendLine(" instance)")
                .AppendLine("            {")
                .AppendLine("                return false;")
                .AppendLine("            }")
                .AppendLine()
                .AppendLine("            bool success = true;");

            for (int i = 0; i < fields.Count; i++)
            {
                FieldGenerationInfo info = fields[i];
                string fieldTypeName = info.FieldSymbol.Type.ToDisplayString(
                    SymbolDisplayFormat.FullyQualifiedFormat
                );
                string fieldDisplayName = TrimGlobalNamespace(fieldTypeName);
                string fieldName = info.FieldSymbol.Name;

                if (info.SkipIfAssigned)
                {
                    builder
                        .Append("            if (instance.")
                        .Append(fieldName)
                        .AppendLine(" == null)")
                        .AppendLine("            {")
                        .Append("                ")
                        .Append(fieldTypeName)
                        .Append(" value_")
                        .Append(i)
                        .Append(" = instance.GetComponent<")
                        .Append(fieldTypeName)
                        .AppendLine(">();")
                        .Append("                if (value_")
                        .Append(i)
                        .AppendLine(" != null)")
                        .AppendLine("                {")
                        .Append("                    instance.")
                        .Append(fieldName)
                        .AppendLine(" = value_")
                        .Append(i)
                        .AppendLine(";")
                        .AppendLine("                }")
                        .AppendLine("                else")
                        .AppendLine("                {")
                        .Append("                    instance.")
                        .Append(fieldName)
                        .AppendLine(" = null;");

                    if (!info.Optional)
                    {
                        builder.AppendLine("                    success = false;");
                        builder
                            .Append(
                                "                    component.LogError(\"Unable to find sibling component of type "
                            )
                            .Append(fieldDisplayName)
                            .Append(" for field '")
                            .Append(fieldName)
                            .AppendLine("'\");");
                    }

                    builder
                        .AppendLine("                }")
                        .AppendLine("            }")
                        .AppendLine();
                }
                else
                {
                    builder
                        .Append("            ")
                        .Append(fieldTypeName)
                        .Append(" value_")
                        .Append(i)
                        .Append(" = instance.GetComponent<")
                        .Append(fieldTypeName)
                        .AppendLine(">();")
                        .Append("            if (value_")
                        .Append(i)
                        .AppendLine(" != null)")
                        .AppendLine("            {")
                        .Append("                instance.")
                        .Append(fieldName)
                        .AppendLine(" = value_")
                        .Append(i)
                        .AppendLine(";")
                        .AppendLine("            }")
                        .AppendLine("            else")
                        .AppendLine("            {")
                        .Append("                instance.")
                        .Append(fieldName)
                        .AppendLine(" = null;");

                    if (!info.Optional)
                    {
                        builder.AppendLine("                success = false;");
                        builder
                            .Append(
                                "                component.LogError(\"Unable to find sibling component of type "
                            )
                            .Append(fieldDisplayName)
                            .Append(" for field '")
                            .Append(fieldName)
                            .AppendLine("'\");");
                    }

                    builder.AppendLine("            }").AppendLine();
                }
            }

            builder
                .AppendLine("            return success;")
                .AppendLine("        }")
                .AppendLine("    }");

            if (namespaceName != null)
            {
                builder.AppendLine("}");
            }

            return builder.ToString();
        }

        private static ImmutableArray<GlobalPreferences> ParseDefaults(
            AdditionalText text,
            CancellationToken cancellationToken
        )
        {
            SourceText? sourceText = text.GetText(cancellationToken);
            if (sourceText == null)
            {
                return ImmutableArray<GlobalPreferences>.Empty;
            }

            string json = sourceText.ToString();
            if (string.IsNullOrWhiteSpace(json))
            {
                return ImmutableArray<GlobalPreferences>.Empty;
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(json);
                JsonElement root = document.RootElement;

                RelationalCodeGenPreference sibling = ReadPreference(root, "sibling");
                RelationalCodeGenPreference parent = ReadPreference(root, "parent");
                RelationalCodeGenPreference child = ReadPreference(root, "child");

                return ImmutableArray.Create(new GlobalPreferences(sibling, parent, child));
            }
            catch (JsonException)
            {
                return ImmutableArray<GlobalPreferences>.Empty;
            }
        }

        private static RelationalCodeGenPreference ReadPreference(
            JsonElement root,
            string propertyName
        )
        {
            if (root.TryGetProperty(propertyName, out JsonElement element))
            {
                string? value = element.GetString();
                if (
                    !string.IsNullOrWhiteSpace(value)
                    && Enum.TryParse(
                        value,
                        ignoreCase: true,
                        out RelationalCodeGenPreference preference
                    )
                )
                {
                    return preference;
                }
            }

            return RelationalCodeGenPreference.Disabled;
        }

        private readonly struct GlobalPreferences(
            RelationalComponentGenerator.RelationalCodeGenPreference sibling,
            RelationalComponentGenerator.RelationalCodeGenPreference parent,
            RelationalComponentGenerator.RelationalCodeGenPreference child
        )
        {
            public static readonly GlobalPreferences Disabled = new GlobalPreferences(
                RelationalCodeGenPreference.Disabled,
                RelationalCodeGenPreference.Disabled,
                RelationalCodeGenPreference.Disabled
            );

            public RelationalCodeGenPreference Sibling { get; } = sibling;

            public RelationalCodeGenPreference Parent { get; } = parent;

            public RelationalCodeGenPreference Child { get; } = child;
        }

        private enum RelationalCodeGenPreference : byte
        {
            Inherit = 0,
            Disabled = 1,
            Enabled = 2,
        }

        private static string TrimGlobalNamespace(string typeName)
        {
            const string GlobalPrefix = "global::";
            if (typeName.StartsWith(GlobalPrefix, StringComparison.Ordinal))
            {
                return typeName.Substring(GlobalPrefix.Length);
            }

            return typeName;
        }

        private sealed class FieldGenerationInfo(
            IFieldSymbol fieldSymbol,
            FieldDeclarationSyntax fieldSyntax,
            RelationalComponentGenerator.RelationalAttributeKind kind,
            bool optional,
            bool skipIfAssigned,
            bool isSupported,
            string? failureReason
        )
        {
            public IFieldSymbol FieldSymbol { get; } = fieldSymbol;

            public FieldDeclarationSyntax FieldSyntax { get; } = fieldSyntax;

            public RelationalAttributeKind Kind { get; } = kind;

            public bool Optional { get; } = optional;

            public bool SkipIfAssigned { get; } = skipIfAssigned;

            public bool IsSupported { get; } = isSupported;

            public string? FailureReason { get; } = failureReason;

            public static FieldGenerationInfo CreateSupported(
                IFieldSymbol fieldSymbol,
                FieldDeclarationSyntax syntax,
                RelationalAttributeKind kind,
                bool optional,
                bool skipIfAssigned
            )
            {
                return new FieldGenerationInfo(
                    fieldSymbol,
                    syntax,
                    kind,
                    optional,
                    skipIfAssigned,
                    true,
                    null
                );
            }

            public static FieldGenerationInfo CreateUnsupported(
                IFieldSymbol fieldSymbol,
                FieldDeclarationSyntax syntax,
                RelationalAttributeKind kind,
                string reason
            )
            {
                return new FieldGenerationInfo(
                    fieldSymbol,
                    syntax,
                    kind,
                    optional: false,
                    skipIfAssigned: false,
                    isSupported: false,
                    failureReason: reason
                );
            }

            public Location Location => FieldSyntax.GetLocation();
        }

        private enum RelationalAttributeKind
        {
            Sibling,
            Parent,
            Child,
        }
    }
}
