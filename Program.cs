using System;
using System.Numerics;
using System.Threading.Tasks;

using System.Runtime.CompilerServices;

using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using System.IO;

namespace issues
{
    class Program
    {

        private const float limit = 4.0f;
        protected const int max_iters = 1000; // Make this higher to see more detail when zoomed in (and slow down rendering a lot)

        // Helper to construct a vector from a lambda that takes an
        // index. It's not efficient, but it's more succint than the
        // corresponding for loop.  Don't use it on a hot code path
        // (i.e. inside a loop)
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Create<T>(Func<int, T> creator) where T : struct
        {
            T[] data = new T[Vector<T>.Count];
            for (int i = 0; i < data.Length; i++)
                data[i] = creator(i);
            return new Vector<T>(data);
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static Vector128<T> Create128<T>(Func<int, T> creator) where T : struct
        {
            T[] data = new T[Vector128<T>.Count];
            for (int i = 0; i < data.Length; i++)
                data[i] = creator(i);
            return Vector128.Create(data);
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static Vector256<T> Create256<T>(Func<int, T> creator) where T : struct
        {
            T[] data = new T[Vector256<T>.Count];
            for (int i = 0; i < data.Length; i++)
                data[i] = creator(i);
            return Vector256.Create(data);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void RenderVector(float xmin, float xmax, float ymin, float ymax, float step, StreamWriter writer)
        {
            Vector<int> vmax_iters = new Vector<int>(max_iters);
            Vector<float> vlimit = new Vector<float>(limit);
            Vector<float> vstep = new Vector<float>(step);
            Vector<float> vxmax = new Vector<float>(xmax);
            Vector<float> vinc = new Vector<float>((float)Vector<float>.Count * step);
            Vector<float> vxmin = Create(i => xmin + step * i);

            float y = ymin;
            int yp = 0;
            for (Vector<float> vy = new Vector<float>(ymin); y <= ymax; vy += vstep, y += step, yp++)
            {
                int xp = 0;
                for (Vector<float> vx = vxmin; Vector.LessThanOrEqualAny(vx, vxmax); vx += vinc, xp += Vector<int>.Count)
                {
                    Vector<float> accumx = vx;
                    Vector<float> accumy = vy;

                    Vector<int> viters = Vector<int>.Zero;
                    Vector<int> increment = Vector<int>.One;
                    do
                    {
                        Vector<float> naccumx = accumx * accumx - accumy * accumy;
                        Vector<float> naccumy = accumx * accumy + accumx * accumy;
                        accumx = naccumx + vx;
                        accumy = naccumy + vy;
                        viters += increment;
                        Vector<float> sqabs = accumx * accumx + accumy * accumy;
                        Vector<int> vCond = Vector.LessThanOrEqual(sqabs, vlimit) &
                            Vector.LessThanOrEqual(viters, vmax_iters);
                        increment = increment & vCond;
                    } while (increment != Vector<int>.Zero);


                    for (int eNum = 0; eNum < Vector<int>.Count; eNum++) 
                    {
                      writer.WriteLine("x: " + xp + eNum + " y: " + yp + " iter: " + viters[eNum]);
                    }
                }
            }
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void RenderVector128(float xmin, float xmax, float ymin, float ymax, float step, StreamWriter writer)
        {
            Vector128<int> vmax_iters = Vector128.Create<int>(max_iters);
            Vector128<float> vlimit   = Vector128.Create<float>(limit);
            Vector128<float> vstep    = Vector128.Create<float>(step);
            Vector128<float> vxmax    = Vector128.Create<float>(xmax);
            Vector128<float> vinc     = Vector128.Create<float>((float)Vector128<float>.Count * step);
            Vector128<float> vxmin = Create128(i => xmin + step * i);

            float y = ymin;
            int yp = 0;
            for (Vector128<float> vy = Vector128.Create<float>(ymin); y <= ymax; vy += vstep, y += step, yp++)
            {
                int xp = 0;
                for (Vector128<float> vx = vxmin; Vector128.LessThanOrEqualAny(vx, vxmax); vx += vinc, xp += Vector128<int>.Count)
                {
                    Vector128<float> accumx = vx;
                    Vector128<float> accumy = vy;

                    Vector128<int> viters = Vector128<int>.Zero;
                    Vector128<int> increment = Vector128.Create<int>(1);
                    do
                    {
                        Vector128<float> naccumx = accumx * accumx - accumy * accumy;
                        Vector128<float> naccumy = accumx * accumy + accumx * accumy;
                        accumx = naccumx + vx;
                        accumy = naccumy + vy;
                        viters += increment;
                        Vector128<float> sqabs = accumx * accumx + accumy * accumy;
                        Vector128<int> vCond = Vector128.LessThanOrEqual(sqabs, vlimit).As<float,int>() & Vector128.LessThanOrEqual(viters, vmax_iters);
                        increment = increment & vCond;
                    } while (increment != Vector128<int>.Zero);

                    for (int eNum = 0; eNum < Vector128<int>.Count; eNum++) 
                    {
                      writer.WriteLine("x: " + xp + eNum + " y: " + yp + " iter: " + viters[eNum]);
                    }
                }
            }
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void RenderVector256(float xmin, float xmax, float ymin, float ymax, float step, StreamWriter writer)
        {
            Vector256<int> vmax_iters = Vector256.Create<int>(max_iters);
            Vector256<float> vlimit   = Vector256.Create<float>(limit);
            Vector256<float> vstep    = Vector256.Create<float>(step);
            Vector256<float> vxmax    = Vector256.Create<float>(xmax);
            Vector256<float> vinc     = Vector256.Create<float>((float)Vector256<float>.Count * step);
            Vector256<float> vxmin = Create256(i => xmin + step * i);

            float y = ymin;
            int yp = 0;
            for (Vector256<float> vy = Vector256.Create<float>(ymin); y <= ymax; vy += vstep, y += step, yp++)
            {
                int xp = 0;
                for (Vector256<float> vx = vxmin; Vector256.LessThanOrEqualAny(vx, vxmax); vx += vinc, xp += Vector256<int>.Count)
                {
                    Vector256<float> accumx = vx;
                    Vector256<float> accumy = vy;

                    Vector256<int> viters = Vector256<int>.Zero;
                    Vector256<int> increment = Vector256.Create<int>(1);
                    do
                    {
                        Vector256<float> naccumx = accumx * accumx - accumy * accumy;
                        Vector256<float> naccumy = accumx * accumy + accumx * accumy;
                        accumx = naccumx + vx;
                        accumy = naccumy + vy;
                        viters += increment;
                        Vector256<float> sqabs = accumx * accumx + accumy * accumy;
                        Vector256<int> vCond = Vector256.LessThanOrEqual(sqabs, vlimit).As<float,int>() & Vector256.LessThanOrEqual(viters, vmax_iters);
                        increment = increment & vCond;
                    } while (increment != Vector256<int>.Zero);

                    for (int eNum = 0; eNum < Vector256<int>.Count; eNum++) 
                    {
                      writer.WriteLine("x: " + xp + eNum + " y: " + yp + " iter: " + viters[eNum]);
                    }
                }
            }
        }

      public static void XBench(int iters)
      {
          float XC = -1.248f;
          float YC = -.0362f;
          float Range = .001f;
          float xmin = XC - Range;
          float xmax = XC + Range;
          float ymin = YC - Range;
          float ymax = YC + Range;
          float step = Range / 100f;

          StreamWriter renderVectorWriter = new StreamWriter("RenderVector.txt");
          for (int count = 0; count < iters; count++)
          {
            renderVectorWriter.WriteLine("Count: " + count);
            RenderVector(xmin, xmax, ymin, ymax, step, renderVectorWriter);
          }
          renderVectorWriter.Close();

          StreamWriter renderVectorWriter128 = new StreamWriter("RenderVector128.txt");
          for (int count = 0; count < iters; count++)
          {
            renderVectorWriter128.WriteLine("Count: " + count);
            RenderVector128(xmin, xmax, ymin, ymax, step, renderVectorWriter128);
          }
          renderVectorWriter128.Close();

          StreamWriter renderVectorWriter256 = new StreamWriter("RenderVector256.txt");
          for (int count = 0; count < iters; count++)
          {
            renderVectorWriter256.WriteLine("Count: " + count);
            RenderVector256(xmin, xmax, ymin, ymax, step, renderVectorWriter256);
          }
          renderVectorWriter256.Close();


      }

      public static void Main(string[] args)
      {
        XBench(1);
      }
    }
}
