using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace TestHelper
{
    public static class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        public class Test
        {
            private static readonly MetadataReference CorlibReference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
            private static readonly MetadataReference SystemCoreReference = MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location);
            private static readonly MetadataReference CSharpSymbolsReference = MetadataReference.CreateFromFile(typeof(CSharpCompilation).Assembly.Location);
            private static readonly MetadataReference CodeAnalysisReference = MetadataReference.CreateFromFile(typeof(Compilation).Assembly.Location);
            private static readonly MetadataReference AttributeReference = MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location);
            private static readonly MetadataReference AnalyzerReference = MetadataReference.CreateFromFile(typeof(ImmutableAttribute).Assembly.Location);
            private static readonly MetadataReference ImmutableReference = MetadataReference.CreateFromFile(typeof(ImmutableArray<int>).Assembly.Location);
            private static readonly MetadataReference NetStandard2Reference = MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.0.0.0").Location);

            private static readonly string TestFileName = "Test0.cs";
            private static readonly string TestProjectName = "TestProject";

            public string TestCode { get; internal set; }
            public string FixedCode { get; internal set; }
            public List<MetadataReference>  MetadataReferences { get; } = new List<MetadataReference>();
            public List<AdditionalFile> AdditionalFiles { get; } = new List<AdditionalFile>();
            public List<DiagnosticResult> ExpectedDiagnostics { get; } = new List<DiagnosticResult>();

            public Test() 
            {
                MetadataReferences.AddRange(new[]
                { 
                    CorlibReference,
                    SystemCoreReference,
                    CSharpSymbolsReference,
                    CodeAnalysisReference,
                    AnalyzerReference,
                    AttributeReference,
                    ImmutableReference,
                    NetStandard2Reference
                });
            }
            public void Run() 
            {
                var analyzer = new TAnalyzer();
                var fix = new TCodeFix();
                var actualResults = GetSortedDiagnostics(analyzer);
                VerifyDiagnosticResults(actualResults, analyzer, ExpectedDiagnostics.ToArray());

                if(FixedCode != null) 
                {
                    VerifyFix(analyzer, fix, TestCode, FixedCode);
                }
            }

            #region  Get Diagnostics

            /// <summary>
            /// Given classes in the form of strings, their language, and an IDiagnosticAnalyzer to apply to it, return the diagnostics found in the string after converting it to a document.
            /// </summary>
            /// <param name="analyzer">The analyzer to be run on the sources</param>
            /// <returns>An IEnumerable of Diagnostics that surfaced in the source code, sorted by Location</returns>
            private Diagnostic[] GetSortedDiagnostics(TAnalyzer analyzer)
            {
                return GetSortedDiagnosticsFromDocument(analyzer, GetDocument());
            }

            private Diagnostic[] GetSortedDiagnosticsFromDocument(TAnalyzer analyzer, Document document)
            {
                var diagnostics = new List<Diagnostic>();
                var compilation = document.Project.GetCompilationAsync().Result;
                var options = new AnalyzerOptions(AdditionalFiles.ToImmutableArray<AdditionalText>());
                var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer), options);
                var diags = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;
                diagnostics.AddRange(diags.Concat(compilation.GetDiagnostics().Where(x => x.Severity == DiagnosticSeverity.Error)));
                var results = SortDiagnostics(diagnostics);
                diagnostics.Clear();
                return results;
            }

            private static Diagnostic[] SortDiagnostics(IEnumerable<Diagnostic> diagnostics)
            {
                return diagnostics.OrderBy(d => d.Location.SourceSpan.Start).ToArray();
            }

            #endregion

            #region Set up compilation and documents
            private Document GetDocument()
            {
                var project = CreateProject();
                return project.Documents.First();

                Project CreateProject()
                {
                    var projectId = ProjectId.CreateNewId(debugName: TestProjectName);

                    var solution = new AdhocWorkspace()
                        .CurrentSolution
                        .AddProject(projectId, TestProjectName, TestProjectName, LanguageNames.CSharp)
                        .AddMetadataReferences(projectId, MetadataReferences)
                        .WithProjectCompilationOptions(projectId, new CSharpCompilationOptions(OutputKind.ConsoleApplication, allowUnsafe: true));

                    var documentId = DocumentId.CreateNewId(projectId, debugName: TestFileName);
                    var documentIdForInit = DocumentId.CreateNewId(projectId, debugName: "IsInitOnly.cs");
                    solution = solution.AddDocument(documentId, TestFileName, SourceText.From(TestCode))
                        .AddDocument(documentIdForInit, "IsInitOnly.cs", SourceText.From(@"namespace System.Runtime.CompilerServices
{
    public sealed class IsExternalInit
    {
    }
}"));
                    return solution.GetProject(projectId);
                }
            }
            #endregion

            #region Actual comparisons and verifications
            /// <summary>
            /// General verifier for codefixes.
            /// Creates a Document from the source string, then gets diagnostics on it and applies the relevant codefixes.
            /// Then gets the string after the codefix is applied and compares it with the expected result.
            /// Note: If any codefix causes new diagnostics to show up, the test fails unless allowNewCompilerDiagnostics is set to true.
            /// </summary>
            /// <param name="analyzer">The analyzer to be applied to the source code</param>
            /// <param name="codeFixProvider">The codefix to be applied to the code wherever the relevant Diagnostic is found</param>
            /// <param name="oldSource">A class in the form of a string before the CodeFix was applied to it</param>
            /// <param name="newSource">A class in the form of a string after the CodeFix was applied to it</param>
            private void VerifyFix(TAnalyzer analyzer, TCodeFix codeFixProvider, string oldSource, string newSource)
            {
                var document = GetDocument();
                var analyzerDiagnostics = GetSortedDiagnosticsFromDocument(analyzer, document);
                var compilerDiagnostics = GetCompilerDiagnostics(document);
                var attempts = analyzerDiagnostics.Length;

                for (int i = 0; i < attempts; ++i)
                {
                    var actions = new List<CodeAction>();
                    var context = new CodeFixContext(document, analyzerDiagnostics[0], (a, d) => actions.Add(a), CancellationToken.None);
                    codeFixProvider.RegisterCodeFixesAsync(context).Wait();

                    if (!actions.Any())
                    {
                        break;
                    }

                    document = ApplyFix(document, actions.ElementAt(0));
                    analyzerDiagnostics = GetSortedDiagnosticsFromDocument(analyzer, document);

                    //check if there are analyzer diagnostics left after the code fix
                    if (!analyzerDiagnostics.Any())
                    {
                        break;
                    }
                }

                //after applying all of the code fixes, compare the resulting string to the inputted one
                var actual = GetStringFromDocument(document);
                Assert.Equal(newSource, actual);
                
                Document ApplyFix(Document document, CodeAction codeAction)
                {
                    var operations = codeAction.GetOperationsAsync(CancellationToken.None).Result;
                    var solution = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;
                    return solution.GetDocument(document.Id);
                }

                string GetStringFromDocument(Document document)
                {
                    var simplifiedDoc = Simplifier.ReduceAsync(document, Simplifier.Annotation).Result;
                    var root = simplifiedDoc.GetSyntaxRootAsync().Result;
                    root = Formatter.Format(root, Formatter.Annotation, simplifiedDoc.Project.Solution.Workspace);
                    return root.GetText().ToString();
                }

                IEnumerable<Diagnostic> GetCompilerDiagnostics(Document document)
                {
                    return document.GetSemanticModelAsync().Result.GetDiagnostics();
                }
            }

            /// <summary>
            /// Checks each of the actual Diagnostics found and compares them with the corresponding DiagnosticResult in the array of expected results.
            /// Diagnostics are considered equal only if the DiagnosticResultLocation, Id, Severity, and Message of the DiagnosticResult match the actual diagnostic.
            /// </summary>
            /// <param name="actualResults">The Diagnostics found by the compiler after running the analyzer on the source code</param>
            /// <param name="analyzer">The analyzer that was being run on the sources</param>
            /// <param name="expectedResults">Diagnostic Results that should have appeared in the code</param>
            private static void VerifyDiagnosticResults(IEnumerable<Diagnostic> actualResults, DiagnosticAnalyzer analyzer, params DiagnosticResult[] expectedResults)
            {
                int expectedCount = expectedResults.Count();
                int actualCount = actualResults.Count();

                if (expectedCount != actualCount)
                {
                    string diagnosticsOutput = actualResults.Any() ? FormatDiagnostics(analyzer, actualResults.ToArray()) : "    NONE.";

                    Assert.True(false,
                        string.Format("Mismatch between number of diagnostics returned, expected \"{0}\" actual \"{1}\"\r\n\r\nDiagnostics:\r\n{2}\r\n", expectedCount, actualCount, diagnosticsOutput));
                }

                for (int i = 0; i < expectedResults.Length; i++)
                {
                    var actual = actualResults.ElementAt(i);
                    var expected = expectedResults[i];

                    if (expected.Line == -1 && expected.Column == -1)
                    {
                        if (actual.Location != Location.None)
                        {
                            Assert.True(false,
                                string.Format("Expected:\nA project diagnostic with No location\nActual:\n{0}",
                                FormatDiagnostics(analyzer, actual)));
                        }
                    }
                    else
                    {
                        VerifyDiagnosticLocation(analyzer, actual, actual.Location, expected.Locations.First());
                        var additionalLocations = actual.AdditionalLocations.ToArray();

                        if (additionalLocations.Length != expected.Locations.Length - 1)
                        {
                            Assert.True(false,
                                string.Format("Expected {0} additional locations but got {1} for Diagnostic:\r\n    {2}\r\n",
                                    expected.Locations.Length - 1, additionalLocations.Length,
                                    FormatDiagnostics(analyzer, actual)));
                        }

                        for (int j = 0; j < additionalLocations.Length; ++j)
                        {
                            VerifyDiagnosticLocation(analyzer, actual, additionalLocations[j], expected.Locations[j + 1]);
                        }
                    }

                    if (actual.Id != expected.Id)
                    {
                        Assert.True(false,
                            string.Format("Expected diagnostic id to be \"{0}\" was \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                                expected.Id, actual.Id, FormatDiagnostics(analyzer, actual)));
                    }

                    if (actual.Severity != expected.Severity)
                    {
                        Assert.True(false,
                            string.Format("Expected diagnostic severity to be \"{0}\" was \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                                expected.Severity, actual.Severity, FormatDiagnostics(analyzer, actual)));
                    }

                    if (actual.GetMessage() != expected.Message)
                    {
                        Assert.True(false,
                            string.Format("Expected diagnostic message to be \"{0}\" was \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                                expected.Message, actual.GetMessage(), FormatDiagnostics(analyzer, actual)));
                    }
                }
            }

            /// <summary>
            /// Helper method to VerifyDiagnosticResult that checks the location of a diagnostic and compares it with the location in the expected DiagnosticResult.
            /// </summary>
            /// <param name="analyzer">The analyzer that was being run on the sources</param>
            /// <param name="diagnostic">The diagnostic that was found in the code</param>
            /// <param name="actual">The Location of the Diagnostic found in the code</param>
            /// <param name="expected">The DiagnosticResultLocation that should have been found</param>
            private static void VerifyDiagnosticLocation(DiagnosticAnalyzer analyzer, Diagnostic diagnostic, Location actual, DiagnosticResultLocation expected)
            {
                var actualSpan = actual.GetLineSpan();

                Assert.True(actualSpan.Path == expected.Path || (actualSpan.Path != null && actualSpan.Path.Contains("Test0.") && expected.Path.Contains("Test.")),
                    string.Format("Expected diagnostic to be in file \"{0}\" was actually in file \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                        expected.Path, actualSpan.Path, FormatDiagnostics(analyzer, diagnostic)));

                var actualLinePosition = actualSpan.StartLinePosition;

                // Only check line position if there is an actual line in the real diagnostic
                if (actualLinePosition.Line > 0)
                {
                    if (actualLinePosition.Line + 1 != expected.Line)
                    {
                        Assert.True(false,
                            string.Format("Expected diagnostic to be on line \"{0}\" was actually on line \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                                expected.Line, actualLinePosition.Line + 1, FormatDiagnostics(analyzer, diagnostic)));
                    }
                }

                // Only check column position if there is an actual column position in the real diagnostic
                if (actualLinePosition.Character > 0)
                {
                    if (actualLinePosition.Character + 1 != expected.Column)
                    {
                        Assert.True(false,
                            string.Format("Expected diagnostic to start at column \"{0}\" was actually at column \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                                expected.Column, actualLinePosition.Character + 1, FormatDiagnostics(analyzer, diagnostic)));
                    }
                }
            }
            #endregion

            #region Formatting Diagnostics
            /// <summary>
            /// Helper method to format a Diagnostic into an easily readable string
            /// </summary>
            /// <param name="analyzer">The analyzer that this verifier tests</param>
            /// <param name="diagnostics">The Diagnostics to be formatted</param>
            /// <returns>The Diagnostics formatted as a string</returns>
            private static string FormatDiagnostics(DiagnosticAnalyzer analyzer, params Diagnostic[] diagnostics)
            {
                var builder = new StringBuilder();
                for (int i = 0; i < diagnostics.Length; ++i)
                {
                    builder.AppendLine("// " + diagnostics[i].ToString());

                    var analyzerType = analyzer.GetType();
                    var rules = analyzer.SupportedDiagnostics;

                    foreach (var rule in rules)
                    {
                        if (rule != null && rule.Id == diagnostics[i].Id)
                        {
                            var location = diagnostics[i].Location;
                            if (location == Location.None)
                            {
                                builder.AppendFormat("GetGlobalResult({0}.{1})", analyzerType.Name, rule.Id);
                            }
                            else
                            {
                                Assert.True(location.IsInSource,
                                    $"Test base does not currently handle diagnostics in metadata locations. Diagnostic in metadata: {diagnostics[i]}\r\n");

                                string resultMethodName = diagnostics[i].Location.SourceTree.FilePath.EndsWith(".cs") ? "GetCSharpResultAt" : "GetBasicResultAt";
                                var linePosition = diagnostics[i].Location.GetLineSpan().StartLinePosition;

                                builder.AppendFormat("{0}({1}, {2}, {3}.{4})",
                                    resultMethodName,
                                    linePosition.Line + 1,
                                    linePosition.Character + 1,
                                    analyzerType.Name,
                                    rule.Id);
                            }

                            if (i != diagnostics.Length - 1)
                            {
                                builder.Append(',');
                            }

                            builder.AppendLine();
                            break;
                        }
                    }
                }
                return builder.ToString();
            }
            #endregion
        }

        public static void VerifyAnalyzer(string source, params DiagnosticResult[] expected)
        {
            var test = new Test
            {
                TestCode = source,
            };
            test.ExpectedDiagnostics.AddRange(expected);
            test.Run();
        }

        public static void VerifyCodeFix(string source, DiagnosticResult expected, string fixedSource)
            => VerifyCodeFix(source, new[] { expected }, fixedSource);

        public static void VerifyCodeFix(string source, DiagnosticResult[] expected, string fixedSource)
        {
            var test = new Test
            {
                TestCode = source,
                FixedCode = fixedSource,
            };

            test.ExpectedDiagnostics.AddRange(expected);
            test.Run();
        }
    }
}