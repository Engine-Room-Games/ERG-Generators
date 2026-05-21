using Microsoft.CodeAnalysis;

namespace EngineRoom.Generators.Singleton
{
    internal sealed class DiagnosticInfo
    {
        public DiagnosticInfo(DiagnosticDescriptor descriptor, Location location, params object[] args)
        {
            Descriptor = descriptor;
            Location = location;
            Args = args;
        }

        public DiagnosticDescriptor Descriptor { get; }

        public Location Location { get; }

        public object[] Args { get; }
    }
}