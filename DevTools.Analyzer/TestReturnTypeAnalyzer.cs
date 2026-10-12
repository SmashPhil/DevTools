using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DevTools.Analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TestReturnTypeAnalyzer : DiagnosticAnalyzer
{
  public const string DiagnosticId = "DT0009";

  private static readonly DiagnosticDescriptor Rule = new(
    DiagnosticId,
    "Test methods must return void or IEnumerator",
    "'{0}' is marked with TestAttribute and must return void or System.Collections.IEnumerator",
    "Usage",
    DiagnosticSeverity.Error,
    isEnabledByDefault: true,
    description: "Methods marked with TestAttribute must return void or exactly System.Collections.IEnumerator " +
                 "for coroutine execution. This rule does not apply to TestCaseAttribute or other derived attributes.");

  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze |
                                           GeneratedCodeAnalysisFlags.ReportDiagnostics);
    context.EnableConcurrentExecution();

    context.RegisterCompilationStartAction(startContext =>
    {
      INamedTypeSymbol test = startContext.Compilation.GetTypeByMetadataName("DevTools.Testing.TestAttribute");
      if (test is null)
        return;

      INamedTypeSymbol enumerator = startContext.Compilation.GetTypeByMetadataName("System.Collections.IEnumerator");
      startContext.RegisterSymbolAction(symbolContext => AnalyzeMethod(symbolContext, test, enumerator),
        SymbolKind.Method);
    });
  }

  private static void AnalyzeMethod(SymbolAnalysisContext context, INamedTypeSymbol test,
    INamedTypeSymbol enumerator)
  {
    IMethodSymbol method = (IMethodSymbol)context.Symbol;
    if (method.ReturnsVoid ||
        (!method.ReturnsByRef && !method.ReturnsByRefReadonly && enumerator != null &&
         SymbolEqualityComparer.Default.Equals(method.ReturnType, enumerator)))
      return;

    foreach (AttributeData attribute in method.GetAttributes())
    {
      if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, test))
      {
        context.ReportDiagnostic(Diagnostic.Create(Rule, method.Locations[0], method.ToDisplayString()));
        return;
      }
    }
  }
}
