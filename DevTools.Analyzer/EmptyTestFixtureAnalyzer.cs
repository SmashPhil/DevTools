using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DevTools.Analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EmptyTestFixtureAnalyzer : DiagnosticAnalyzer
{
  public const string DiagnosticId = "DT0003";

  private static readonly DiagnosticDescriptor Rule = new(
    DiagnosticId,
    "Test fixture contains no tests",
    "'{0}' is a test fixture with no discoverable Test or TestCase methods",
    "Usage",
    DiagnosticSeverity.Warning,
    isEnabledByDefault: true,
    description: "Concrete test fixtures should contain at least one test method, either declared or inherited.");

  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze |
                                           GeneratedCodeAnalysisFlags.ReportDiagnostics);
    context.EnableConcurrentExecution();

    context.RegisterCompilationStartAction(startContext =>
    {
      INamedTypeSymbol fixture = startContext.Compilation.GetTypeByMetadataName("DevTools.Testing.TestFixtureAttribute");
      INamedTypeSymbol test = startContext.Compilation.GetTypeByMetadataName("DevTools.Testing.TestAttribute");
      if (fixture is null || test is null)
        return;

      startContext.RegisterSymbolAction(symbolContext => AnalyzeType(symbolContext, fixture, test),
        SymbolKind.NamedType);
    });
  }

  private static void AnalyzeType(SymbolAnalysisContext context, INamedTypeSymbol fixture, INamedTypeSymbol test)
  {
    INamedTypeSymbol type = (INamedTypeSymbol)context.Symbol;
    if (type.TypeKind != TypeKind.Class || type.IsAbstract ||
        FixtureAnalyzerUtility.GetInheritedAttribute(type, fixture) is null)
      return;

    for (INamedTypeSymbol current = type; current != null; current = current.BaseType)
    {
      foreach (ISymbol member in current.GetMembers())
      {
        if (member is not IMethodSymbol method)
          continue;
        if (method.MethodKind is not (MethodKind.Ordinary
            or MethodKind.ExplicitInterfaceImplementation))
          continue;

        if (!SymbolEqualityComparer.Default.Equals(current, type) &&
            (method.DeclaredAccessibility == Accessibility.Private || method.IsStatic))
          continue;

        if (FixtureAnalyzerUtility.GetAttribute(method, test) != null)
          return;
      }
    }
    context.ReportDiagnostic(Diagnostic.Create(Rule, type.Locations[0], type.ToDisplayString()));
  }
}
