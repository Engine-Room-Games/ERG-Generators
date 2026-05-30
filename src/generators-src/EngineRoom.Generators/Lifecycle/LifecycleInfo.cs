using System;
using System.Collections.Immutable;

namespace EngineRoom.Generators.Lifecycle
{
    internal sealed class LifecycleInfo : IEquatable<LifecycleInfo>
    {
        public LifecycleInfo(
            string className,
            string? @namespace,
            string hintPrefix,
            bool hasSingleton,
            bool hasDependencyField,
            ImmutableArray<ImmutableArray<LifecycleEntry>> entriesByKind)
        {
            ClassName = className;
            Namespace = @namespace;
            HintPrefix = hintPrefix;
            HasSingleton = hasSingleton;
            HasDependencyField = hasDependencyField;
            EntriesByKind = entriesByKind;
        }

        public string ClassName { get; }

        public string? Namespace { get; }

        public string HintPrefix { get; }

        public bool HasSingleton { get; }

        public bool HasDependencyField { get; }

        // Indexed by (int)LifecycleKind; always length LifecycleKinds.Count.
        public ImmutableArray<ImmutableArray<LifecycleEntry>> EntriesByKind { get; }

        public bool HasAnyEntries
        {
            get
            {
                if (HasSingleton || HasDependencyField)
                {
                    return true;
                }

                foreach (var bucket in EntriesByKind)
                {
                    if (bucket.Length > 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool Equals(LifecycleInfo? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ClassName != other.ClassName
                || Namespace != other.Namespace
                || HintPrefix != other.HintPrefix
                || HasSingleton != other.HasSingleton
                || HasDependencyField != other.HasDependencyField
                || EntriesByKind.Length != other.EntriesByKind.Length)
            {
                return false;
            }

            for (int kind = 0; kind < EntriesByKind.Length; kind++)
            {
                var left = EntriesByKind[kind];
                var right = other.EntriesByKind[kind];
                if (left.Length != right.Length)
                {
                    return false;
                }

                for (int index = 0; index < left.Length; index++)
                {
                    if (!left[index].Equals(right[index]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as LifecycleInfo);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (ClassName?.GetHashCode() ?? 0);
                hash = hash * 31 + (Namespace?.GetHashCode() ?? 0);
                hash = hash * 31 + (HintPrefix?.GetHashCode() ?? 0);
                hash = hash * 31 + (HasSingleton ? 1 : 0);
                hash = hash * 31 + (HasDependencyField ? 1 : 0);
                foreach (var bucket in EntriesByKind)
                {
                    foreach (var entry in bucket)
                    {
                        hash = hash * 31 + entry.GetHashCode();
                    }
                }
                return hash;
            }
        }
    }
}
