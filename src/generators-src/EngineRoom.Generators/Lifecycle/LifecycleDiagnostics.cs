using Microsoft.CodeAnalysis;

namespace EngineRoom.Generators.Lifecycle
{
    internal static class LifecycleDiagnostics
    {
        private const string Category = "EngineRoom.Runtime.Lifecycle";

        public static readonly DiagnosticDescriptor MustBeMonoBehaviour = new DiagnosticDescriptor(
            id: "ERG0201",
            title: "Lifecycle attributes require a MonoBehaviour",
            messageFormat: "Method '{0}' is decorated with a lifecycle attribute but its containing class does not inherit from UnityEngine.MonoBehaviour",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MustBePartial = new DiagnosticDescriptor(
            id: "ERG0202",
            title: "Lifecycle attributes require a partial class",
            messageFormat: "Class '{0}' uses lifecycle attributes and must be declared partial",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MustNotDefineLifecycleMethod = new DiagnosticDescriptor(
            id: "ERG0203",
            title: "Generated lifecycle method conflicts with user-defined one",
            messageFormat: "Class '{0}' must not define its own '{1}()' method — a generated lifecycle dispatcher will conflict. Move that code into a private method marked with [{1}] instead.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MethodMustBeInstanceVoidParameterless = new DiagnosticDescriptor(
            id: "ERG0204",
            title: "Lifecycle methods must be instance, parameterless, and return void",
            messageFormat: "Method '{0}' is decorated with [{1}] and must be an instance method, take no parameters, and return void",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor DuplicateExplicitOrder = new DiagnosticDescriptor(
            id: "ERG0205",
            title: "Two lifecycle methods share the same explicit order",
            messageFormat: "Method '{0}' shares explicit [{1}({2})] order with method '{3}'. Their relative invocation order falls back to source declaration order.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);
    }
}
