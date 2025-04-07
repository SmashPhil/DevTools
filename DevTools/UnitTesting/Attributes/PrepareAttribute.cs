using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevTools.UnitTesting;

[AttributeUsage(AttributeTargets.Method)]
public class PrepareAttribute : Attribute
{
}