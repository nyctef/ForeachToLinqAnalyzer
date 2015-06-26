using System;
using System.Collections.Generic;
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
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace ForeachToLinqAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ForeachToLinqCodeFixProvider)), Shared]
    public class ForeachToLinqCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(ForeachToLinqAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
            //var diagnostic = context.Diagnostics.First();
            //var diagnosticSpan = diagnostic.Location.SourceSpan;

            //// Find the type declaration identified by the diagnostic.
            //var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().First();

            //// Register a code action that will invoke the fix.
            //context.RegisterCodeFix(
            //    CodeAction.Create("Make uppercase", c => MakeUppercaseAsync(context.Document, declaration, c)),
            //    diagnostic);
        }
    }
}