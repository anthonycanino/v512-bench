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
  class Micro
  {
    public static int Size = 4096;
    public static int Iterations = 100;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public unsafe static int NoVectorDotProd(ReadOnlySpan<int> left, ReadOnlySpan<int> right)
    {
      int result = 0;
      for (int i = 0; i < left.Length; i++)
      {
        result += left[i] * right[i];
      }
      return result;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public unsafe static int Vector128DotProd(ReadOnlySpan<int> left, ReadOnlySpan<int> right)
    {
      int result = 0;
      fixed (int *pleft = left, pright = right)
      {
        Vector128<int> vresult = Vector128<int>.Zero;
        for (int i = 0; i < left.Length; i += Vector128<int>.Count)
        {
          Vector128<int> vleft = Sse2.LoadVector128(pleft + i);
          Vector128<int> vright = Sse2.LoadVector128(pright + i);
          result += Vector128.Dot(vleft, vright);
        }
      }

      return result;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public unsafe static int Vector256DotProd(ReadOnlySpan<int> left, ReadOnlySpan<int> right)
    {
      int result = 0;
      fixed (int *pleft = left, pright = right)
      {
        Vector256<int> vresult = Vector256<int>.Zero;
        for (int i = 0; i < left.Length; i += Vector256<int>.Count)
        {
          Vector256<int> vleft = Avx.LoadVector256(pleft + i);
          Vector256<int> vright = Avx.LoadVector256(pright + i);
          result += Vector256.Dot(vleft, vright);
        }
      }

      return result;
    }

    public static void Run()
    {
      var lefta = new int[Size];
      var righta = new int[Size];
      for (int i = 0; i < Size; i++) 
      {
        lefta[i] = i;
        righta[i] = i;
      }

      ReadOnlySpan<int> left = new ReadOnlySpan<int>(lefta);
      ReadOnlySpan<int> right = new ReadOnlySpan<int>(righta);

      Console.WriteLine("NoVectorDotProd: " + NoVectorDotProd(left, right));
      Console.WriteLine("Vector128DotProd: " + Vector128DotProd(left, right));
      Console.WriteLine("Vector256DotProd: " + Vector256DotProd(left, right));
    }
  }
}
