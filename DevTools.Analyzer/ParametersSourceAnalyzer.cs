using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DevTools.Analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ParametersSourceAnalyzer : DiagnosticAnalyzer
{
  public const string DiagnosticId = "DT0007";

  private static readonly DiagnosticDescriptor Rule = new(
    DiagnosticId,
    "ParametersSource must identify a valid source",
    "Parameter source for '{0}' is invalid: {1}",
    "Usage",
    DiagnosticSeverity.Error,
    isEnabledByDefault: true,
    description: "ParametersSource must name a static array field or a static, non-generic, parameterless method " +
                 "returning IEnumerable. Properties are not supported.");

  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze |
                                           GeneratedCodeAnalysisFlags.ReportDiagnostics);
    context.EnableConcurrentExecution();

    context.RegisterCompilationStartAction(startContext =>
    {
      INamedTypeSymbol source = startContext.Compilation.GetTypeByMetadataName("DevTools.Testing.ParametersSourceAttribute");
      if (source is null)
        return;

      INamedTypeSymbol enumerable = startContext.Compilation.GetTypeByMetadataName("System.Collections.IEnumerable");
      startContext.RegisterSymbolAction(symbolContext => AnalyzeMethod(symbolContext, source, enumerable),
        SymbolKind.Method);
    });
  }

  private static void AnalyzeMethod(SymbolAnalysisContext context, INamedTypeSymbol source,
    INamedTypeSymbol enumerable)
  {
    IMethodSymbol method = (IMethodSymbol)context.Symbol;
    foreach (IParameterSymbol parameter in method.Parameters)
    {
      AttributeData attribute = FixtureAnalyzerUtility.GetAttribute(parameter, source);
      if (attribute is null || attribute.ConstructorArguments.Length is not (1 or 2))
        continue;

      ImmutableArray<TypedConstant> arguments = attribute.ConstructorArguments;
      if (arguments[arguments.Length - 1].Kind == TypedConstantKind.Error)
        continue;

      INamedTypeSymbol sourceType = method.ContainingType;
      if (arguments.Length == 2)
      {
        if (arguments[0].Kind == TypedConstantKind.Error)
          continue;
        if (!arguments[0].IsNull)
          sourceType = arguments[0].Value as INamedTypeSymbol;
      }

      string name = arguments[arguments.Length - 1].Value as string;
      string reason = ValidateSource(sourceType, name, enumerable);
      if (reason != null)
      {
        Location location = attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation() ??
                            parameter.Locations[0];
        context.ReportDiagnostic(Diagnostic.Create(Rule, location, parameter.Name, reason));
      }
    }
  }

  private static string ValidateSource(INamedTypeSymbol sourceType, string name, INamedTypeSymbol enumerable)
  {
    if (string.IsNullOrEmpty(name))
      return "the member name must not be null or empty";
    if (sourceType is null || sourceType.IsUnboundGenericType)
      return "the source type must identify a constructed named type";

    // The runner searches the entire hierarchy for a field before looking for a method.
    for (INamedTypeSymbol current = sourceType; current != null; current = current.BaseType)
    {
      foreach (ISymbol member in current.GetMembers(name))
      {
        if (member is IFieldSymbol field)
        {
          return field.IsStatic && (field.Type is IArrayTypeSymbol || field.Type.SpecialType == SpecialType.System_Array) ?
            null : "the source field must be static and have an array type";
        }
      }
    }

    for (INamedTypeSymbol current = sourceType; current != null; current = current.BaseType)
    {
      IMethodSymbol candidate = null;
      int methodCount = 0;
      int parameterlessCount = 0;
      foreach (ISymbol member in current.GetMembers(name))
      {
        if (member is not IMethodSymbol method || method.MethodKind != MethodKind.Ordinary)
          continue;

        methodCount++;
        if (method.Parameters.Length == 0)
        {
          candidate = method;
          parameterlessCount++;
        }
      }
      if (methodCount == 0)
        continue;
      if (parameterlessCount != 1)
        return "the source must resolve to one parameterless method";
      if (!candidate.IsStatic || candidate.IsGenericMethod || candidate.ReturnsByRef || candidate.ReturnsByRefReadonly)
        return "the source method must be static, non-generic, and return its result by value";
      if (enumerable != null)
      {
        if (SymbolEqualityComparer.Default.Equals(candidate.ReturnType, enumerable))
          return null;
        foreach (INamedTypeSymbol implementedInterface in candidate.ReturnType.AllInterfaces)
        {
          if (SymbolEqualityComparer.Default.Equals(implementedInterface, enumerable))
            return null;
        }
        return "the source method must return IEnumerable";
      }
      return null;
    }
    return "no field or method with that name exists on the source type";
  }
}
