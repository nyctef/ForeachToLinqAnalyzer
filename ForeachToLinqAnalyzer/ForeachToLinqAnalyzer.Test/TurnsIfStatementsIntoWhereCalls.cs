using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using ForeachToLinqAnalyzer;
using System.Collections.Generic;
using System.Linq;

namespace ForeachToLinqAnalyzer.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
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

        class Bar {{ public void Frombulate() {{ }} public int Count {{ get; }} }}

        void Foo() 
        {{
            var bar = new List<Bar>();
{0}
        }}
    }}
}}";
        //No diagnostics expected to show up
        [TestMethod]
        public void TestMethod1()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public void TestMethod2()
        {
            var testCode = string.Format(codeTemplate, "");

            var diagnostics = GetDiagnostics(testCode);

            Assert.AreEqual(0, diagnostics.Length);
        }

        [TestMethod]
        public void SuggestsWhereWhenBodyOfForeachHasNullCheck()
        {
            var testCode = string.Format(codeTemplate, @"
            foreach (var foo in bar) 
            {
                if (foo != null) 
                {
                    foo.Frombulate();
                }
            }");
            var diagnostics = GetDiagnostics(testCode);

            Assert.AreEqual(1, diagnostics.Length);
            Assert.AreEqual("foo != null", GetDiagnosticCode(testCode, diagnostics.Single()));

            var fixedCode = string.Format(codeTemplate, @"
            foreach (var foo in bar.Where(x => x != null))
            {
                foo.Frombulate();
            }");

            VerifyCSharpFix(testCode, fixedCode);
        }

        [TestMethod]
        public void CanOperateOnComplexForeachExpression()
        {
            var testCode = string.Format(codeTemplate, @"
            foreach (var foo in bar.OfType<object>()) 
            {
                if (foo != null) 
                {
                    foo.Frombulate();
                }
            }");
            var fixedCode = string.Format(codeTemplate, @"
            foreach (var foo in bar.OfType<object>().Where(x => x != null))
            {
                foo.Frombulate();
            }");

            VerifyCSharpFix(testCode, fixedCode);
        }

        [TestMethod]
        public void SuggestsWhenBodyOfForeachHasSimpleEqualityCheck()
        {
            var testCode = string.Format(codeTemplate, @"
            foreach (var foo in bar) 
            {
                if (foo.Count != 3) 
                {
                    foo.Frombulate();
                }
            }");
            var diagnostics = GetDiagnostics(testCode);

            Assert.AreEqual(1, diagnostics.Length);
            Assert.AreEqual("foo.Count != 3", GetDiagnosticCode(testCode, diagnostics.Single()));

            var fixedCode = string.Format(codeTemplate, @"
            foreach (var foo in bar.Where(x => x.Count != 3))
            {
                foo.Frombulate();
            }");

            VerifyCSharpFix(testCode, fixedCode);
        }

        [TestMethod]
        public void DoesntSuggestWhenBodyOfForeachHasStatementsOutsideOfIf()
        {
            var testCode = string.Format(codeTemplate, @"
            foreach (var foo in bar) 
            {
                if (foo.Count != 3) 
                {
                    foo.Frombulate();
                }
                Console.WriteLine(foo);
            }");
            var diagnostics = GetDiagnostics(testCode);

            Assert.AreEqual(0, diagnostics.Length);
        }

        [TestMethod]
        public void DoesntSuggestWhenIfStatementIsUnrelated()
        {
            var testCode = string.Format(codeTemplate, @"
            foreach (var foo in bar) 
            {
                if (1 == 1) 
                {
                    foo.Frombulate();
                }
            }");
            var diagnostics = GetDiagnostics(testCode);

            Assert.AreEqual(0, diagnostics.Length);
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

        void ScratchPad()
        {
            var bar = new List<string>();
            foreach (var foo in bar)
            {
                if (foo != null)
                {
                    Console.WriteLine(foo);
                }
            }
        }
    }
}