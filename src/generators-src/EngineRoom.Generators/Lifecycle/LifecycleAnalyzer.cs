using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using EngineRoom.Generators.Dependency;
using EngineRoom.Generators.Helpers;
using EngineRoom.Generators.Singleton;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EngineRoom.Generators.Lifecycle
{
    // Owns the ERG02xx range. LifecycleGenerator silently skips invalid input,
    // so every user-facing error/warning surfaces from here.
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LifecycleAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            LifecycleDiagnostics.MustBeMonoBehaviour,
            LifecycleDiagnostics.MustBePartial,
            LifecycleDiagnostics.MustNotDefineLifecycleMethod,
            LifecycleDiagnostics.MethodMustBeInstanceVoidParameterless,
            LifecycleDiagnostics.DuplicateExplicitOrder);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
        }

        private static void AnalyzeNamedType(SymbolAnalysisContext ctx)
        {
            if (ctx.Symbol is not INamedTypeSymbol classSymbol || classSymbol.TypeKind != TypeKind.Class)
            {
                return;
            }

            // Discover what dispatchers the generator will emit for this class:
            // one per lifecycle kind that has at least one attributed method, plus
            // Awake if the class is [Singleton], plus Start if any field has
            // [Dependency]. Methods with bad signatures are still reported so the
            // user sees the real reason their attribute is being ignored.
            var attributedMethods = new List<AttributedMethod>();
            var dispatcherKinds = new bool[LifecycleKinds.Count];
            var hasDependencyField = false;

            foreach (var member in classSymbol.GetMembers())
            {
                if (member is IFieldSymbol field
                    && SymbolInspector.HasAttribute(field, DependencyConstants.AttributeFullName))
                {
                    hasDependencyField = true;
                    continue;
                }

                if (member is not IMethodSymbol method || method.MethodKind != MethodKind.Ordinary)
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

                        attributedMethods.Add(new AttributedMethod(method, kind, attribute));
                        dispatcherKinds[k] = true;
                    }
                }
            }

            var hasSingletonAttribute = SymbolInspector.HasAttribute(classSymbol, SingletonConstants.AttributeFullName);
            if (hasSingletonAttribute)
            {
                dispatcherKinds[(int)LifecycleKind.Awake] = true;
            }

            if (hasDependencyField)
            {
                dispatcherKinds[(int)LifecycleKind.Start] = true;
            }

            var willGenerateAnyDispatcher = false;
            for (int k = 0; k < LifecycleKinds.Count; k++)
            {
                if (dispatcherKinds[k])
                {
                    willGenerateAnyDispatcher = true;
                    break;
                }
            }

            // Nothing to dispatch and no attributes to validate — leave the class
            // alone. [Singleton]/[Dependency] structural checks live in their own
            // analyzers; ERG02xx only fires when this generator actually emits.
            if (!willGenerateAnyDispatcher)
            {
                return;
            }

            var classLocation = GetClassIdentifierLocation(classSymbol) ?? Location.None;
            var className = classSymbol.Name;

            if (attributedMethods.Count > 0
                && !SymbolInspector.InheritsFrom(classSymbol, LifecycleConstants.MonoBehaviourFullyQualifiedName))
            {
                // [Singleton]/[Dependency] already own the MonoBehaviour check on
                // their own attributes; only report here for lifecycle attributes
                // (where this analyzer is the only signal).
                foreach (var attributed in attributedMethods)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        LifecycleDiagnostics.MustBeMonoBehaviour,
                        attributed.Method.Locations.FirstOrDefault() ?? classLocation,
                        attributed.Method.Name));
                }
            }

            if (attributedMethods.Count > 0 && !SymbolInspector.IsPartial(classSymbol))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(LifecycleDiagnostics.MustBePartial, classLocation, className));
            }

            ValidateAttributedMethodSignatures(ctx, attributedMethods);
            ValidateUserDefinedLifecycleConflicts(ctx, classSymbol, dispatcherKinds, className);
            ValidateExplicitOrderUniqueness(ctx, attributedMethods);
        }

        private static void ValidateAttributedMethodSignatures(
            SymbolAnalysisContext ctx,
            List<AttributedMethod> attributedMethods)
        {
            foreach (var attributed in attributedMethods)
            {
                var method = attributed.Method;
                if (method.Parameters.Length == 0 && method.ReturnsVoid && !method.IsStatic)
                {
                    continue;
                }

                ctx.ReportDiagnostic(Diagnostic.Create(
                    LifecycleDiagnostics.MethodMustBeInstanceVoidParameterless,
                    method.Locations.FirstOrDefault() ?? Location.None,
                    method.Name,
                    LifecycleKinds.MethodName(attributed.Kind)));
            }
        }

        private static void ValidateUserDefinedLifecycleConflicts(
            SymbolAnalysisContext ctx,
            INamedTypeSymbol classSymbol,
            bool[] dispatcherKinds,
            string className)
        {
            for (int k = 0; k < LifecycleKinds.Count; k++)
            {
                if (!dispatcherKinds[k])
                {
                    continue;
                }

                var kind = (LifecycleKind)k;
                var lifecycleMethodName = LifecycleKinds.MethodName(kind);

                // Walk members named exactly the lifecycle entry point and report
                // any user-defined ones (signature: parameterless instance) — the
                // generator's dispatcher will collide with them.
                foreach (var candidate in classSymbol.GetMembers(lifecycleMethodName))
                {
                    if (candidate is not IMethodSymbol method
                        || method.MethodKind != MethodKind.Ordinary
                        || method.IsStatic
                        || method.Parameters.Length != 0)
                    {
                        continue;
                    }

                    var location = method.Locations.FirstOrDefault() ?? Location.None;
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        LifecycleDiagnostics.MustNotDefineLifecycleMethod,
                        location,
                        className,
                        lifecycleMethodName));
                }
            }
        }

        private static void ValidateExplicitOrderUniqueness(
            SymbolAnalysisContext ctx,
            List<AttributedMethod> attributedMethods)
        {
            // Same explicit Order within the same kind: degrades to source-order,
            // which is rarely what the user intends; warn so they can spot it.
            var groupedByKind = new Dictionary<LifecycleKind, List<AttributedMethod>>();
            foreach (var attributed in attributedMethods)
            {
                if (!groupedByKind.TryGetValue(attributed.Kind, out var list))
                {
                    list = new List<AttributedMethod>();
                    groupedByKind[attributed.Kind] = list;
                }
                list.Add(attributed);
            }

            foreach (var pair in groupedByKind)
            {
                var kind = pair.Key;
                var groupByOrder = new Dictionary<int, List<AttributedMethod>>();
                foreach (var attributed in pair.Value)
                {
                    var (order, hasExplicit) = ReadOrder(attributed.Attribute);
                    if (!hasExplicit)
                    {
                        continue;
                    }

                    if (!groupByOrder.TryGetValue(order, out var list))
                    {
                        list = new List<AttributedMethod>();
                        groupByOrder[order] = list;
                    }
                    list.Add(attributed);
                }

                foreach (var orderPair in groupByOrder)
                {
                    if (orderPair.Value.Count < 2)
                    {
                        continue;
                    }

                    var attributeName = LifecycleKinds.MethodName(kind);
                    var orderText = orderPair.Key.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    for (int i = 0; i < orderPair.Value.Count; i++)
                    {
                        var current = orderPair.Value[i];
                        var other = orderPair.Value[(i + 1) % orderPair.Value.Count];
                        ctx.ReportDiagnostic(Diagnostic.Create(
                            LifecycleDiagnostics.DuplicateExplicitOrder,
                            current.Method.Locations.FirstOrDefault() ?? Location.None,
                            current.Method.Name,
                            attributeName,
                            orderText,
                            other.Method.Name));
                    }
                }
            }
        }

        private static (int Order, bool HasExplicitOrder) ReadOrder(AttributeData attribute)
        {
            int order = 0;
            bool hasExplicitOrder = false;

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

        private static Location? GetClassIdentifierLocation(INamedTypeSymbol classSymbol)
        {
            foreach (var reference in classSymbol.DeclaringSyntaxReferences)
            {
                if (reference.GetSyntax() is ClassDeclarationSyntax declaration)
                {
                    return declaration.Identifier.GetLocation();
                }
            }

            return classSymbol.Locations.FirstOrDefault();
        }

        private readonly struct AttributedMethod
        {
            public AttributedMethod(IMethodSymbol method, LifecycleKind kind, AttributeData attribute)
            {
                Method = method;
                Kind = kind;
                Attribute = attribute;
            }

            public IMethodSymbol Method { get; }

            public LifecycleKind Kind { get; }

            public AttributeData Attribute { get; }
        }
    }
}
