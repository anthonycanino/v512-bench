using System;
using System.Numerics;
using System.Threading.Tasks;

using System.Runtime.CompilerServices;

using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using System.IO;
using System.Diagnostics;

using System.Collections.Generic;
using System.Linq;

namespace issues
{
  class Micro
  {
    // 512x512
    public static int[] Sizes = new int[] 
    {
      //(512 * 512), (1024 * 1024), (2048 * 2048), (4096 * 4096), (8192 * 8192)
      128, 256, 512, 1024, 2048, 4096, 8192, 16384
    };
    public static int Iterations = 100;

    public static double ElapsedMicroSeconds(Stopwatch watch)
    {
        double ticks = watch.ElapsedTicks;
	return (double) 1000000 * ((double) ticks  / (double) Stopwatch.Frequency);
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public unsafe static float NoVectorDotProd(float[] left, float[] right)
    {
      float result = 0;
      for (int i = 0; i < left.Length; i++)
      {
        result += left[i] * right[i];
      }
      return result;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public unsafe static float Vector128DotProd(float[] left, float[] right)
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
    public unsafe static float Vector256DotProd(float[] left, float[] right)
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
    public unsafe static float Vector512DotProd(float[] left, float[] right)
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
      var left = new float[1024];
      var right = new float[1024];
      for (int i = 0; i < 1024; i++) 
      {
        left[i] = 2;
        right[i] = 2;
      }

      Console.WriteLine("NoVectorDotProd: " + NoVectorDotProd(left, right));

      Console.WriteLine("Vector128DotProd: " + Vector128DotProd(left, right));
      Vector128DotProd(left, right);

      if (mode == "coreclr")
      {
        Vector256DotProd(left, right);
      }
      else if (mode == "llvm")
      {
        Console.WriteLine("Vector512DotProd: " + Vector512DotProd(left, right));
      }
    }

    static double standardDeviation(IEnumerable<double> sequence)
    {
      double result = 0;

      if (sequence.Any())
      {
        double average = sequence.Average();
        double sum = sequence.Sum(d => Math.Pow(d - average, 2));
        result = Math.Sqrt((sum) / (sequence.Count() - 1));
      }
      return result;
    }

    public static void RecordResult(double[] data, int size, StreamWriter writer, string tag)
    {
      double mean = data.Average();
      double stddev = standardDeviation(data);
      writer.WriteLine($"{tag},{size},{mean} us,{stddev} us");
    }

    public static void RunBenchSize(string mode, int size, StreamWriter writer, int iterations)
    {
      if (size % Vector128<float>.Count != 0 || size % Vector256<float>.Count != 0 || size % Vector512<float>.Count != 0)
      {
        Console.WriteLine($"{size} not evenly divisable by 128,256,and 512 vectors, skipping...");
        return;
      }
       

      var left = new float[size];
      var right = new float[size];
      for (int i = 0; i < size; i++) 
      {
        left[i] = 2;
        right[i] = 2;
      }

      var watch = new System.Diagnostics.Stopwatch();

      var data = new double[iterations];
        
      //
      // NoVectorDotProd
      // 

      for (int i = 0; i < iterations; i++)
      {
        watch.Restart();
        var res = NoVectorDotProd(left, right);
        watch.Stop();
        data[i] = ElapsedMicroSeconds(watch);
        if (i + 1 == iterations)
          Console.WriteLine("NoVector :: size: " + size + " result: " + res);
      }
      RecordResult(data, size, writer, "NoVector");

      //
      // Vector128DotProd
      // 

      for (int i = 0; i < iterations; i++)
      {
        watch.Restart();
        var res = Vector128DotProd(left, right);
        watch.Stop();
        data[i] = ElapsedMicroSeconds(watch);
        if (i + 1 == iterations)
          Console.WriteLine("Vector128 :: size: " + size + " result: " + res);
      }
      RecordResult(data, size, writer, "Vector128");

      //
      // Vector256DotProd
      // 
      if (mode == "coreclr")
      {

        for (int i = 0; i < iterations; i++)
        {
          watch.Restart();
          var res = Vector256DotProd(left, right);
          watch.Stop();
          data[i] = ElapsedMicroSeconds(watch);
          if (i + 1 == iterations)
           Console.WriteLine("Vector256 :: size: " + size + " result: " + res);
        }
        RecordResult(data, size, writer, "Vector256");
      }

      if (mode == "llvm")
      {
        for (int i = 0; i < iterations; i++)
        {
          watch.Restart();
          var res = Vector512DotProd(left, right);
          watch.Stop();
          data[i] = ElapsedMicroSeconds(watch);
          if (i + 1 == iterations)
            Console.WriteLine("Vector512 :: size: " + size + " result: " + res);
        }
        RecordResult(data, size, writer, "Vector512");

      }
    }

    public static void RunBench(string mode)
    {
	if (Stopwatch.IsHighResolution)
            {
                Console.WriteLine("Operations timed using the system's high-resolution performance counter.");
            }
            else
            {
                Console.WriteLine("Operations timed using the DateTime class.");
            }

            long frequency = Stopwatch.Frequency;
            Console.WriteLine("  Timer frequency in ticks per second = {0}",
                frequency);
            long nanosecPerTick = (1000L*1000L*1000L) / frequency;
            Console.WriteLine("  Timer is accurate within {0} nanoseconds",
                nanosecPerTick);

      StreamWriter writer = new StreamWriter("pre.csv");
      foreach (int size in Sizes)
      {
        RunBenchSize(mode, size, writer, 1);
      }
      writer.Close();

      writer = new StreamWriter("cold.csv");
      writer.WriteLine("name,size,mean,stddev");

      foreach (int size in Sizes)
      {
        RunBenchSize(mode, size, writer, Iterations);
      }

      writer.Close();

      writer = new StreamWriter("results.csv");
      writer.WriteLine("name,size,mean,stddev");

      foreach (int size in Sizes)
      {
        RunBenchSize(mode, size, writer, Iterations);
      }

      writer.Close();

    }

  }
}
