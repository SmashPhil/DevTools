using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DevTools.Analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TestMethodStaticSuppressor : DiagnosticSuppressor
{
  private static readonly SuppressionDescriptor TestMethod = new(
    "DTSPR0001",
    "CA1822",
    "Test and lifecycle methods should remain instance methods for fixture execution.");

  public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => [TestMethod];

  public override void ReportSuppressions(SuppressionAnalysisContext context)
  {
    INamedTypeSymbol[] attributeTypes =
    [
      context.Compilation.GetTypeByMetadataName("DevTools.Testing.TestAttribute"),
      context.Compilation.GetTypeByMetadataName("DevTools.Testing.TestCaseAttribute"),
      context.Compilation.GetTypeByMetadataName("DevTools.Testing.SetUpAttribute"),
      context.Compilation.GetTypeByMetadataName("DevTools.Testing.TearDownAttribute"),
      context.Compilation.GetTypeByMetadataName("DevTools.Testing.OneTimeSetUpAttribute"),
      context.Compilation.GetTypeByMetadataName("DevTools.Testing.OneTimeTearDownAttribute")
    ];

    foreach (Diagnostic diagnostic in context.ReportedDiagnostics)
    {
      if (diagnostic.Id != "CA1822")
        continue;

      SyntaxTree tree = diagnostic.Location.SourceTree;
      if (tree is null)
        continue;

      SyntaxNode root = tree.GetRoot(context.CancellationToken);
      SyntaxNode node = root.FindNode(diagnostic.Location.SourceSpan);
      if (node.FirstAncestorOrSelf<MemberDeclarationSyntax>() is not MethodDeclarationSyntax declaration)
        continue;

      SemanticModel semanticModel = context.GetSemanticModel(tree);
      if (semanticModel.GetDeclaredSymbol(declaration, context.CancellationToken) is not IMethodSymbol method)
        continue;

      if (HasAttribute(method, attributeTypes))
      {
        context.ReportSuppression(Suppression.Create(TestMethod, diagnostic));
      }
    }
  }

  private static bool HasAttribute(IMethodSymbol method, INamedTypeSymbol[] attributeTypes)
  {
    foreach (AttributeData attribute in method.GetAttributes())
    {
      foreach (INamedTypeSymbol attributeType in attributeTypes)
      {
        if (attributeType != null && SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType))
        {
          return true;
        }
      }
    }
    return false;
  }
}
