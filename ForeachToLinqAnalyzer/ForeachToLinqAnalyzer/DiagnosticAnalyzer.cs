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
            context.RegisterSyntaxTreeAction(AnalyzeSymbol);
        }

        internal static string ContainingIf = "--ContainingIf";
        internal static string IfWithContinue = "--IfWithContinue";
        internal static string IfType = "--IfType";

        private static async void AnalyzeSymbol(SyntaxTreeAnalysisContext context)
        {
            var cancel = context.CancellationToken;
            var tree = await context.Tree.GetRootAsync(cancel);
            var foreachs = tree.DescendantNodes().OfType<ForEachStatementSyntax>().ToList();
            foreach (var fe in foreachs)
            {
                var loopVariableName = fe.Identifier.Text;

                IfStatementSyntax ifStatement;
                string ifType;

                if (TrySearchForIfThenContinueStatement(fe, out ifStatement, out ifType) ||
                    TrySearchForSingleIfStatement(fe, out ifStatement, out ifType))
                {
                    var ifExpression = ifStatement.Condition;
                    var identifiersInExpression = ifExpression.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>();
                    var containsLoopVariable = identifiersInExpression.Any(x => x.Identifier.Text == loopVariableName);
                    if (containsLoopVariable)
                    {
                        var properties = new Dictionary<string, string> { { IfType, ifType } }.ToImmutableDictionary();
                        context.ReportDiagnostic(Diagnostic.Create(Rule, ifStatement.Condition.GetLocation(), properties));
                    }
                }
            }
        }

        private static bool TrySearchForIfThenContinueStatement(ForEachStatementSyntax fe, out IfStatementSyntax ifStatement, out string ifType)
        {
            if (fe.Statement is BlockSyntax)
            {
                var block = ((BlockSyntax)fe.Statement);
                if (block.Statements.Count > 1 && block.Statements.First() is IfStatementSyntax)
                {
                    ifStatement = (block.Statements.FirstOrDefault() as IfStatementSyntax);
                    if (ifStatement?.Statement is ContinueStatementSyntax || 
                        ((ifStatement?.Statement as BlockSyntax)?.Statements)?.FirstOrDefault() is ContinueStatementSyntax)
                    {
                        ifType = IfWithContinue;
                        return true;
                    }
                }
            }

            ifStatement = null;
            ifType = null;
            return false;
        }

        private static bool TrySearchForSingleIfStatement(ForEachStatementSyntax fe, out IfStatementSyntax ifStatement, out string ifType)
        {
            if (fe.Statement is IfStatementSyntax)
            {
                ifType = ContainingIf;
                ifStatement = ((IfStatementSyntax)fe.Statement);
                return true;
            }

            var block = fe.Statement as BlockSyntax;
            if ((block?.Statements)?.Count == 1 && (block?.Statements)?.Single() is IfStatementSyntax)
            {
                ifStatement = ((IfStatementSyntax)block.Statements.Single());
                ifType = ContainingIf;
                return true;
            }

            ifStatement = null;
            ifType = null;
            return false;
        }
    }
}
