using System;
using System.Numerics;
using System.Threading.Tasks;

using System.Runtime.CompilerServices;

using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using System.IO;
using System.Diagnostics;

namespace issues
{
  class Program
  {
    public static void Main(string[] args)
    {
      string mode = args[0];
      string type = args[1];
      int iters = Int32.Parse(args[2]);

      if (mode == "diag")
      {
        Micro.RunDiag(type);
      }
      else
      {
        Micro.RunBench(type, iters);
      }

    }
  }
}
