using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevTools.Benchmarking;

/// <summary>
/// Divide resulting benchmark time by some constant. Should be used when a test is running
/// multiple repeated tasks that cannot be generalized without polluting the test with overhead.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class DivideByAttribute : Attribute
{
  public DivideByAttribute(float divisor)
  {
    Divisor = divisor;
  }

  public float Divisor { get; }
}