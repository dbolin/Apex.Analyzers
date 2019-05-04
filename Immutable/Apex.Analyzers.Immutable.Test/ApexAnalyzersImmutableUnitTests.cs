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
        public void IMM002MemberPropNotReadonlyNotAuto()
        {
            var test = GetCode(@"
        [Immutable]
        class Test
        {
            private int x {get => 1; set {} }
        }
");
            VerifyCSharpDiagnostic(test);
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

        [TestMethod]
        public void IMM003MemberFieldsWhitelisted()
        {
            var test = GetCode(@"
        enum TestEnum {
            A
        }
        [Immutable]
        class Test
        {
            private readonly TestEnum x;
            private readonly byte a;
            private readonly char b;
            private readonly sbyte c;
            private readonly short d;
            private readonly ushort e;
            private readonly int f;
            private readonly uint g;
            private readonly long h;
            private readonly ulong i;
            private readonly string j;
            private readonly DateTime k;
            private readonly float l;
            private readonly double m;
            private readonly decimal n;
            private readonly object o;
        }
");
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void IMM003MemberFieldsImmutable()
        {
            var test = GetCode(@"
        [Immutable]
        class TestI {
        }
        [Immutable]
        class Test
        {
            private readonly TestI x;
        }
");
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void IMM003MemberFieldsNotImmutable()
        {
            var test = GetCode(@"
        class TestI {
        }
        [Immutable]
        class Test
        {
            private readonly TestI x;
        }
");
            var expected = new DiagnosticResult
            {
                Id = "IMM003",
                Message = "Type of field 'x' is not immutable",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 17, 36)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void IMM004MemberPropsWhitelisted()
        {
            var test = GetCode(@"
        enum TestEnum {
            A
        }
        [Immutable]
        class Test
        {
            private TestEnum x {get;}
            private byte a {get;}
            private char b {get;}
            private sbyte c {get;}
            private short d {get;}
            private ushort e {get;}
            private int f {get;}
            private uint g {get;}
            private long h {get;}
            private ulong i {get;}
            private string j {get;}
            private DateTime k {get;}
            private float l {get;}
            private double m {get;}
            private decimal n {get;}
            private object o {get;}
        }
");
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void IMM004MemberPropsImmutable()
        {
            var test = GetCode(@"
        [Immutable]
        class TestI {
        }
        [Immutable]
        class Test
        {
            private TestI x {get;}
        }
");
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void IMM004MemberPropsNotImmutable()
        {
            var test = GetCode(@"
        class TestI {
        }
        [Immutable]
        class Test
        {
            private TestI x {get; }
        }
");
            var expected = new DiagnosticResult
            {
                Id = "IMM004",
                Message = "Type of auto property 'x' is not immutable",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 17, 27)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void IMM005NormalConstructor()
        {
            var test = GetCode(@"
        [Immutable]
        class Test
        {
            private readonly int x;
            public Test()
            {
                x = 5;
                this.x = 6;
            }
        }
");
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void IMM005NormalConstructor2()
        {
            var test = GetCode(@"
        [Immutable]
        class Test
        {
            public static void Method(Test t)
            {}
            public static Test Instance = new Test();
            private readonly int x;
            public Test()
            {
                Method(Instance);
            }
        }
");
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void IMM005MethodCallExplicitThisParamInConstructor()
        {
            var test = GetCode(@"
        [Immutable]
        class Test
        {
            public static void Method(Test t)
            {}

            private readonly int x;
            Test()
            {
                Method(this);
                x = 5;
            }
        }
");
            var expected = new DiagnosticResult
            {
                Id = "IMM005",
                Message = "Possibly incorrect usage of 'this' in the constructor of an immutable type",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 21, 24)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void IMM005MethodCallIndirectThisParamInConstructor()
        {
            var test = GetCode(@"
        [Immutable]
        class Test
        {
            public static void Method(Test t)
            {}

            private readonly int x;
            Test()
            {
                var asd = this;
                Method(asd);
                x = 5;
            }
        }
");
            var expected = new DiagnosticResult
            {
                Id = "IMM005",
                Message = "Possibly incorrect usage of 'this' in the constructor of an immutable type",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 21, 27)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void IMM005AssignThisToStaticInConstructor()
        {
            var test = GetCode(@"
        [Immutable]
        class Test
        {
            public static Test asd;
            private readonly int x;
            Test()
            {
                asd = this;
                x = 5;
            }
        }
");
            var expected = new DiagnosticResult
            {
                Id = "IMM005",
                Message = "Possibly incorrect usage of 'this' in the constructor of an immutable type",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 19, 23)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void IMM005CaptureThisInConstructor()
        {
            var test = GetCode(@"
        [Immutable]
        class Test
        {
            public static void Method(Func<int> t)
            {
                t();
            }

            private readonly int x;
            Test()
            {
                Method(() => this.x);
                x = 5;
            }
        }
");
            var expected = new DiagnosticResult
            {
                Id = "IMM005",
                Message = "Possibly incorrect usage of 'this' in the constructor of an immutable type",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 23, 24)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
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

        [TestMethod]
        public void IMM006BaseTypeObject()
        {
            var test = GetCode(@"
        [Immutable]
        class Test : object
        {
        }
");
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void IMM006BaseTypeImmutable()
        {
            var test = GetCode(@"
        [Immutable]
        class Test1
        {
        }

        [Immutable]
        class Test2 : Test1
        {
        }
");
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void IMM006BaseTypeNotImmutable()
        {
            var test = GetCode(@"
        class Test1
        {
        }

        [Immutable]
        class Test2 : Test1
        {
        }
");
            var expected = new DiagnosticResult
            {
                Id = "IMM006",
                Message = "Type 'Test2' base type must be 'object' or immutable",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 17, 15)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void IMM007DerivedTypeNotImmutable()
        {
            var test = GetCode(@"
        [Immutable]
        class Test1
        {
        }

        class Test2 : Test1
        {
        }
");
            var expected = new DiagnosticResult
            {
                Id = "IMM007",
                Message = "Type 'Test2' must be immutable because it derives from 'Test1'",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 17, 15)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void IMM007DerivedFromInterfaceTypeNotImmutable()
        {
            var test = GetCode(@"
        [Immutable]
        interface Test1
        {
        }

        class Test2 : Test1
        {
        }
");
            var expected = new DiagnosticResult
            {
                Id = "IMM007",
                Message = "Type 'Test2' must be immutable because it derives from 'Test1'",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 17, 15)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
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
