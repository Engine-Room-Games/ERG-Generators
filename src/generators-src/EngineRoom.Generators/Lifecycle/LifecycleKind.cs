namespace EngineRoom.Generators.Lifecycle
{
    internal enum LifecycleKind
    {
        Awake = 0,
        Start = 1,
        OnEnable = 2,
        OnDisable = 3,
        Update = 4,
        FixedUpdate = 5,
        LateUpdate = 6,
        OnDestroy = 7,
    }

    internal static class LifecycleKinds
    {
        public const int Count = 8;

        public static string MethodName(LifecycleKind kind)
        {
            return kind switch
            {
                LifecycleKind.Awake => "Awake",
                LifecycleKind.Start => "Start",
                LifecycleKind.OnEnable => "OnEnable",
                LifecycleKind.OnDisable => "OnDisable",
                LifecycleKind.Update => "Update",
                LifecycleKind.FixedUpdate => "FixedUpdate",
                LifecycleKind.LateUpdate => "LateUpdate",
                LifecycleKind.OnDestroy => "OnDestroy",
                _ => string.Empty,
            };
        }

        public static string AttributeFullName(LifecycleKind kind)
        {
            return kind switch
            {
                LifecycleKind.Awake => LifecycleConstants.AwakeAttributeFullName,
                LifecycleKind.Start => LifecycleConstants.StartAttributeFullName,
                LifecycleKind.OnEnable => LifecycleConstants.OnEnableAttributeFullName,
                LifecycleKind.OnDisable => LifecycleConstants.OnDisableAttributeFullName,
                LifecycleKind.Update => LifecycleConstants.UpdateAttributeFullName,
                LifecycleKind.FixedUpdate => LifecycleConstants.FixedUpdateAttributeFullName,
                LifecycleKind.LateUpdate => LifecycleConstants.LateUpdateAttributeFullName,
                LifecycleKind.OnDestroy => LifecycleConstants.OnDestroyAttributeFullName,
                _ => string.Empty,
            };
        }

        // Awake and Start retain a back-compat partial hook (OnAwake / OnStart) so
        // existing code that implements those partials keeps working after the
        // Singleton / Dependency generators stop emitting them directly.
        public static string? BackCompatPartialName(LifecycleKind kind)
        {
            return kind switch
            {
                LifecycleKind.Awake => "OnAwake",
                LifecycleKind.Start => "OnStart",
                _ => null,
            };
        }
    }
}
