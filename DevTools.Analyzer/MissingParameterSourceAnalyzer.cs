using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DevTools.Analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MissingParameterSourceAnalyzer : DiagnosticAnalyzer
{
  public const string DiagnosticId = "DT0006";

  private static readonly DiagnosticDescriptor Rule = new(
    DiagnosticId,
    "Test parameters require a source attribute",
    "Parameter '{0}' must be marked with Parameters, ParametersSource, or DefParameters",
    "Usage",
    DiagnosticSeverity.Error,
    isEnabledByDefault: true,
    description: "Fixture constructor, test, and lifecycle parameters require a source attribute. " +
                 "Methods supplied by TestCaseAttribute do not require parameter source attributes.");

  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze |
                                           GeneratedCodeAnalysisFlags.ReportDiagnostics);
    context.EnableConcurrentExecution();

    context.RegisterCompilationStartAction(startContext =>
    {
      INamedTypeSymbol fixture = startContext.Compilation.GetTypeByMetadataName("DevTools.Testing.TestFixtureAttribute");
      INamedTypeSymbol testCase = startContext.Compilation.GetTypeByMetadataName("DevTools.Testing.TestCaseAttribute");
      ImmutableArray<INamedTypeSymbol> methods =
      [
        startContext.Compilation.GetTypeByMetadataName("DevTools.Testing.TestAttribute"),
        startContext.Compilation.GetTypeByMetadataName("DevTools.Testing.SetUpAttribute"),
        startContext.Compilation.GetTypeByMetadataName("DevTools.Testing.TearDownAttribute"),
        startContext.Compilation.GetTypeByMetadataName("DevTools.Testing.OneTimeSetUpAttribute"),
        startContext.Compilation.GetTypeByMetadataName("DevTools.Testing.OneTimeTearDownAttribute")
      ];
      ImmutableArray<INamedTypeSymbol> sources =
      [
        startContext.Compilation.GetTypeByMetadataName("DevTools.Testing.ParametersAttribute"),
        startContext.Compilation.GetTypeByMetadataName("DevTools.Testing.ParametersSourceAttribute"),
        startContext.Compilation.GetTypeByMetadataName("DevTools.Testing.DefParametersAttribute")
      ];
      startContext.RegisterSymbolAction(symbolContext => AnalyzeMethod(symbolContext, fixture, testCase, methods, sources),
        SymbolKind.Method);
    });
  }

  private static void AnalyzeMethod(SymbolAnalysisContext context, INamedTypeSymbol fixture,
    INamedTypeSymbol testCase, ImmutableArray<INamedTypeSymbol> methods, ImmutableArray<INamedTypeSymbol> sources)
  {
    IMethodSymbol method = (IMethodSymbol)context.Symbol;
    if (method.Parameters.Length == 0)
      return;

    if (method.MethodKind == MethodKind.Constructor)
    {
      if (fixture is null || method.ContainingType.IsAbstract ||
          method.DeclaredAccessibility != Accessibility.Public ||
          FixtureAnalyzerUtility.GetInheritedAttribute(method.ContainingType, fixture) is null)
        return;
    }
    else
    {
      bool isTestMethod = false;
      for (IMethodSymbol current = method; current != null; current = current.OverriddenMethod)
      {
        if (testCase != null && FixtureAnalyzerUtility.GetAttribute(current, testCase) != null)
          return;

        isTestMethod |= HasAttribute(current, methods);
      }
      if (!isTestMethod)
        return;
    }

    foreach (IParameterSymbol parameter in method.Parameters)
    {
      bool hasSource = false;
      for (IMethodSymbol current = method; current != null; current = current.OverriddenMethod)
      {
        if (HasAttribute(current.Parameters[parameter.Ordinal], sources))
        {
          hasSource = true;
          break;
        }
      }
      if (!hasSource)
      {
        context.ReportDiagnostic(Diagnostic.Create(Rule, parameter.Locations[0], parameter.Name));
      }
    }
  }

  private static bool HasAttribute(ISymbol symbol, ImmutableArray<INamedTypeSymbol> attributeTypes)
  {
    foreach (INamedTypeSymbol attributeType in attributeTypes)
    {
      if (attributeType != null && FixtureAnalyzerUtility.GetAttribute(symbol, attributeType) != null)
        return true;
    }
    return false;
  }
}
