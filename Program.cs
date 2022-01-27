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
      string micro = args[0];
      string mode = args[1];
      string type = args[2];
      int iters = Int32.Parse(args[3]);

			if (micro == "dot")
			{
				if (mode == "diag")
				{
					Dot.RunDiag(type);
				}
				else
				{
					Dot.RunBench(type, iters);
				}
			}
			else if (micro == "matmul")
			{
				if (mode == "diag")
				{
					Matmul.RunDiag(type);
				}
				else
				{
					Matmul.RunBench(type, iters);
				}
			}

    }
  }
}
