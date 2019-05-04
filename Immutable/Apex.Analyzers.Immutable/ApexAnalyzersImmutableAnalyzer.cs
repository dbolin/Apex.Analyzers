using System.Collections.Immutable;
using Apex.Analyzers.Immutable.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Apex.Analyzers.Immutable
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ApexAnalyzersImmutableAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(IMM001.Rule, IMM002.Rule);
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            IMM001.Initialize(context);
            IMM002.Initialize(context);
        }
    }
}
