using System.Linq;
using Microsoft.CodeAnalysis;

namespace Apex.Analyzers.Immutable.Rules
{
    internal static class Helper
    {
        internal static bool HasImmutableAttribute(INamedTypeSymbol type)
        {
            var attributes = type.GetAttributes();
            return attributes.Any(x => x.AttributeClass.Name == "ImmutableAttribute"
                && x.AttributeClass.ContainingNamespace?.Name == "System");
        }
    }
}
