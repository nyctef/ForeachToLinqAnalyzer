using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using ForeachToLinqAnalyzer;

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
            void Foo() {{
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
            var testCode = string.Format(codeTemplate,
@"foreach (var foo in bar) {
    if (bar != null) {
        bar.Frombulate();
    }
}");
            var diagnostics = GetDiagnostics(testCode);
            Assert.AreEqual(1, diagnostics.Length);

            var fixedCode = string.Format(codeTemplate,
@"foreach (var foo in bar.Where(x => x != null)) {
    bar.Frombulate();
}");

            VerifyCSharpFix(testCode, fixedCode);
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