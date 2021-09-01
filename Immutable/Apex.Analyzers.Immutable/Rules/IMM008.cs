﻿using System;
using System.Linq;
using Apex.Analyzers.Immutable.Semantics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Apex.Analyzers.Immutable.Rules
{
    internal static class IMM008
    {
        public const string DiagnosticId = "IMM008";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.IMM008Title), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.IMM008MessageFormat), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.IMM008Description), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Architecture";

        public static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);
        internal static void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();
            // Can't just use MethodBodyOperation in general to analyze property method bodies, but the exceptional
            // case is for get only properties, so this should work for init only
            // https://github.com/dotnet/roslyn/issues/28163
            context.RegisterOperationAction(AnalyzeOperation, OperationKind.MethodBodyOperation);
        }

        private static void AnalyzeOperation(OperationAnalysisContext context)
        {
            if(!Helper.HasImmutableAttributeAndShouldVerify(context.ContainingSymbol?.ContainingType))
            {
                return;
            }

            if (context.ContainingSymbol is IMethodSymbol method && Helper.IsInitOnlyMethod(method))
            {
                CheckOperation(context.Operation, context, false);
            }
        }

        private static void CheckOperation(IOperation operation, OperationAnalysisContext context, bool hasReportedThisCapture)
        {
            if(operation is IInstanceReferenceOperation op
                && op.ReferenceKind == InstanceReferenceKind.ContainingTypeInstance
                && (op.Parent is IArgumentOperation
                    || op.Parent is ISymbolInitializerOperation
                    || op.Parent is IAssignmentOperation))
            {
                var diagnostic = Diagnostic.Create(Rule, op.Syntax.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }

            bool reportedThisCapture = false;

            if (!hasReportedThisCapture)
            {
                if (operation.Syntax is AnonymousFunctionExpressionSyntax syntax)
                {
                    var model = operation.SemanticModel;
                    var dataFlowAnalysis = model.AnalyzeDataFlow(syntax);
                    var capturedVariables = dataFlowAnalysis.Captured;
                    if (capturedVariables.Any(x => x is IParameterSymbol p && p.IsThis))
                    {
                        var diagnostic = Diagnostic.Create(Rule, operation.Syntax.GetLocation());
                        context.ReportDiagnostic(diagnostic);
                        reportedThisCapture = true;
                    }
                }
            }

            foreach (var child in operation.Children)
            {
                CheckOperation(child, context, reportedThisCapture || hasReportedThisCapture);
            }
        }
    }
}
