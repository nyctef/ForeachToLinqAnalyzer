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
            var refactorType = diagnostic.Properties[ForeachToLinqAnalyzer.RefactorType];
            var foreachStatement = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ForEachStatementSyntax>().First();
            if (refactorType == ForeachToLinqAnalyzer.ContainingIfToWhere)
            {
                var ifStatement = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<IfStatementSyntax>().First();
                context.RegisterCodeFix(
                    CodeAction.Create("Convert if statement to LINQ method chain",
                    c => ConvertIfToLINQ(context.Document, ifStatement, foreachStatement, c)),
                    diagnostic);
            }
            else if (refactorType == ForeachToLinqAnalyzer.IfWithContinueToWhere)
            {
                var ifStatement = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<IfStatementSyntax>().First();
                context.RegisterCodeFix(
                    CodeAction.Create("Convert if statement to LINQ method chain",
                    c => ConvertIfContinueToLINQ(context.Document, ifStatement, foreachStatement, c)),
                    diagnostic);
            }
            else if (refactorType == ForeachToLinqAnalyzer.VariableToSelect)
            {
                var variableDeclarator = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<VariableDeclaratorSyntax>().First();
                context.RegisterCodeFix(
                    CodeAction.Create("Convert variable assignment to LINQ method chain",
                    c => ConvertVariableAssignmentToLINQ(context.Document, variableDeclarator, foreachStatement, c)),
                    diagnostic);
            }
        }

        private async Task<Document> ConvertIfToLINQ(Document document, IfStatementSyntax ifStatement, ForEachStatementSyntax foreachStatement, CancellationToken c)
        {
            var generator = SyntaxGenerator.GetGenerator(document);

            var variableName = ifStatement.Condition.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Single(x => x.Identifier.Text == foreachStatement.Identifier.Text);

            var whereCall = generator.InvocationExpression(
                generator.MemberAccessExpression(foreachStatement.Expression, "Where"),
                generator.Argument(
                    generator.ValueReturningLambdaExpression(
                        new[] { generator.LambdaParameter("x") }, 
                        ifStatement.Condition.ReplaceNode(variableName, generator.IdentifierName("x")))));

            var root = await document.GetSyntaxRootAsync(c);

            var newFeStatement = foreachStatement
                .WithExpression((ExpressionSyntax)whereCall)
                .WithStatement(ifStatement.Statement.WithAdditionalAnnotations(Formatter.Annotation));

            var newRoot = root.ReplaceNode(foreachStatement, newFeStatement);
            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<Document> ConvertIfContinueToLINQ(Document document, IfStatementSyntax ifStatement, ForEachStatementSyntax foreachStatement, CancellationToken c)
        {
            var generator = SyntaxGenerator.GetGenerator(document);

            var variableName = ifStatement.Condition.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Single(x => x.Identifier.Text == foreachStatement.Identifier.Text);

            var whereCall = generator.InvocationExpression(
                generator.MemberAccessExpression(foreachStatement.Expression, "Where"),
                generator.Argument(
                    generator.ValueReturningLambdaExpression(
                        new[] { generator.LambdaParameter("x") },
                        generator.LogicalNotExpression(
                            ifStatement.Condition.ReplaceNode(variableName, generator.IdentifierName("x"))))));

            var root = await document.GetSyntaxRootAsync(c);

            var restOfForeachBody = foreachStatement.Statement
                .RemoveNode(ifStatement, SyntaxRemoveOptions.AddElasticMarker)
                .WithAdditionalAnnotations(Formatter.Annotation);
            var newFeStatement = foreachStatement
                .WithExpression((ExpressionSyntax)whereCall)
                .WithStatement(restOfForeachBody);

            var newRoot = root.ReplaceNode(foreachStatement, newFeStatement);
            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<Document> ConvertVariableAssignmentToLINQ(Document document, VariableDeclaratorSyntax variableDeclarator, ForEachStatementSyntax foreachStatement, CancellationToken c)
        {
            var generator = SyntaxGenerator.GetGenerator(document);

            var oldVariableName = foreachStatement.Identifier.Text;
            var newVariableName = variableDeclarator.Identifier.Text;

            var oldVariables = variableDeclarator.Initializer.Value.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Where(x => x.Identifier.Text == oldVariableName);

            var selectCall = generator.InvocationExpression(
                generator.MemberAccessExpression(foreachStatement.Expression, "Select"),
                generator.Argument(
                    generator.ValueReturningLambdaExpression(
                        new[] { generator.LambdaParameter("x") },
                            variableDeclarator.Initializer.Value.ReplaceNodes(oldVariables, (x, y) => generator.IdentifierName("x")))));

            var root = await document.GetSyntaxRootAsync(c);

            Debug.WriteLine(variableDeclarator.Parent);
            var restOfForeachBody = foreachStatement.Statement
                .RemoveNode(variableDeclarator, SyntaxRemoveOptions.AddElasticMarker)
                .WithAdditionalAnnotations(Formatter.Annotation);
            var newFeStatement = foreachStatement
                .WithExpression((ExpressionSyntax)selectCall)
                .WithIdentifier(generator.IdentifierName(newVariableName).GetFirstToken())
                .WithStatement(restOfForeachBody);

            var newRoot = root.ReplaceNode(foreachStatement, newFeStatement);

            var withEmptyVariableDeclarationsRemoved = new RemoveEmptyVariableDeclarationSyntax().Visit(newRoot);

            return document.WithSyntaxRoot(withEmptyVariableDeclarationsRemoved);
        }

        private class RemoveEmptyVariableDeclarationSyntax : CSharpSyntaxRewriter
        {
            public override SyntaxNode VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
            {
                if (!node.Declaration.Variables.Any())
                {
                    return null;
                }
                return node;
            }
        }
    }
}