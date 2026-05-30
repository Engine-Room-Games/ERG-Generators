namespace EngineRoom.Generators.Lifecycle
{
    internal static class LifecycleConstants
    {
        public static readonly string AwakeAttributeFullName = typeof(global::EngineRoom.Runtime.Lifecycle.AwakeAttribute).FullName!;
        public static readonly string StartAttributeFullName = typeof(global::EngineRoom.Runtime.Lifecycle.StartAttribute).FullName!;
        public static readonly string OnEnableAttributeFullName = typeof(global::EngineRoom.Runtime.Lifecycle.OnEnableAttribute).FullName!;
        public static readonly string OnDisableAttributeFullName = typeof(global::EngineRoom.Runtime.Lifecycle.OnDisableAttribute).FullName!;
        public static readonly string UpdateAttributeFullName = typeof(global::EngineRoom.Runtime.Lifecycle.UpdateAttribute).FullName!;
        public static readonly string FixedUpdateAttributeFullName = typeof(global::EngineRoom.Runtime.Lifecycle.FixedUpdateAttribute).FullName!;
        public static readonly string LateUpdateAttributeFullName = typeof(global::EngineRoom.Runtime.Lifecycle.LateUpdateAttribute).FullName!;
        public static readonly string OnDestroyAttributeFullName = typeof(global::EngineRoom.Runtime.Lifecycle.OnDestroyAttribute).FullName!;

        public const string MonoBehaviourFullyQualifiedName = "global::UnityEngine.MonoBehaviour";

        // Names of the per-feature helpers contributed by sibling generators.
        // Singleton emits a bool helper (false = duplicate, Destroy called, bail).
        // Dependency emits a void helper (field assignments).
        public const string SingletonAwakeHelperName = "SingletonAwake";
        public const string DependencyStartHelperName = "DependencyStart";
    }
}
