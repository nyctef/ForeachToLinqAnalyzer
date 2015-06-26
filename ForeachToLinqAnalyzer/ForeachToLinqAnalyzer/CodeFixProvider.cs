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

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var ifStatement = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<IfStatementSyntax>().First();
            var feStatement = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ForEachStatementSyntax>().First();
            var ifType = diagnostic.Properties[ForeachToLinqAnalyzer.IfType];
            if (ifType == ForeachToLinqAnalyzer.ContainingIf)
            {
                context.RegisterCodeFix(
                    CodeAction.Create("Convert if statement to LINQ method chain",
                    c => ConvertIfToLINQ(context.Document, ifStatement, feStatement, c)),
                    diagnostic);
            }
            else if (ifType == ForeachToLinqAnalyzer.IfWithContinue)
            {
                context.RegisterCodeFix(
                    CodeAction.Create("Convert if statement to LINQ method chain",
                    c => ConvertIfContinueToLINQ(context.Document, ifStatement, feStatement, c)),
                    diagnostic);
            }
        }

        private async Task<Document> ConvertIfToLINQ(Document document, IfStatementSyntax ifStatement, ForEachStatementSyntax feStatement, CancellationToken c)
        {
            var generator = SyntaxGenerator.GetGenerator(document);

            var variableName = ifStatement.Condition.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Single(x => x.Identifier.Text == feStatement.Identifier.Text);

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
            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<Document> ConvertIfContinueToLINQ(Document document, IfStatementSyntax ifStatement, ForEachStatementSyntax feStatement, CancellationToken c)
        {
            var generator = SyntaxGenerator.GetGenerator(document);

            var variableName = ifStatement.Condition.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Single(x => x.Identifier.Text == feStatement.Identifier.Text);

            var whereCall = generator.InvocationExpression(
                generator.MemberAccessExpression(feStatement.Expression, "Where"),
                generator.Argument(
                    generator.ValueReturningLambdaExpression(
                        new[] { generator.LambdaParameter("x") },
                        generator.LogicalNotExpression(
                            ifStatement.Condition.ReplaceNode(variableName, generator.IdentifierName("x"))))));

            var root = await document.GetSyntaxRootAsync(c);

            var restOfForeachBody = feStatement.Statement
                .RemoveNode(ifStatement, SyntaxRemoveOptions.AddElasticMarker)
                .WithAdditionalAnnotations(Formatter.Annotation);
            var newFeStatement = feStatement
                .WithExpression((ExpressionSyntax)whereCall)
                .WithStatement(restOfForeachBody);

            var newRoot = root.ReplaceNode(feStatement, newFeStatement);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}