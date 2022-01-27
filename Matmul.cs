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
  class Matmul
  {
    public static int[] Sizes = new int[] 
    {
      256, 512, 1024,
    };

		public static int Discard = 10;
		public static int Samples = 1;

    public static double ElapsedMilliSeconds(Stopwatch watch)
    {
      double ticks = watch.ElapsedTicks;
			return (double) (1000L) * ((double) ticks  / (double) Stopwatch.Frequency);
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public unsafe static void NoVectorMatmul(float* a, float* b, float *c, int length)
    {
      for (int i = 0; i < length; i++)
      {
				for (int k = 0; k < length; k++)
				{
					for (int j = 0; j < length; j++)
					{
						c[i*length+j] += a[i*length+k] * b[k*length+j];
					}
				}
      }
    }

		[MethodImplAttribute(MethodImplOptions.NoInlining)]
    public unsafe static void Vector128Matmul(float* a, float* b, float *c, int length)
    {
      for (int i = 0; i < length; i++)
      {
				for (int k = 0; k < length; k++)
				{
					Vector128<float> sv = Vector128.Create(a[i*length+k]);
					for (int j = 0; j < length; j += Vector128<float>.Count)
					{
						Vector128<float> cv = Sse2.LoadVector128(c+(i*length+j));
						Vector128<float> bv = Sse2.LoadVector128(b+(i*length+j));
						Vector128<float> tv = Sse2.Multiply(bv, sv);
						cv = Sse.Add(tv, cv);
						Sse2.Store(c+(i*length+j), cv);
					}
				}
      }
    }

		[MethodImplAttribute(MethodImplOptions.NoInlining)]
    public unsafe static void Vector256Matmul(float* a, float* b, float *c, int length)
    {
      for (int i = 0; i < length; i++)
      {
				for (int k = 0; k < length; k++)
				{
					Vector256<float> sv = Vector256.Create(a[i*length+k]);
					for (int j = 0; j < length; j += Vector256<float>.Count)
					{
						Vector256<float> cv = Avx.LoadVector256(c+(i*length+j));
						Vector256<float> bv = Avx.LoadVector256(b+(i*length+j));
						Vector256<float> tv = Avx.Multiply(bv, sv);
						cv = Avx.Add(tv, cv);
						Avx.Store(c+(i*length+j), cv);
					}
				}
      }
    }

		[MethodImplAttribute(MethodImplOptions.NoInlining)]
    public unsafe static void Vector512Matmul(float* a, float* b, float *c, int length)
    {
      for (int i = 0; i < length; i++)
      {
				for (int k = 0; k < length; k++)
				{
					Vector512<float> sv = Vector512.Create(a[i*length+k]);
					for (int j = 0; j < length; j += Vector512<float>.Count)
					{
						Vector512<float> cv = Avx512.LoadVector512(c+(i*length+j));
						Vector512<float> bv = Avx512.LoadVector512(b+(i*length+j));
						Vector512<float> tv = Avx512.Multiply(bv, sv);
						cv = Avx512.Add(tv, cv);
						Avx512.Store(c+(i*length+j), cv);
					}
				}
      }
    }

		public static void Set(float[] arr, int size, float val)
		{
			for (int i = 0; i < size; i++) 
			{
				for (int j = 0; j < size; j++)
				{
					arr[i*size+j] = val;
				}
			}
		}

		public static void Dump(float[] arr, int size)
		{
			for (int i = 0; i < size; i++) 
			{
				for (int j = 0; j < size; j++)
				{
					Console.Write(arr[i*size+j] + " ");
				}
				Console.WriteLine();
			}
		}

    public static void RunDiag(string mode)
    {
      var A = new float[16*16];
      var B = new float[16*16];
      var C = new float[16*16];

			Set(A, 16, 2);
			Set(B, 16, 3);

			unsafe
			{
				fixed (float* pa = A, pb = B, pc = C)
				{
					Set(C, 16, 0);
					NoVectorMatmul(pa, pb, pc, 16);
					Console.WriteLine("== NoVectorMatul ==");
					Dump(C, 16);

					Set(C, 16, 0);
					Vector128Matmul(pa, pb, pc, 16);
					Console.WriteLine("== Vector128Matmul ==");
					Dump(C, 16);

					if (mode == "coreclr")
					{
						Set(C, 16, 0);
						Vector256Matmul(pa, pb, pc, 16);
						Console.WriteLine("== Vector256Matmul ==");
						Dump(C, 16);
					}

					if (mode == "llvm")
					{
						Set(C, 16, 0);
						Vector512Matmul(pa, pb, pc, 16);
						Console.WriteLine("== Vector512Matmul ==");
						Dump(C, 16);
					}
				}
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
			double error = stddev / Math.Sqrt((double)data.Length);
      writer.WriteLine($"{tag},{size},{mean},{stddev},{error}");
    }

    public static void RunBenchSize(string mode, int size, StreamWriter writer, int iterations)
    {
      if (size % Vector128<float>.Count != 0 || size % Vector256<float>.Count != 0 || size % Vector512<float>.Count != 0)
      {
        Console.WriteLine($"{size} not evenly divisable by 128,256,and 512 vectors, skipping...");
        return;
      }

			Console.WriteLine($"Running size {size} for {iterations} iterations drawing {Samples} samples");

			var A = new float[size*size];
      var B = new float[size*size];
      var C = new float[size*size];

			Set(A, 16, 2);
			Set(B, 16, 3);

			var watch = new System.Diagnostics.Stopwatch();
			var data = new double[iterations-Discard];

			unsafe
			{
				fixed (float* pa = A, pb = B, pc = C)
				{ 
					//
					// NoVectorDotProd
					// 
					for (int i = 0; i < iterations; i++)
					{
						Set(C, size, 0);
						watch.Restart();
						for (int j = 0; j < Samples; j++)
							NoVectorMatmul(pa, pb, pc, size);
						watch.Stop();
						if (i >= Discard)
							data[i-Discard] = (double) watch.ElapsedMilliseconds / (double) Samples;
					}
					RecordResult(data, size, writer, "NoVector");

					//
					// Vector128DotProd
					// 
					for (int i = 0; i < iterations; i++)
					{
						Set(C, size, 0);
						watch.Restart();
						for (int j = 0; j < Samples; j++)
							Vector128Matmul(pa, pb, pc, size);
						watch.Stop();
						if (i >= Discard)
							data[i-Discard] = (double) watch.ElapsedMilliseconds / (double) Samples;
					}
					RecordResult(data, size, writer, "Vector128");

					//
					// Vector256DotProd
					// 
					if (mode == "coreclr")
					{

						for (int i = 0; i < iterations; i++)
						{
							Set(C, size, 0);
							watch.Restart();
							for (int j = 0; j < Samples; j++)
								Vector256Matmul(pa, pb, pc, size);
							watch.Stop();
							if (i >= Discard)
								data[i-Discard] = (double) watch.ElapsedMilliseconds / (double) Samples;
						}
						RecordResult(data, size, writer, "Vector256");
					}
					else
					{
      			writer.WriteLine("Vector256,,,,");
					}

					if (mode == "llvm")
					{
						for (int i = 0; i < iterations; i++)
						{
							Set(C, size, 0);
							watch.Restart();
							for (int j = 0; j < Samples; j++)
								Vector512Matmul(pa, pb, pc, size);
							watch.Stop();
							if (i >= Discard)
								data[i-Discard] = (double) watch.ElapsedMilliseconds / (double) Samples;
						}
						RecordResult(data, size, writer, "Vector512");
					}
					else
					{
      			writer.WriteLine("Vector512,,,,");
					}

					writer.WriteLine("MKL_sdot,,,,");
				}
			}
		}

    public static void RunBench(string mode, int iters)
    {
      StreamWriter writer = new StreamWriter("pre.csv");
      writer.WriteLine("name,size,mean,stddev,error");
      foreach (int size in Sizes)
      {
        RunBenchSize(mode, size, writer, 1 + Discard);
      }
      writer.Close();

      writer = new StreamWriter("results.csv");
      writer.WriteLine("name,size,mean (ms),stddev,error");

			int totalIters = iters + Discard;


      foreach (int size in Sizes)
      {
        RunBenchSize(mode, size, writer, totalIters);
      }

      writer.Close();

    }

  }
}
