using Microsoft.CodeAnalysis;

namespace EngineRoom.Generators.Singleton
{
    internal static class SingletonDiagnostics
    {
        private const string Category = "EngineRoom.Singleton";

        public static readonly DiagnosticDescriptor MustBeMonoBehaviour = new DiagnosticDescriptor(
            id: "ERG0001",
            title: "[Singleton] must be applied to a MonoBehaviour",
            messageFormat: "Class '{0}' is decorated with [Singleton] but does not inherit from UnityEngine.MonoBehaviour",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MustBePartial = new DiagnosticDescriptor(
            id: "ERG0002",
            title: "[Singleton] class must be partial",
            messageFormat: "Class '{0}' is decorated with [Singleton] and must be declared partial",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MustNotDefineAwake = new DiagnosticDescriptor(
            id: "ERG0003",
            title: "[Singleton] class must not define its own Awake",
            messageFormat: "Class '{0}' is decorated with [Singleton] and must not define an Awake() method. Move that code into AwakeInternal() instead.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MemberMustBePublic = new DiagnosticDescriptor(
            id: "ERG0004",
            title: "[SingletonMember] must be public",
            messageFormat: "Member '{0}' is marked [SingletonMember] and must be declared public to appear on the generated singleton interface",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MemberMustBeInstance = new DiagnosticDescriptor(
            id: "ERG0005",
            title: "[SingletonMember] must not be static",
            messageFormat: "Member '{0}' is marked [SingletonMember] and must be an instance member, not static",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }
}
