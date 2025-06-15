using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace DevTools.UnitTesting;

[PublicAPI]
public interface IMapGenerator
{
  MapGenerationSettings MapGenSettings { get; }
}