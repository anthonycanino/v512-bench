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
    // 512x512
    public static int Size = 262144;
    public static int Iterations = 100;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public unsafe static float NoVectorDotProd(ReadOnlySpan<float> left, ReadOnlySpan<float> right)
    {
      float result = 0;
      for (int i = 0; i < left.Length; i++)
      {
        result += left[i] * right[i];
      }
      return result;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public unsafe static float Vector128DotProd(ReadOnlySpan<float> left, ReadOnlySpan<float> right)
    {
      float result = 0;
      fixed (float *pleft = left, pright = right)
      {
        Vector128<float> vresult = Vector128<float>.Zero;
        for (int i = 0; i < left.Length; i += Vector128<float>.Count)
        {
          Vector128<float> vleft = Sse2.LoadVector128(pleft + i);
          Vector128<float> vright = Sse2.LoadVector128(pright + i);
          vresult = Sse2.Add(Sse2.Multiply(vleft, vright), vresult);
        } 
        
        vresult = Sse3.HorizontalAdd(vresult, vresult);
        vresult = Sse3.HorizontalAdd(vresult, vresult);


        result = vresult.ToScalar();
      }

      return result;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public unsafe static float Vector256DotProd(ReadOnlySpan<float> left, ReadOnlySpan<float> right)
    {
      float result = 0;
      fixed (float *pleft = left, pright = right)
      {
        Vector256<float> vresult = Vector256<float>.Zero;
        for (int i = 0; i < left.Length; i += Vector256<float>.Count)
        {
          Vector256<float> vleft = Avx.LoadVector256(pleft + i);
          Vector256<float> vright = Avx.LoadVector256(pright + i);
          vresult = Avx.Add(Avx.Multiply(vleft, vright), vresult);
        }

        var lres = Avx.ExtractVector128(vresult, 0);
        var hres = Avx.ExtractVector128(vresult, 1);
        var res = Avx.Add(lres, hres);
        res = Avx.HorizontalAdd(res, res);
        res = Avx.HorizontalAdd(res, res);
        result = res.ToScalar();
      }

      return result;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public unsafe static float Vector512DotProd(ReadOnlySpan<float> left, ReadOnlySpan<float> right)
    {
      float result = 0;
      fixed (float *pleft = left, pright = right)
      {
        Vector512<float> vresult = Vector512<float>.Zero;
        for (int i = 0; i < left.Length; i += Vector512<float>.Count)
        {
          Vector512<float> vleft = Avx512.LoadVector512(pleft + i);
          Vector512<float> vright = Avx512.LoadVector512(pright + i);
          vresult = Avx512.Add(Avx512.Multiply(vleft, vright), vresult);
        }

        result = Avx512.ReduceAdd(vresult);
      }

      return result;
    }

    public static void RunDiag(string mode)
    {
      var lefta = new float[Size];
      var righta = new float[Size];
      for (int i = 0; i < Size; i++) 
      {
        lefta[i] = i;
        righta[i] = i;
      }

      ReadOnlySpan<float> left = new ReadOnlySpan<float>(lefta);
      ReadOnlySpan<float> right = new ReadOnlySpan<float>(righta);

      Console.WriteLine("NoVectorDotProd: " + NoVectorDotProd(left, right));

      Console.WriteLine("Vector128DotProd: " + Vector128DotProd(left, right));
      Vector128DotProd(left, right);

      if (mode == "coreclr")
      {
        Vector256DotProd(left, right);
      }
      else if (mode == "llvm")
      {
        Vector512DotProd(left, right);
      }
    }

    public static void RunBench(string mode)
    {
      var lefta = new float[Size];
      var righta = new float[Size];
      for (int i = 0; i < Size; i++) 
      {
        lefta[i] = i;
        righta[i] = i;
      }

      ReadOnlySpan<float> left = new ReadOnlySpan<float>(lefta);
      ReadOnlySpan<float> right = new ReadOnlySpan<float>(righta);

      var watch = new System.Diagnostics.Stopwatch();
      if (Stopwatch.IsHighResolution)
      {
        Console.WriteLine("Using high resolution timer...");
      }
      else
      {
        Console.WriteLine("Using millisecond resolution timer...");
      }
  
      //
      // NoVectorDotProd
      // 
      watch.Restart();

      for (int i = 0; i < Iterations; i++)
      {
        NoVectorDotProd(left, right);
      }

      watch.Stop();
      double ns = 1000000000.0 * (double)watch.ElapsedTicks / Stopwatch.Frequency;
      Console.WriteLine($"NoVectorDotProduce: {ns / (double) Iterations} ns");

      //
      // Vector128DotProd
      // 
      watch.Restart();

      for (int i = 0; i < Iterations; i++)
      {
        Vector128DotProd(left, right);
      }

      watch.Stop();
      ns = 1000000000.0 * (double)watch.ElapsedTicks / Stopwatch.Frequency;
      Console.WriteLine($"Vector128DotProd: {ns / (double) Iterations} ns");



      //
      // Vector256DotProd
      // 
      if (mode == "coreclr")
      {
        watch.Restart();

        for (int i = 0; i < Iterations; i++)
        {
          Vector256DotProd(left, right);
        }

        watch.Stop();
        ns = 1000000000.0 * (double)watch.ElapsedTicks / Stopwatch.Frequency;
        Console.WriteLine($"Vector256DotProd: {ns / (double) Iterations} ns");
      }

      if (mode == "llvm")
      {
        watch.Restart();

        for (int i = 0; i < Iterations; i++)
        {
          Vector512DotProd(left, right);
        }

        watch.Stop();
        ns = 1000000000.0 * (double)watch.ElapsedTicks / Stopwatch.Frequency;
        Console.WriteLine($"Vector512DotProd: {ns / (double) Iterations} ns");
      }


    }
  }
}
