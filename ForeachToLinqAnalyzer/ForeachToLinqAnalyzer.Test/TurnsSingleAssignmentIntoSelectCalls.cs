using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace ForeachToLinqAnalyzer.Test
{
    [TestClass]
    public class TurnsSingleAssignmentIntoSelectCalls : CodeFixVerifier
    {
        private readonly string codeTemplate = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ConsoleApplication1
{{
    class TypeName
    {{

        class Bar 
        {{ 
            public void Frombulate() {{ }} 
            public int Count {{ get; set; }} 
            public Bar Increment() {{ return new Bar {{ Count = Count + 1 }}; }} 
        }}

        void Foo() 
        {{
            var bar = new List<Bar>();
{0}
        }}
    }}
}}";

        [TestMethod]
        public void SuggestsForInitialVariableAssignment()
        {
            var testCode = string.Format(codeTemplate, @"
            foreach (var foo in bar) 
            {
                var foo2 = foo.Increment();
                foo2.Frombulate();
            }");
            var diagnostics = GetDiagnostics(testCode);

            Assert.AreEqual(1, diagnostics.Length);
            Assert.AreEqual("foo2 = foo.Increment()", GetDiagnosticCode(testCode, diagnostics.Single()));

            var fixedCode = string.Format(codeTemplate, @"
            foreach (var foo2 in bar.Select(x => x.Increment()))
            {
                foo2.Frombulate();
            }");

            VerifyCSharpFix(testCode, fixedCode);
        }

        [TestMethod]
        public void DoesntBreakMultipleDeclaratorsInAssignment()
        {
            var testCode = string.Format(codeTemplate, @"
            foreach (var foo in bar) 
            {
                var foo2 = foo.Increment(), foo3 = new Bar();
                foo2.Frombulate();
            }");

            var fixedCode = string.Format(codeTemplate, @"
            foreach (var foo2 in bar.Select(x => x.Increment()))
            {
                var foo3 = new Bar();
                foo2.Frombulate();
            }");

            VerifyCSharpFix(testCode, fixedCode);
        }

        [TestMethod]
        public void DoesntSuggestIfLoopVariableIsUsedLater()
        {
            var testCode = string.Format(codeTemplate, @"
            foreach (var foo in bar) 
            {
                var foo2 = foo.Increment();
                foo.Frombulate();
            }");
            var diagnostics = GetDiagnostics(testCode);

            Assert.AreEqual(0, diagnostics.Length);
        }

        [TestMethod]
        public void DoesntBreakOnIncompleteCode()
        {
            var testCode = string.Format(codeTemplate, @"
            foreach (var foo in bar) 
            {
                var foo2
                if (foo != null) { }
            }");
            var diagnostics = GetDiagnostics(testCode);

            Assert.AreEqual(0, diagnostics.Length);
        }

        [TestMethod]
        public void LoopVariableCanBeUsedMoreThanOnceInOneRValue()
        {
            Assert.Inconclusive();
        }

        [TestMethod]
        public void LoopVariableCanNotBeUsedInMoreThanOneRValue()
        {
            Assert.Inconclusive();
        }

        private string GetDiagnosticCode(string code, Diagnostic diagnostic)
        {
            var span = diagnostic.Location.SourceSpan;
            return code.Substring(span.Start, span.Length);
        }

        protected Diagnostic[] GetDiagnostics(string testCode)
        {
            return GetSortedDiagnostics(new[] { testCode }, LanguageNames.CSharp, GetCSharpDiagnosticAnalyzer());
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new ForeachToLinqCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ForeachToLinqAnalyzer();
        }
    }
}
