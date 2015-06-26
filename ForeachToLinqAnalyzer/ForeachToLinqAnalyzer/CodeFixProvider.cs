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
using System.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;

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
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var ifStatement = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<IfStatementSyntax>().First();
            var feStatement = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ForEachStatementSyntax>().First();
            context.RegisterCodeFix(
                CodeAction.Create("Convert if statement to LINQ method chain", 
                c => ConvertIfToLINQ(context.Document, ifStatement, feStatement, c)), 
                diagnostic);
        }

        private async Task<Document> ConvertIfToLINQ(Document document, IfStatementSyntax ifStatement, ForEachStatementSyntax feStatement, CancellationToken c)
        {
            var generator = SyntaxGenerator.GetGenerator(document);

            var variableName = ifStatement.Condition.ChildNodes().OfType<IdentifierNameSyntax>().Single();

            var whereCall = generator.InvocationExpression(
                generator.MemberAccessExpression(feStatement.Expression, "Where"),
                generator.Argument(
                    generator.ValueReturningLambdaExpression(
                        new[] { generator.LambdaParameter("x") }, 
                        ifStatement.Condition.ReplaceNode(variableName, generator.IdentifierName("x")))));

            var root = await document.GetSyntaxRootAsync(c);

            var newFeStatement = feStatement
                .WithExpression((ExpressionSyntax)whereCall)
                .WithStatement(ifStatement.Statement.WithAdditionalAnnotations(Formatter.Annotation));

            var newRoot = root.ReplaceNode(feStatement, newFeStatement);
            //var formattedRoot = Formatter.Format(newRoot, Formatter.Annotation, document.Project.Solution.Workspace);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}