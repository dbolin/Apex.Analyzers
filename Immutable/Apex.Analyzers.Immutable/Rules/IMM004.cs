using Apex.Analyzers.Immutable.Semantics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Apex.Analyzers.Immutable.Rules
{
    internal static class IMM004
    {
        public const string DiagnosticId = "IMM004";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.IMM004Title), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.IMM004MessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormatGeneric = new LocalizableResourceString(nameof(Resources.IMM004MessageFormatGeneric), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.IMM004Description), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Architecture";

        public static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);
        public static DiagnosticDescriptor RuleGeneric = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormatGeneric, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        internal static void Initialize(AnalysisContext context, ImmutableTypes immutableTypes)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(x => AnalyzeSymbol(x, immutableTypes), SymbolKind.Property);
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context, ImmutableTypes immutableTypes)
        {
            immutableTypes.Initialize(context.Compilation, context.Options, context.CancellationToken);

            var symbol = (IPropertySymbol)context.Symbol;
            var containingType = symbol.ContainingType;
            if (containingType == null)
            {
                return;
            }

            string genericTypeArgument = null;
            if (Helper.HasImmutableAttributeAndShouldVerify(containingType)
                && Helper.ShouldCheckMemberTypeForImmutability(symbol)
                && !immutableTypes.IsImmutableType(symbol.Type, ref genericTypeArgument)
                && Helper.IsAutoProperty(symbol))
            {
                if (genericTypeArgument != null)
                {
                    var diagnostic = Diagnostic.Create(RuleGeneric, symbol.Locations[0], symbol.Name, genericTypeArgument);
                    context.ReportDiagnostic(diagnostic);
                }
                else
                {
                    var diagnostic = Diagnostic.Create(Rule, symbol.Locations[0], symbol.Name);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
