using System.Collections.Immutable;
using Apex.Analyzers.Immutable.Rules;
using Apex.Analyzers.Immutable.Semantics;
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
                return ImmutableArray.Create(IMM001.Rule, IMM002.Rule, IMM003.Rule, IMM004.Rule, IMM005.Rule, IMM006.Rule, IMM007.Rule, IMM008.Rule);
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            var whitelist = new ImmutableTypes();
            IMM001.Initialize(context);
            IMM002.Initialize(context);
            IMM003.Initialize(context, whitelist);
            IMM004.Initialize(context, whitelist);
            IMM005.Initialize(context);
            IMM006.Initialize(context, whitelist);
            IMM007.Initialize(context);
            IMM008.Initialize(context);
        }
    }
}

