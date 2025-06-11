using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevTools.UnitTesting;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class PropertyAttribute : MetaDataAttribute
{
  public PropertyAttribute(string name, string value) : base(MetaDataName.Category, (name, value))
  {
  }
}