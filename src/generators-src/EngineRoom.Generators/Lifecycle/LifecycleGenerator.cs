using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using EngineRoom.Generators.Dependency;
using EngineRoom.Generators.Helpers;
using EngineRoom.Generators.Singleton;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace EngineRoom.Generators.Lifecycle
{
    // Owns dispatching for Unity's lifecycle methods (Awake, Start, OnEnable,
    // OnDisable, Update, FixedUpdate, LateUpdate, OnDestroy). Methods marked
    // with the matching [Awake]/[Start]/... attribute are invoked in source
    // order, or by ascending Order when explicitly specified.
    //
    // Special cases handled inline (not via the attribute path, because sibling
    // generators don't see this one's syntax tree):
    //   - [Singleton] classes: Awake() calls SingletonAwake() first and aborts
    //     on a 'false' return (duplicate instance — gameObject was Destroyed).
    //   - Classes with any [Dependency] field: Start() calls DependencyStart()
    //     before any user [Start] methods.
    //
    // Validation lives in LifecycleAnalyzer; this generator silently skips any
    // input the analyzer would flag so its output never piles on top of a
    // compile error.
    [Generator(LanguageNames.CSharp)]
    public sealed class LifecycleGenerator : IIncrementalGenerator
    {
        // Attribute simple names (with and without the trailing 'Attribute' suffix)
        // used to cheaply pre-filter class candidates at the syntax level — the
        // expensive semantic checks then run only on the survivors.
        private static readonly HashSet<string> RelevantAttributeNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "Awake", "AwakeAttribute",
            "Start", "StartAttribute",
            "OnEnable", "OnEnableAttribute",
            "OnDisable", "OnDisableAttribute",
            "Update", "UpdateAttribute",
            "FixedUpdate", "FixedUpdateAttribute",
            "LateUpdate", "LateUpdateAttribute",
            "OnDestroy", "OnDestroyAttribute",
            "Singleton", "SingletonAttribute",
            "Dependency", "DependencyAttribute",
        };

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var infos = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (node, _) => IsCandidateClass(node),
                    transform: static (ctx, _) => ExtractInfo(ctx))
                .Where(static info => info is not null);

            context.RegisterSourceOutput(infos, static (ctx, info) => Emit(ctx, info!));
        }

        private static bool IsCandidateClass(SyntaxNode node)
        {
            if (node is not ClassDeclarationSyntax classDecl)
            {
                return false;
            }

            if (HasRelevantAttribute(classDecl.AttributeLists))
            {
                return true;
            }

            foreach (var member in classDecl.Members)
            {
                if (HasRelevantAttribute(member.AttributeLists))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasRelevantAttribute(SyntaxList<AttributeListSyntax> lists)
        {
            foreach (var list in lists)
            {
                foreach (var attribute in list.Attributes)
                {
                    var simpleName = GetSimpleName(attribute.Name);
                    if (simpleName is not null && RelevantAttributeNames.Contains(simpleName))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static string? GetSimpleName(NameSyntax name)
        {
            return name switch
            {
                IdentifierNameSyntax identifier => identifier.Identifier.Text,
                QualifiedNameSyntax qualified => qualified.Right.Identifier.Text,
                AliasQualifiedNameSyntax aliased => aliased.Name.Identifier.Text,
                _ => null,
            };
        }

        private static LifecycleInfo? ExtractInfo(GeneratorSyntaxContext ctx)
        {
            var classDecl = (ClassDeclarationSyntax)ctx.Node;
            if (!classDecl.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                return null;
            }

            if (ctx.SemanticModel.GetDeclaredSymbol(classDecl) is not INamedTypeSymbol classSymbol)
            {
                return null;
            }

            if (!SymbolInspector.InheritsFrom(classSymbol, LifecycleConstants.MonoBehaviourFullyQualifiedName))
            {
                return null;
            }

            // A partial class hits this transform once per declaration; pick the
            // first declaration (by source position) so the dispatcher is emitted
            // exactly once even when the user splits the class across files.
            if (!IsPrimaryDeclaration(classSymbol, classDecl))
            {
                return null;
            }

            var hasSingleton = SymbolInspector.HasAttribute(classSymbol, SingletonConstants.AttributeFullName);
            var hasDependencyField = false;

            var bucketBuilders = new ImmutableArray<LifecycleEntry>.Builder[LifecycleKinds.Count];
            for (int i = 0; i < LifecycleKinds.Count; i++)
            {
                bucketBuilders[i] = ImmutableArray.CreateBuilder<LifecycleEntry>();
            }

            foreach (var member in classSymbol.GetMembers())
            {
                if (!hasDependencyField
                    && member is IFieldSymbol field
                    && SymbolInspector.HasAttribute(field, DependencyConstants.AttributeFullName))
                {
                    hasDependencyField = true;
                }

                if (member is not IMethodSymbol method
                    || method.MethodKind != MethodKind.Ordinary
                    || method.IsStatic
                    || method.Parameters.Length != 0
                    || !method.ReturnsVoid)
                {
                    continue;
                }

                foreach (var attribute in method.GetAttributes())
                {
                    var attributeName = attribute.AttributeClass?.ToDisplayString();
                    if (attributeName is null)
                    {
                        continue;
                    }

                    for (int k = 0; k < LifecycleKinds.Count; k++)
                    {
                        var kind = (LifecycleKind)k;
                        if (attributeName != LifecycleKinds.AttributeFullName(kind))
                        {
                            continue;
                        }

                        var (order, hasExplicitOrder) = ReadOrder(attribute);
                        var declarationPosition = method.DeclaringSyntaxReferences.Length > 0
                            ? method.DeclaringSyntaxReferences[0].Span.Start
                            : 0;

                        bucketBuilders[k].Add(new LifecycleEntry(
                            methodName: method.Name,
                            order: order,
                            hasExplicitOrder: hasExplicitOrder,
                            declarationPosition: declarationPosition));
                    }
                }
            }

            var buckets = ImmutableArray.CreateBuilder<ImmutableArray<LifecycleEntry>>(LifecycleKinds.Count);
            for (int i = 0; i < LifecycleKinds.Count; i++)
            {
                buckets.Add(bucketBuilders[i].ToImmutable().Sort(static (left, right) =>
                {
                    int compare = left.Order.CompareTo(right.Order);
                    return compare != 0 ? compare : left.DeclarationPosition.CompareTo(right.DeclarationPosition);
                }));
            }

            var containingNamespace = classSymbol.ContainingNamespace.IsGlobalNamespace
                ? null
                : classSymbol.ContainingNamespace.ToDisplayString();
            var hintPrefix = containingNamespace is null
                ? classSymbol.Name
                : containingNamespace + "." + classSymbol.Name;

            var info = new LifecycleInfo(
                className: classSymbol.Name,
                @namespace: containingNamespace,
                hintPrefix: hintPrefix,
                hasSingleton: hasSingleton,
                hasDependencyField: hasDependencyField,
                entriesByKind: buckets.ToImmutable());

            return info.HasAnyEntries ? info : null;
        }

        private static (int Order, bool HasExplicitOrder) ReadOrder(AttributeData attribute)
        {
            int order = 0;
            bool hasExplicitOrder = false;

            // Ctor `(int order)` overload — the parameterless ctor leaves the
            // ConstructorArguments collection empty so we can distinguish
            // "unspecified" from "explicit 0".
            if (attribute.AttributeConstructor is { Parameters.Length: 1 }
                && attribute.ConstructorArguments.Length == 1
                && attribute.ConstructorArguments[0].Value is int parsed)
            {
                order = parsed;
                hasExplicitOrder = true;
            }

            foreach (var named in attribute.NamedArguments)
            {
                if (named.Key == "Order" && named.Value.Value is int namedOrder)
                {
                    order = namedOrder;
                    hasExplicitOrder = true;
                }
            }

            return (order, hasExplicitOrder);
        }

        private static bool IsPrimaryDeclaration(INamedTypeSymbol classSymbol, ClassDeclarationSyntax candidate)
        {
            if (classSymbol.DeclaringSyntaxReferences.Length == 0)
            {
                return false;
            }

            // SyntaxNode identity isn't stable across compilations; compare by
            // tree+span instead.
            var first = classSymbol.DeclaringSyntaxReferences[0];
            return first.SyntaxTree == candidate.SyntaxTree && first.Span == candidate.Span;
        }

        private static void Emit(SourceProductionContext ctx, LifecycleInfo info)
        {
            var methodsBlock = new StringBuilder();
            bool first = true;

            for (int k = 0; k < LifecycleKinds.Count; k++)
            {
                var kind = (LifecycleKind)k;
                var entries = info.EntriesByKind[k];
                var includeSingleton = kind == LifecycleKind.Awake && info.HasSingleton;
                var includeDependency = kind == LifecycleKind.Start && info.HasDependencyField;
                var backCompat = LifecycleKinds.BackCompatPartialName(kind);

                if (entries.Length == 0 && !includeSingleton && !includeDependency)
                {
                    continue;
                }

                if (!first)
                {
                    methodsBlock.Append('\n');
                }
                first = false;

                AppendDispatcher(methodsBlock, LifecycleKinds.MethodName(kind), entries, includeSingleton, includeDependency, backCompat);
            }

            var body = TemplateLoader.LoadAndSubstitute("Lifecycle/LifecyclePartial", new Dictionary<string, string>
            {
                ["ClassName"] = info.ClassName,
                ["Methods"] = methodsBlock.ToString().TrimEnd('\n'),
            });

            var text = SourceFileBuilder.Build(body, info.Namespace);
            ctx.AddSource(info.HintPrefix + ".Lifecycle.Partial.g.cs", SourceText.From(text, Encoding.UTF8));
        }

        private static void AppendDispatcher(
            StringBuilder builder,
            string methodName,
            ImmutableArray<LifecycleEntry> entries,
            bool includeSingleton,
            bool includeDependency,
            string? backCompatPartialName)
        {
            builder.Append("private void ").Append(methodName).Append("()\n");
            builder.Append("{\n");

            if (includeSingleton)
            {
                // Short-circuit on duplicate: the SingletonAwake helper has
                // already called Destroy(gameObject); calling any further user
                // [Awake] methods on a doomed object would be a bug.
                builder.Append("    if (!").Append(LifecycleConstants.SingletonAwakeHelperName).Append("())\n");
                builder.Append("    {\n");
                builder.Append("        return;\n");
                builder.Append("    }\n");
            }
            else if (includeDependency)
            {
                builder.Append("    ").Append(LifecycleConstants.DependencyStartHelperName).Append("();\n");
            }

            foreach (var entry in entries)
            {
                builder.Append("    ").Append(entry.MethodName).Append("();\n");
            }

            if (backCompatPartialName is not null)
            {
                builder.Append("    ").Append(backCompatPartialName).Append("();\n");
            }

            builder.Append("}\n");

            if (backCompatPartialName is not null)
            {
                builder.Append('\n');
                builder.Append("partial void ").Append(backCompatPartialName).Append("();\n");
            }
        }
    }
}
