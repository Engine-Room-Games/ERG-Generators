using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using EngineRoom.Generators.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace EngineRoom.Generators.Singleton
{
    /// <summary>
    /// Source generator for [Singleton]-decorated MonoBehaviours.
    /// Injects the runtime attributes and ISingleton&lt;T&gt; interface, then for each
    /// decorated class emits a matching I&lt;ClassName&gt; interface and a partial Awake
    /// implementation that publishes the instance and (optionally) keeps it across scenes.
    /// </summary>
    [Generator(LanguageNames.CSharp)]
    public sealed class SingletonGenerator : IIncrementalGenerator
    {
        private const string AttributeFullName = "EngineRoom.SingletonAttribute";
        private const string MemberAttributeFullName = "EngineRoom.SingletonMemberAttribute";
        private const string MonoBehaviourFullName = "global::UnityEngine.MonoBehaviour";

        private const string RuntimeNamespace = "EngineRoom";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(static ctx =>
            {
                AddRuntimeType(ctx, "SingletonAttribute");
                AddRuntimeType(ctx, "SingletonMemberAttribute");
                AddRuntimeType(ctx, "ISingleton");
            });

            var singletons = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    AttributeFullName,
                    predicate: static (node, _) => node is ClassDeclarationSyntax,
                    transform: static (ctx, _) => ExtractInfo(ctx))
                .Where(static info => info is not null);

            context.RegisterSourceOutput(singletons, static (ctx, info) => Emit(ctx, info!));
        }

        private static SingletonInfo? ExtractInfo(GeneratorAttributeSyntaxContext ctx)
        {
            if (ctx.TargetSymbol is not INamedTypeSymbol classSymbol)
            {
                return null;
            }

            var classDeclaration = (ClassDeclarationSyntax)ctx.TargetNode;
            var classLocation = classDeclaration.Identifier.GetLocation();
            var className = classSymbol.Name;
            var diagnostics = new List<DiagnosticInfo>();

            if (!InheritsMonoBehaviour(classSymbol))
            {
                diagnostics.Add(new DiagnosticInfo(SingletonDiagnostics.MustBeMonoBehaviour, classLocation, className));
            }

            if (!classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                diagnostics.Add(new DiagnosticInfo(SingletonDiagnostics.MustBePartial, classLocation, className));
            }

            var existingAwake = classSymbol.GetMembers("Awake")
                .OfType<IMethodSymbol>()
                .FirstOrDefault(static method => method.MethodKind == MethodKind.Ordinary
                    && method.Parameters.Length == 0
                    && !method.IsStatic);
            if (existingAwake is not null)
            {
                var awakeLocation = existingAwake.Locations.FirstOrDefault() ?? classLocation;
                diagnostics.Add(new DiagnosticInfo(SingletonDiagnostics.MustNotDefineAwake, awakeLocation, className));
            }

            var memberDeclarations = CollectMembers(classSymbol, diagnostics, classLocation);

            var containingNamespace = classSymbol.ContainingNamespace.IsGlobalNamespace
                ? null
                : classSymbol.ContainingNamespace.ToDisplayString();
            var hintPrefix = containingNamespace is null ? className : containingNamespace + "." + className;

            return new SingletonInfo(
                className: className,
                interfaceName: "I" + className,
                @namespace: containingNamespace,
                hintPrefix: hintPrefix,
                destroyOnLoad: GetDestroyOnLoad(ctx.Attributes),
                memberDeclarations: memberDeclarations,
                diagnostics: diagnostics);
        }

        private static List<string> CollectMembers(INamedTypeSymbol classSymbol, List<DiagnosticInfo> diagnostics, Location fallbackLocation)
        {
            var declarations = new List<string>();

            foreach (var member in classSymbol.GetMembers())
            {
                var hasMemberAttribute = member.GetAttributes()
                    .Any(static attribute => attribute.AttributeClass?.ToDisplayString() == MemberAttributeFullName);
                if (!hasMemberAttribute)
                {
                    continue;
                }

                var memberLocation = member.Locations.FirstOrDefault() ?? fallbackLocation;

                if (member.IsStatic)
                {
                    diagnostics.Add(new DiagnosticInfo(SingletonDiagnostics.MemberMustBeInstance, memberLocation, member.Name));
                    continue;
                }

                if (member.DeclaredAccessibility != Accessibility.Public)
                {
                    diagnostics.Add(new DiagnosticInfo(SingletonDiagnostics.MemberMustBePublic, memberLocation, member.Name));
                    continue;
                }

                var declaration = SymbolFormatter.FormatAsInterfaceMember(member);
                if (!string.IsNullOrEmpty(declaration))
                {
                    declarations.Add(declaration);
                }
            }

            return declarations;
        }

        private static bool InheritsMonoBehaviour(INamedTypeSymbol type)
        {
            var current = type.BaseType;
            while (current is not null)
            {
                var name = current.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                if (name == MonoBehaviourFullName)
                {
                    return true;
                }

                current = current.BaseType;
            }

            return false;
        }

        private static bool GetDestroyOnLoad(ImmutableArray<AttributeData> attributes)
        {
            if (attributes.Length == 0)
            {
                return false;
            }

            var attribute = attributes[0];
            if (attribute.ConstructorArguments.Length > 0 && attribute.ConstructorArguments[0].Value is bool ctorValue)
            {
                return ctorValue;
            }

            foreach (var pair in attribute.NamedArguments)
            {
                if (pair.Key == "DestroyOnLoad" && pair.Value.Value is bool namedValue)
                {
                    return namedValue;
                }
            }

            return false;
        }

        private static void AddRuntimeType(IncrementalGeneratorPostInitializationContext ctx, string templateName)
        {
            var body = TemplateLoader.Load("Singleton/" + templateName);
            var source = SourceFileBuilder.Build(body, RuntimeNamespace);
            ctx.AddSource(templateName + ".g.cs", SourceText.From(source, Encoding.UTF8));
        }

        private static void Emit(SourceProductionContext ctx, SingletonInfo info)
        {
            foreach (var diagnostic in info.Diagnostics)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(diagnostic.Descriptor, diagnostic.Location, diagnostic.Args));
            }

            if (info.HasBlockingDiagnostic)
            {
                return;
            }

            // Member declarations land inside the interface block, so indent each
            // line to match the type body's "members live one level in" convention.
            var membersBlock = info.MemberDeclarations.Count == 0
                ? string.Empty
                : string.Join("\n", info.MemberDeclarations.Select(static line => "    " + line));

            // Substitution drops in just the call; the placeholder already sits at
            // the right indent inside the Awake body in the template.
            var dontDestroyLine = info.DestroyOnLoad
                ? string.Empty
                : "DontDestroyOnLoad(gameObject);";

            var interfaceBody = TemplateLoader.LoadAndSubstitute("Singleton/SingletonInterface", new Dictionary<string, string>
            {
                ["InterfaceName"] = info.InterfaceName,
                ["Members"] = membersBlock,
            });

            var partialBody = TemplateLoader.LoadAndSubstitute("Singleton/SingletonPartial", new Dictionary<string, string>
            {
                ["ClassName"] = info.ClassName,
                ["InterfaceName"] = info.InterfaceName,
                ["DontDestroyLine"] = dontDestroyLine,
            });

            var interfaceText = SourceFileBuilder.Build(interfaceBody, info.Namespace);
            var partialText = SourceFileBuilder.Build(partialBody, info.Namespace);

            ctx.AddSource(info.HintPrefix + ".Singleton.Interface.g.cs", SourceText.From(interfaceText, Encoding.UTF8));
            ctx.AddSource(info.HintPrefix + ".Singleton.Partial.g.cs", SourceText.From(partialText, Encoding.UTF8));
        }
    }
}
