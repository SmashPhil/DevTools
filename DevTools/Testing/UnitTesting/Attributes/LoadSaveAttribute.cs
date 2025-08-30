using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevTools.UnitTesting;

[AttributeUsage(AttributeTargets.Class)]
public class LoadSaveAttribute : Attribute, IMetaData
{
  public LoadSaveAttribute(string fileName)
  {
    Key = MetaDataName.LoadSave;
    Value = fileName;
  }

  public int Key { get; }

  public object Value { get; }
}