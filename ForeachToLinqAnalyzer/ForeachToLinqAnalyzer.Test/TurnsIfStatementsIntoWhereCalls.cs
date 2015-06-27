﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using TestHelper;
using ForeachToLinqAnalyzer;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace ForeachToLinqAnalyzer.Test
{
    [TestFixture]
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
        [Test]
        public void TestMethod1()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [Test]
        public void TestMethod2()
        {
            var testCode = string.Format(codeTemplate, "");

            var diagnostics = GetDiagnostics(testCode);

            Assert.AreEqual(0, diagnostics.Length);
        }

        [Test]
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

        [Test]
        public void FixDoesntBreakWhenLoopVariableIsUsedTwiceInIfClause()
        {
            var testCode = string.Format(codeTemplate, @"
            foreach (var foo in bar)
            {
                if (foo != null && foo.Count != 3)
                {
                    foo.Frombulate();
                }
            }");

            var fixedCode = string.Format(codeTemplate, @"
            foreach (var foo in bar.Where(x => x != null && x.Count != 3))
            {
                foo.Frombulate();
            }");

            VerifyCSharpFix(testCode, fixedCode);
        }

        [Test]
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

        [Test]
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

        [Test]
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
        
        [Test]
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

        [Test]
        public void SuggestsForInitialIfThenContinueStatement()
        {
            var testCode = string.Format(codeTemplate, @"
            foreach (var foo in bar)
            {
                if (foo.Count == 3)
                {
                    continue;
                }
                // carry on if Count != 3
                foo.Frombulate();
            }");
            var diagnostics = GetDiagnostics(testCode);

            Assert.AreEqual(1, diagnostics.Length);
            Assert.AreEqual("foo.Count == 3", GetDiagnosticCode(testCode, diagnostics.Single()));

            var fixedCode = string.Format(codeTemplate, @"
            foreach (var foo in bar.Where(x => !(x.Count == 3)))
            {
                // carry on if Count != 3
                foo.Frombulate();
            }");

            VerifyCSharpFix(testCode, fixedCode);
        }

        [Test]
        public void DoesntSuggestForWritelineWithoutBlock()
        {
            var testCode = string.Format(codeTemplate, @"
            foreach (var foo in bar) 
                Console.WriteLine(""fizz or buzz or something"");
            ");
            var diagnostics = GetDiagnostics(testCode);

            Assert.AreEqual(0, diagnostics.Length);
        }

        [Test]
        public void DoesntSuggestWhenContainingIfStatementHasElseBlock()
        {
            var testCode = string.Format(codeTemplate, @"
            foreach (var foo in bar) 
            {
                if (foo.Count == 3) 
                {
                    foo.Frombulate();
                }
                else
                {
                    Console.WriteLine(""Discombobulate instead"");
                }
            }");
            var diagnostics = GetDiagnostics(testCode);

            Assert.AreEqual(0, diagnostics.Length);
        }

        [Test]
        public void SuggestsForIfContinueElseDoSomething()
        {
            var testCode = string.Format(codeTemplate, @"
            foreach (var foo in bar)
            {
                if (foo.Count == 3)
                {
                    continue;
                }
                else
                {
                    Console.WriteLine(""Discombobulate instead"");
                }
                Console.WriteLine(""Do another thing"");
            }");
            var diagnostics = GetDiagnostics(testCode);

            Assert.AreEqual(1, diagnostics.Length);
            Assert.AreEqual("foo.Count == 3", GetDiagnosticCode(testCode, diagnostics.Single()));

            // TODO: inline redundant block
            var fixedCode = string.Format(codeTemplate, @"
            foreach (var foo in bar.Where(x => !(x.Count == 3)))
            {
                {
                    Console.WriteLine(""Discombobulate instead"");
                }
                Console.WriteLine(""Do another thing"");
            }");

            VerifyCSharpFix(testCode, fixedCode); ;
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
                else
                {
                    continue;
                }

                var foo2 = foo.Replace("asdf", "asdfasdf");
            }
        }
    }
}
