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
        public const string DiagnosticId = "ForeachToLinq";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        internal static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        internal const string Category = "Naming";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSemanticModelAction(AnalyzeSymbol);
        }

        private static async void AnalyzeSymbol(SemanticModelAnalysisContext context)
        {
            var cancel = context.CancellationToken;
            var tree = await context.SemanticModel.SyntaxTree.GetRootAsync(cancel);
            var foreachs = tree.DescendantNodes().OfType<ForEachStatementSyntax>().ToList();
            foreach (var fe in foreachs)
            {
                var loopVariableName = fe.Identifier.Text;

                var ifs = fe.DescendantNodes().OfType<IfStatementSyntax>();

                foreach (var i in ifs)
                {
                    var ifExpression = i.Condition;
                    var notEqualsExpression = ifExpression as BinaryExpressionSyntax;
                    if (notEqualsExpression != null && notEqualsExpression.Kind() == SyntaxKind.NotEqualsExpression)
                    {
                        var identifiersInExpression = ifExpression.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>();
                        var containsLoopVariable = identifiersInExpression.Any(x => x.Identifier.Text == loopVariableName);
                        if (containsLoopVariable)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(Rule, i.Condition.GetLocation()));
                        }
                    }
                }
            }
        }
    }
}
