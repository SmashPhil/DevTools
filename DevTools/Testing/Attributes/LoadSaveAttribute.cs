using System;
using JetBrains.Annotations;

namespace DevTools.Testing;

[PublicAPI]
[AttributeUsage(AttributeTargets.Class)]
public class LoadSaveAttribute : MetaDataAttribute<string>
{
  public LoadSaveAttribute(string fileName) : base(MetaDataName.LoadSave, fileName)
  {
  }
}