using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Apex.Analyzers.Immutable.Rules;

namespace Apex.Analyzers.Immutable
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ApexAnalyzersImmutableCodeFixProvider)), Shared]
    public class ApexAnalyzersImmutableCodeFixProvider : CodeFixProvider
    {
        private const string titleReadonly = "Make readonly";
        private const string titleSetAccessor = "Remove set accessor";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(IMM001.DiagnosticId, IMM002.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            if (diagnostic.Id == IMM001.DiagnosticId)
            {
                // Find the field declaration identified by the diagnostic.
                var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<FieldDeclarationSyntax>().First();

                // Register a code action that will invoke the fix.
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: titleReadonly,
                        createChangedDocument: c => AddReadonlyModifierAsync(context.Document, declaration, c),
                        equivalenceKey: titleReadonly),
                    diagnostic);
            }
            else if (diagnostic.Id == IMM002.DiagnosticId)
            {
                // Find the field declaration identified by the diagnostic.
                var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<PropertyDeclarationSyntax>().First();

                // Register a code action that will invoke the fix.
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: titleSetAccessor,
                        createChangedDocument: c => RemoveSetMethodAsync(context.Document, declaration, c),
                        equivalenceKey: titleSetAccessor),
                    diagnostic);
            }
        }

        private async Task<Document> AddReadonlyModifierAsync(Document document, FieldDeclarationSyntax decl, CancellationToken cancellationToken)
        {
            var readonlyToken = SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword);
            var newSyntax = decl.WithModifiers(decl.Modifiers.Add(readonlyToken));

            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = oldRoot.ReplaceNode(decl, newSyntax);

            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<Document> RemoveSetMethodAsync(Document document, PropertyDeclarationSyntax decl, CancellationToken cancellationToken)
        {
            var setAccessorNode = decl.AccessorList.Accessors.Where(x => x.Keyword.Text == SyntaxFactory.Token(SyntaxKind.SetKeyword).Text)
                .FirstOrDefault();

            if(setAccessorNode == null)
            {
                return document;
            }

            var newSyntax = decl.WithAccessorList(decl.AccessorList.RemoveNode(setAccessorNode, SyntaxRemoveOptions.KeepNoTrivia));

            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = oldRoot.ReplaceNode(decl, newSyntax);

            return document.WithSyntaxRoot(newRoot);
        }
    }
}
