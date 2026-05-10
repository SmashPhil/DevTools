using System;

namespace DevTools.Testing;

[AttributeUsage(AttributeTargets.Class)]
public class LoadSaveAttribute : MetaDataAttribute<string>
{
  public LoadSaveAttribute(string fileName) : base(MetaDataName.LoadSave, fileName)
  {
  }
}