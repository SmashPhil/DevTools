using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DevTools.Analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NullTestCaseArgumentsAnalyzer : DiagnosticAnalyzer
{
  public const string DiagnosticId = "DT0008";

  private static readonly DiagnosticDescriptor Rule = new(
    DiagnosticId,
    "TestCase argument array must not be null",
    "'{0}' has a TestCase with a null argument array; use an empty array or an array containing the intended arguments",
    "Usage",
    DiagnosticSeverity.Error,
    isEnabledByDefault: true,
    description: "TestCaseAttribute requires a non-null argument array. Empty arrays and individual null arguments are allowed.");

  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze |
                                           GeneratedCodeAnalysisFlags.ReportDiagnostics);
    context.EnableConcurrentExecution();

    context.RegisterCompilationStartAction(startContext =>
    {
      INamedTypeSymbol testCase = startContext.Compilation.GetTypeByMetadataName("DevTools.Testing.TestCaseAttribute");
      if (testCase is null)
        return;

      startContext.RegisterSymbolAction(symbolContext => AnalyzeMethod(symbolContext, testCase),
        SymbolKind.Method);
    });
  }

  private static void AnalyzeMethod(SymbolAnalysisContext context, INamedTypeSymbol testCase)
  {
    IMethodSymbol method = (IMethodSymbol)context.Symbol;
    foreach (AttributeData attribute in method.GetAttributes())
    {
      if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, testCase) ||
          attribute.ConstructorArguments.Length != 1)
        continue;

      TypedConstant arguments = attribute.ConstructorArguments[0];
      if (arguments.Kind == TypedConstantKind.Array && arguments.IsNull)
      {
        Location location = attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation() ??
                            method.Locations[0];
        context.ReportDiagnostic(Diagnostic.Create(Rule, location, method.ToDisplayString()));
      }
    }
  }
}
