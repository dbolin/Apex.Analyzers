using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Linq;

namespace Apex.Analyzers.Immutable.Rules
{
    internal static class IMM007
    {
        public const string DiagnosticId = "IMM007";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.IMM007Title), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.IMM007MessageFormat), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.IMM007Description), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Architecture";

        public static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);
        internal static void Initialize(AnalysisContext context)
        {
            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            var symbol = (INamedTypeSymbol)context.Symbol;
            if (!Helper.HasImmutableAttribute(symbol))
            {
                var baseTypeName = Helper.HasImmutableAttribute(symbol.BaseType) ? symbol.BaseType.Name : null;
                var interfaceName = symbol.AllInterfaces.FirstOrDefault(x => Helper.HasImmutableAttribute(x))?.Name;

                if (baseTypeName != null)
                {
                    var diagnostic = Diagnostic.Create(Rule, symbol.Locations[0], symbol.Name, baseTypeName);
                    context.ReportDiagnostic(diagnostic);
                }
                else if(interfaceName != null)
                {
                    var diagnostic = Diagnostic.Create(Rule, symbol.Locations[0], symbol.Name, interfaceName);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
