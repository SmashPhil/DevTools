using Microsoft.CodeAnalysis;

namespace DevTools.Analyzer;

internal static class FixtureAnalyzerUtility
{
  public static AttributeData GetAttribute(ISymbol symbol, INamedTypeSymbol attributeType)
  {
    foreach (AttributeData attribute in symbol.GetAttributes())
    {
      for (INamedTypeSymbol type = attribute.AttributeClass; type != null; type = type.BaseType)
      {
        if (SymbolEqualityComparer.Default.Equals(type, attributeType))
          return attribute;
      }
    }
    return null;
  }

  public static AttributeData GetInheritedAttribute(INamedTypeSymbol type, INamedTypeSymbol attributeType)
  {
    for (INamedTypeSymbol current = type; current != null; current = current.BaseType)
    {
      AttributeData attribute = GetAttribute(current, attributeType);
      if (attribute != null)
        return attribute;
    }
    return null;
  }
}
