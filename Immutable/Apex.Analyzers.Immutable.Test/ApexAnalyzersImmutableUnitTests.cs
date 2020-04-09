using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using TestHelper;
using Apex.Analyzers.Immutable;
using Xunit;

namespace Apex.Analyzers.Immutable.Test
{
    public class UnitTest
    {

        //No diagnostics expected to show up
        [Fact]
        public void Empty()
        {
            var test = @"namespace Test { public class Program { public static void Main() {} }}";

            VerifyCSharpDiagnostic(test);
        }

        [Fact]
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
                            new DiagnosticResultLocation("Test0.cs", 16, 25)
                        }
            };

            var fixtest = test.Replace("private int x", "private readonly int x");

            VerifyCSharpFix(test, new[] { expected }, fixtest);
        }

        [Fact]
        public void IMM001MemberFieldNotReadonlyNonSerialized()
        {
            var test = GetCode(@"
        [Immutable]
        class Test
        {
            [NonSerialized]
            private int x;
        }
");
            VerifyCSharpDiagnostic(test);
        }

        [Fact]
        public void IMM001MemberFieldNotReadonlyNonSerializedPublic()
        {
            var test = GetCode(@"
        [Immutable]
        class Test
        {
            [NonSerialized]
            public int x;
        }
");

            var expected = new DiagnosticResult
            {
                Id = "IMM001",
                Message = "Field 'x' is not declared as readonly",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 17, 24)
                        }
            };

            var fixtest = test.Replace("public int x", "public readonly int x");
            VerifyCSharpFix(test, new[] { expected }, fixtest);
        }

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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
                            new DiagnosticResultLocation("Test0.cs", 16, 25)
                        }
            };

            var fixtest = test.Replace("private int x {get; set;}", "private int x {get; }");
            VerifyCSharpFix(test, new[] { expected }, fixtest);
        }

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
        public void IMM003MemberFieldsWhitelistedByConvention()
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
            private readonly Guid p;
            private readonly TimeSpan q;
            private readonly DateTimeOffset r;
            private readonly int? s;
            private readonly KeyValuePair<int, string> t;
        }
