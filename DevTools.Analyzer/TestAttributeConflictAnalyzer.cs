using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DevTools.Analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TestAttributeConflictAnalyzer : DiagnosticAnalyzer
{
  public const string DiagnosticId = "DT0001";

  private static readonly DiagnosticDescriptor Rule = new(
    DiagnosticId,
    "Test and TestCase attributes must not be combined",
    "'{0}' has both TestAttribute and TestCaseAttribute; remove TestAttribute",
    "Usage",
    DiagnosticSeverity.Error,
    isEnabledByDefault: true,
    description: "TestCaseAttribute already marks a method as a test and must not be combined with TestAttribute.");

  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze |
                                           GeneratedCodeAnalysisFlags.ReportDiagnostics);
    context.EnableConcurrentExecution();

    context.RegisterCompilationStartAction(startContext =>
    {
      INamedTypeSymbol test = startContext.Compilation.GetTypeByMetadataName("DevTools.Testing.TestAttribute");
      INamedTypeSymbol testCase = startContext.Compilation.GetTypeByMetadataName("DevTools.Testing.TestCaseAttribute");
      if (test is null || testCase is null)
        return;

      startContext.RegisterSymbolAction(symbolContext => AnalyzeMethod(symbolContext, test, testCase),
        SymbolKind.Method);
    });
  }

  private static void AnalyzeMethod(SymbolAnalysisContext context, INamedTypeSymbol test,
    INamedTypeSymbol testCase)
  {
    IMethodSymbol method = (IMethodSymbol)context.Symbol;
    bool hasTest = false;
    bool hasTestCase = false;
    foreach (AttributeData attribute in method.GetAttributes())
    {
      if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, test))
      {
        hasTest = true;
      }
      else if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, testCase))
      {
        hasTestCase = true;
      }

      if (hasTest && hasTestCase)
      {
        context.ReportDiagnostic(Diagnostic.Create(Rule, method.Locations[0], method.ToDisplayString()));
        return;
      }
    }
  }
}
