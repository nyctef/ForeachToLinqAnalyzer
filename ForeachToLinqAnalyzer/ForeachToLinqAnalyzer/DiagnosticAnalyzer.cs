using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ForeachToLinqAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ForeachToLinqAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ForeachToLinqAnalyzer";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        internal static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        internal const string Category = "Naming";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            context.RegisterSemanticModelAction(AnalyzeSymbol);
        }

        private static async void AnalyzeSymbol(SemanticModelAnalysisContext context)
        {
            // TODO: Replace the following code with your own analysis, generating Diagnostic objects for any issues you find
            var cancel = context.CancellationToken;
            var tree = await context.SemanticModel.SyntaxTree.GetRootAsync(cancel);
            var foreachs = tree.DescendantNodes().OfType<ForEachStatementSyntax>().ToList();
            foreach (var fe in foreachs)
            {

            var dataFlow = context.SemanticModel.AnalyzeDataFlow(fe);
                var loopVariable = dataFlow.VariablesDeclared.Single(x => x.Name == fe.Identifier.Text);

                var ifs = fe.DescendantNodes().OfType<IfStatementSyntax>();

                foreach (var i in ifs)
                {
                    var ifExpression = i.Condition;
                    var notEqualsExpression = ifExpression as BinaryExpressionSyntax;
                    if (notEqualsExpression != null && notEqualsExpression.Kind() == SyntaxKind.NotEqualsExpression)
                    {
                        var left = notEqualsExpression.Left as IdentifierNameSyntax;
                        var right = notEqualsExpression.Right as LiteralExpressionSyntax;
                        // TODO: the first half of this (.Right == null) seems to be the bit that fires, weirdly enough >.>
                        var rightIsNull = notEqualsExpression.Right == null || (right != null && right.Kind() == SyntaxKind.NullLiteralExpression);
                        var leftIsTheLoopVariable = left != null && left.Identifier.Text == loopVariable.Name;
                        if (leftIsTheLoopVariable && rightIsNull)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(Rule, i.GetLocation()));
                        }
                    }
                }
            }
        }
    }
}
