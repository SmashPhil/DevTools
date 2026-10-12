using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DevTools.Analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TestFixtureConstructorAnalyzer : DiagnosticAnalyzer
{
  public const string DiagnosticId = "DT0004";

  private static readonly DiagnosticDescriptor Rule = new(
    DiagnosticId,
    "Test fixtures require a supported public constructor",
    "'{0}' must have exactly one public constructor with at most one parameter; other parameterized constructors are not allowed",
    "Usage",
    DiagnosticSeverity.Error,
    isEnabledByDefault: true,
    description: "Concrete test fixtures must have exactly one public constructor with zero or one parameter. " +
                 "Non-public parameterized instance constructors are not allowed. Parameter sources are validated separately.");

  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze |
                                           GeneratedCodeAnalysisFlags.ReportDiagnostics);
    context.EnableConcurrentExecution();

    context.RegisterCompilationStartAction(startContext =>
    {
      INamedTypeSymbol fixture = startContext.Compilation.GetTypeByMetadataName("DevTools.Testing.TestFixtureAttribute");
      if (fixture is null)
        return;

      startContext.RegisterSymbolAction(symbolContext => AnalyzeType(symbolContext, fixture),
        SymbolKind.NamedType);
    });
  }

  private static void AnalyzeType(SymbolAnalysisContext context, INamedTypeSymbol fixture)
  {
    INamedTypeSymbol type = (INamedTypeSymbol)context.Symbol;
    if (type.TypeKind != TypeKind.Class || type.IsAbstract ||
        FixtureAnalyzerUtility.GetInheritedAttribute(type, fixture) is null)
      return;

    int publicConstructors = 0;
    foreach (IMethodSymbol constructor in type.InstanceConstructors)
    {
      bool isPublic = constructor.DeclaredAccessibility == Accessibility.Public;
      if (constructor.Parameters.Length > 0 && (!isPublic || constructor.Parameters.Length != 1))
      {
        context.ReportDiagnostic(Diagnostic.Create(Rule, type.Locations[0], type.ToDisplayString()));
        return;
      }

      if (isPublic)
      {
        publicConstructors++;
      }
    }

    if (publicConstructors != 1)
    {
      context.ReportDiagnostic(Diagnostic.Create(Rule, type.Locations[0], type.ToDisplayString()));
    }
  }

}
