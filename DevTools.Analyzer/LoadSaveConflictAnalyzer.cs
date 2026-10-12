using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DevTools.Analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LoadSaveConflictAnalyzer : DiagnosticAnalyzer
{
  public const string DiagnosticId = "DT0002";

  private static readonly DiagnosticDescriptor Rule = new(
    DiagnosticId,
    "Save loading bypasses generation settings",
    "'{0}' loads a save file and implements generation settings interfaces; custom generation settings will be ignored",
    "Usage",
    DiagnosticSeverity.Warning,
    isEnabledByDefault: true,
    description: "Fixtures that load a save file bypass custom world, map, scenario, and storyteller generation settings.");

  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze |
                                           GeneratedCodeAnalysisFlags.ReportDiagnostics);
    context.EnableConcurrentExecution();

    context.RegisterCompilationStartAction(startContext =>
    {
      INamedTypeSymbol fixture = startContext.Compilation.GetTypeByMetadataName("DevTools.Testing.TestFixtureAttribute");
      INamedTypeSymbol loadSave = startContext.Compilation.GetTypeByMetadataName("DevTools.Testing.LoadSaveAttribute");
      if (fixture is null || loadSave is null)
        return;

      ImmutableArray<INamedTypeSymbol> settings =
      [
        startContext.Compilation.GetTypeByMetadataName("DevTools.Testing.IWorldGeneration"),
        startContext.Compilation.GetTypeByMetadataName("DevTools.Testing.IMapGeneration"),
        startContext.Compilation.GetTypeByMetadataName("DevTools.Testing.IScenario"),
        startContext.Compilation.GetTypeByMetadataName("DevTools.Testing.IStoryteller")
      ];
      startContext.RegisterSymbolAction(symbolContext => AnalyzeType(symbolContext, fixture, loadSave, settings),
        SymbolKind.NamedType);
    });
  }

  private static void AnalyzeType(SymbolAnalysisContext context, INamedTypeSymbol fixture,
    INamedTypeSymbol loadSave, ImmutableArray<INamedTypeSymbol> settings)
  {
    INamedTypeSymbol type = (INamedTypeSymbol)context.Symbol;
    if (type.TypeKind != TypeKind.Class || type.IsAbstract ||
        FixtureAnalyzerUtility.GetInheritedAttribute(type, fixture) is null)
      return;

    AttributeData attribute = FixtureAnalyzerUtility.GetInheritedAttribute(type, loadSave);
    if (attribute is null || attribute.ConstructorArguments.Length == 0 ||
        attribute.ConstructorArguments[0].Value is not string fileName || string.IsNullOrEmpty(fileName))
      return;

    foreach (INamedTypeSymbol implementedInterface in type.AllInterfaces)
    {
      foreach (INamedTypeSymbol setting in settings)
      {
        if (setting != null && SymbolEqualityComparer.Default.Equals(implementedInterface, setting))
        {
          context.ReportDiagnostic(Diagnostic.Create(Rule, type.Locations[0], type.ToDisplayString()));
          return;
        }
      }
    }
  }
}
