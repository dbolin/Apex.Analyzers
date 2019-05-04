using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using Apex.Analyzers.Immutable;

namespace Apex.Analyzers.Immutable.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
    {

        //No diagnostics expected to show up
        [TestMethod]
        public void Empty()
        {
            var test = @"namespace Test { public class Program { public static void Main() {} }}";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void IMM001MemberFieldNotReadonly()
        {
            var test = GetCode(@"
        [Immutable]
        class Test
        {
            private int x;
        }
");
            var expected = new DiagnosticResult
            {
                Id = "IMM001",
                Message = "Field 'x' is not declared as readonly",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 15, 25)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtest = test.Replace("private int x", "private readonly int x");
            VerifyCSharpFix(test, fixtest);
        }

        [TestMethod]
        public void IMM001MemberFieldReadonly()
        {
            var test = GetCode(@"
        [Immutable]
        class Test
        {
            private readonly int x;
        }
");
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void IMM001StaticFieldNotReadonly()
        {
            var test = GetCode(@"
        [Immutable]
        class Test
        {
            private static int x;
        }
");
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void IMM001ConstField()
        {
            var test = GetCode(@"
        [Immutable]
        class Test
        {
            private const int x = 1;
        }
");
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void IMM002MemberPropNotReadonly()
        {
            var test = GetCode(@"
        [Immutable]
        class Test
        {
            private int x {get; set;}
        }
");
            var expected = new DiagnosticResult
            {
                Id = "IMM002",
                Message = "Property 'x' defines a set method",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 15, 25)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtest = test.Replace("private int x {get; set;}", "private int x {get; }");
            VerifyCSharpFix(test, fixtest);
        }

        [TestMethod]
        public void IMM002MemberPropReadonly()
        {
            var test = GetCode(@"
        [Immutable]
        class Test
        {
            private int x {get;}
        }
");
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void IMM002StaticPropNotReadonly()
        {
            var test = GetCode(@"
        [Immutable]
        class Test
        {
            private static int x {get; set;}
        }
");
            VerifyCSharpDiagnostic(test);
        }

        private string GetCode(string code)
        {
            return @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
" + code + @"

        class Program
        {   
            public static void Main() {}
        }
    }";
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new ApexAnalyzersImmutableCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ApexAnalyzersImmutableAnalyzer();
        }
    }
}