");
            VerifyCSharpDiagnostic(test);
        }

         [Fact]
        public void IMM003MemberFieldsWhitelistedByConfiguration()
        {
            var code = GetCode(@"
        [Immutable]
        class Test
        {
            private readonly Func<int> a;
        }
");
            
            string whitelist = $"System.Func`1";
            var test = new CSharpCodeFixVerifier<ApexAnalyzersImmutableAnalyzer, ApexAnalyzersImmutableCodeFixProvider>.Test();
            test.TestCode = code;
            test.AdditionalFiles.Add(new AdditionalFile("ImmutableTypes.txt", whitelist));
            test.Run();
        }

        [Fact]
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

        [Fact]
        public void IMM003MemberFieldsImmutableNamespace()
        {
            var test = GetCode(@"
        [Immutable]
        class Test
        {
            private readonly ImmutableArray<int> x;
        }
");
            VerifyCSharpDiagnostic(test);
        }

        [Fact]
        public void IMM003MemberFieldsGeneric()
        {
            var test = GetCode(@"
        [Immutable]
        class Test<T>
        {
            private readonly T x;
        }
");
            VerifyCSharpDiagnostic(test);
        }

        [Fact]
        public void IMM003MemberFieldsGenericNotImmutableConcrete()
        {
            var test = GetCode(@"
    public class MutableClass
    {
    }

    [Immutable]
    public class Class1<T>
    {
        private readonly int x;
        private readonly T Value;
    }

    [Immutable]
    public class Test
    {
        private readonly Class1<MutableClass> TestValue;
    }
");
            var expected = new DiagnosticResult
            {
                Id = "IMM003",
                Message = "Type of field 'TestValue' is not immutable because type argument 'MutableClass' is not immutable",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 27, 47)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [Fact]
        public void IMM003MemberFieldsNotImmutableNestedInGenericOnFaith_should_not_validate_non_type_arguments()
        {
            var test = GetCode(@"
    [Immutable(onFaith: true)]
    public class Class1<T>
    {
        public class MutableClass
        {
        }

        private readonly int x;
        private readonly MutableClass Value;
    }

    [Immutable]
    public class Test
    {
        private readonly Class1<int> TestValue;
    }
");
            VerifyCSharpDiagnostic(test);
        }

        [Fact]
        public void IMM003MemberFieldsNotImmutableNestedInGenericOnFaith_should_validate_type_arguments()
        {
            var test = GetCode(@"
    public class MutableClass
    {
    }

    [Immutable(onFaith: true)]
    public class Class1<T>
    {
        private readonly int x;
        private readonly T Value;
    }

    [Immutable]
    public class Test
    {
        private readonly Class1<MutableClass> TestValue;
    }
");
            var expected = new DiagnosticResult
            {
                Id = "IMM003",
                Message = "Type of field 'TestValue' is not immutable because type argument 'MutableClass' is not immutable",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 27, 47)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [Fact]
        public void IMM003MemberFieldsNestedGenericNotImmutableConcrete()
        {
            var test = GetCode(@"
    public class MutableClass
    {
    }

    [Immutable]
    public class Class1<T>
    {
        private readonly int x;
        private readonly T Value;
    }

    [Immutable]
    public class Test
    {
        private readonly Class1<Class1<MutableClass>> TestValue;
    }
");
            var expected = new DiagnosticResult
            {
                Id = "IMM003",
                Message = "Type of field 'TestValue' is not immutable because type argument 'MutableClass' is not immutable",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 27, 55)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [Fact]
        public void IMM003MemberFieldsGenericFromSystemNotImmutableConcrete()
        {
            var test = GetCode(@"
    public class MutableClass
    {
    }

    [Immutable]
    public class Test
    {
        private readonly ImmutableSortedDictionary<MutableClass, int> TestValue;
    }
");
            var expected = new DiagnosticResult
            {
                Id = "IMM003",
                Message = "Type of field 'TestValue' is not immutable because type argument 'MutableClass' is not immutable",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 20, 71)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [Fact]
        public void IMM003MemberFieldsGenericNotImmutableConcretePropogation()
        {
            var test = GetCode(@"
    public class MutableClass
    {
    }

    [Immutable]
    public class ImmutableTuple<T>
    {
        public readonly T Value;
    }

    [Immutable]
    public class Class1<T>
    {
        private readonly int x;
        private readonly ImmutableTuple<T> Value;
    }

    [Immutable]
    public class Test
    {
        private readonly Class1<MutableClass> TestValue;
    }
");
            var expected = new DiagnosticResult
            {
                Id = "IMM003",
                Message = "Type of field 'TestValue' is not immutable because type argument 'MutableClass' is not immutable",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 33, 47)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [Fact]
        public void IMM003MemberFieldsGenericNotImmutableConcretePropogationLoop()
        {
            var test = GetCode(@"
    public class MutableClass
    {
    }

    [Immutable]
    public class ImmutableTuple<T>
    {
        public readonly T Value;
    }

    [Immutable]
    public class Class2<T>
    {
        private readonly int x;
        private readonly ImmutableTuple<Class1<T>> Value;
        private readonly ImmutableTuple<T> Value2;
    }

    [Immutable]
    public class Class1<T>
    {
        private readonly int x;
        private readonly ImmutableTuple<Class2<T>> Value;
    }

    [Immutable]
    public class Test
    {
        private readonly Class1<MutableClass> TestValue;
    }
");
            var expected = new DiagnosticResult
            {
                Id = "IMM003",
                Message = "Type of field 'TestValue' is not immutable because type argument 'MutableClass' is not immutable",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 41, 47)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [Fact]
        public void IMM003MemberFieldsNotImmutable()
        {
            var test = GetCode(@"
        class TestI {
        }
        [Immutable]
        class Test
        {
            private readonly TestI x;
            private readonly KeyValuePair<int, TestI> y;
        }
");
            var expected1 = new DiagnosticResult
            {
                Id = "IMM003",
                Message = "Type of field 'x' is not immutable",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 18, 36)
                        }
            };

            var expected2 = new DiagnosticResult
            {
                Id = "IMM003",
                Message = "Type of field 'y' is not immutable because type argument 'TestI' is not immutable",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 19, 55)
                        }
            };

            VerifyCSharpDiagnostic(test, expected1, expected2);
        }

        [Fact]
        public void IMM004MemberPropsWhitelistedByConvention()
        {
            var test = GetCode(@"
        public enum TestEnum {
            A
        }
        [Immutable]
        public sealed class Test
        {
            public TestEnum x {get;}
            public byte a {get;}
            public char b {get;}
            public sbyte c {get;}
            public short d {get;}
            public ushort e {get;}
            public int f {get;}
            public uint g {get;}
            public long h {get;}
            public ulong i {get;}
            public string j {get;}
            public DateTime k {get;}
            public float l {get;}
            public double m {get;}
            public decimal n {get;}
            public object o {get;}
            public Guid p {get;}
            public TimeSpan q {get;}
            public DateTimeOffset r {get;}
            public int? s {get;}
        }
");
            VerifyCSharpDiagnostic(test);
        }

        [Fact]
        public void IMM004MemberPropsWhitelistedByConfiguration()
        {
            var code = GetCode(@"
        [Immutable]
        class Test
        {
            public Action a {get;}
        }
");

            string whitelist = $"System.Action";
            var test = new CSharpCodeFixVerifier<ApexAnalyzersImmutableAnalyzer, ApexAnalyzersImmutableCodeFixProvider>.Test();
            test.TestCode = code;
            test.AdditionalFiles.Add(new AdditionalFile("ImmutableTypes.txt", whitelist));
            test.Run();
        }

        [Fact]
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

        [Fact]
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
                            new DiagnosticResultLocation("Test0.cs", 18, 27)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [Fact]
        public void IMM004MemberPropsGenericNotImmutable()
        {
            var test = GetCode(@"
        class TestI {
        }
        [Immutable]
        class Test
        {
            private ImmutableDictionary<Guid, TestI> x {get; }
        }
");
            var expected = new DiagnosticResult
            {
                Id = "IMM004",
                Message = "Type of auto property 'x' is not immutable because type argument 'TestI' is not immutable",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 18, 54)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [Fact]
        public void IMM004MemberPropsGenericImmutable()
        {
            var test = GetCode(@"
        [Immutable]
        class TestI {
        }
        [Immutable]
        class Test
        {
            private ImmutableArray<TestI>? x {get; }
        }
");
            VerifyCSharpDiagnostic(test);
        }

        [Fact]
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

        [Fact]
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

        [Fact]
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
                            new DiagnosticResultLocation("Test0.cs", 22, 24)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [Fact]
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
                            new DiagnosticResultLocation("Test0.cs", 22, 27)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [Fact]
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
                            new DiagnosticResultLocation("Test0.cs", 20, 23)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [Fact]
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
                            new DiagnosticResultLocation("Test0.cs", 24, 24)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [Fact]
        public void IMM005CaptureThisInConstructorAllowed()
        {
            var test = GetCode(@"
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
            VerifyCSharpDiagnostic(test);
        }

        [Fact]
        public void IMM006BaseTypeStruct()
        {
            var test = GetCode(@"
        [Immutable]
        struct Test
        {
        }
");
            VerifyCSharpDiagnostic(test);
        }

        [Fact]
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

        [Fact]
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

        [Fact]
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
                            new DiagnosticResultLocation("Test0.cs", 18, 15)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [Fact]
        public void IMM006BaseTypeNotImmutableGeneric()
        {
            var test = GetCode(@"
        class MutableClass {
            public int X;
        }
        [Immutable]
        class Test1<T>
        {
            public readonly T Value;
        }

        [Immutable]
        class Test2 : Test1<MutableClass>
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
                            new DiagnosticResultLocation("Test0.cs", 23, 15)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [Fact]
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
                            new DiagnosticResultLocation("Test0.cs", 18, 15)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [Fact]
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
                            new DiagnosticResultLocation("Test0.cs", 18, 15)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        private void VerifyCSharpDiagnostic(string source, params DiagnosticResult[] expected)
        {
            CSharpCodeFixVerifier<ApexAnalyzersImmutableAnalyzer, ApexAnalyzersImmutableCodeFixProvider>.VerifyAnalyzer(source, expected);
        }

        private void VerifyCSharpFix(string oldSource, DiagnosticResult[] expected, string newSource)
        {
            CSharpCodeFixVerifier<ApexAnalyzersImmutableAnalyzer, ApexAnalyzersImmutableCodeFixProvider>.VerifyCodeFix(oldSource, expected, newSource);
        }

        private string GetCode(string code, string namesp = "ConsoleApplication1")
        {
            return @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using System.Collections.Immutable;

    namespace " + namesp + @"
    {
" + code + @"

        class Program
        {   
            public static void Main() {}
        }
    }";
        }

    }
}
