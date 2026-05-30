using System;

namespace EngineRoom.Generators.Lifecycle
{
    // Strings + primitives only so the incremental cache key doesn't depend on
    // Roslyn symbol or Location identity (which churns between compilations).
    internal readonly struct LifecycleEntry : IEquatable<LifecycleEntry>
    {
        public LifecycleEntry(string methodName, int order, bool hasExplicitOrder, int declarationPosition)
        {
            MethodName = methodName;
            Order = order;
            HasExplicitOrder = hasExplicitOrder;
            DeclarationPosition = declarationPosition;
        }

        public string MethodName { get; }

        public int Order { get; }

        public bool HasExplicitOrder { get; }

        // Source-text offset of the method declaration; used as the stable
        // tiebreaker when two entries share the same Order.
        public int DeclarationPosition { get; }

        public bool Equals(LifecycleEntry other)
        {
            return MethodName == other.MethodName
                && Order == other.Order
                && HasExplicitOrder == other.HasExplicitOrder
                && DeclarationPosition == other.DeclarationPosition;
        }

        public override bool Equals(object? obj)
        {
            return obj is LifecycleEntry other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (MethodName?.GetHashCode() ?? 0);
                hash = hash * 31 + Order;
                hash = hash * 31 + (HasExplicitOrder ? 1 : 0);
                hash = hash * 31 + DeclarationPosition;
                return hash;
            }
        }
    }
}
