using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DevTools.Analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SetUpTearDownReturnTypeAnalyzer : DiagnosticAnalyzer
{
  public const string DiagnosticId = "DT0005";

  private static readonly DiagnosticDescriptor Rule = new(
    DiagnosticId,
    "SetUp and TearDown methods must return void",
    "'{0}' is marked with SetUp, OneTimeSetUp, TearDown, or OneTimeTearDown and must return void",
    "Usage",
    DiagnosticSeverity.Error,
    isEnabledByDefault: true,
    description: "SetUp and TearDown methods must have a void return type.");

  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze |
                                           GeneratedCodeAnalysisFlags.ReportDiagnostics);
    context.EnableConcurrentExecution();

    context.RegisterCompilationStartAction(startContext =>
    {
      INamedTypeSymbol setUp = startContext.Compilation.GetTypeByMetadataName("DevTools.Testing.SetUpAttribute");
      INamedTypeSymbol tearDown = startContext.Compilation.GetTypeByMetadataName("DevTools.Testing.TearDownAttribute");
      INamedTypeSymbol oneTimeSetUp = startContext.Compilation.GetTypeByMetadataName("DevTools.Testing.OneTimeSetUpAttribute");
      INamedTypeSymbol oneTimeTearDown = startContext.Compilation.GetTypeByMetadataName("DevTools.Testing.OneTimeTearDownAttribute");
      if (setUp is null && tearDown is null)
        return;

      startContext.RegisterSymbolAction(symbolContext => AnalyzeMethod(symbolContext, setUp, tearDown, oneTimeSetUp, oneTimeTearDown),
        SymbolKind.Method);
    });
  }

  private static void AnalyzeMethod(SymbolAnalysisContext context, params INamedTypeSymbol[] symbols)
  {
    IMethodSymbol method = (IMethodSymbol)context.Symbol;
    if (method.ReturnsVoid)
      return;

    for (IMethodSymbol current = method; current != null; current = current.OverriddenMethod)
    {
      foreach (INamedTypeSymbol symbol in symbols)
      {
        if (symbol != null && FixtureAnalyzerUtility.GetAttribute(current, symbol) != null)
        {
          context.ReportDiagnostic(Diagnostic.Create(Rule, method.Locations[0], method.ToDisplayString()));
          return;
        }
      }
    }
  }
}
