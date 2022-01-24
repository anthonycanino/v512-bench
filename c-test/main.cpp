#include <cstdio>
#include <cstdlib>
#include <chrono>
#include <string>

#include <immintrin.h>

#include "mkl.h"

int sizes[] = { 128, 256, 512, 1024, 2048, 4096, 8192, 16384 };

#define NDISCARD 10
#define SAMPLES 20000

void record_result(const std::string& name, int size, double* data, int iters, FILE *f) {
  double mean = 0.0;
  for (int i = 0; i < iters; i++) {
    mean += data[i];
  }
  mean /= iters;
  fprintf(f, "%s,%d,%lf\n", name.c_str(), size, mean);
}

float run_no_vector_reduce(float *l, float *r, int size) {
  float res = 0.0;
  for (int i = 0; i < size; i++) {
    res += l[i] * r[i];
  }
  return res;
}

float run_vector_128_reduce(float *l, float *r, int size) {
  __m128 resv = _mm_setzero_ps();
  for (int i = 0; i < size; i += 4) {
    __m128 lv = _mm_loadu_ps(l + i);
    __m128 rv = _mm_loadu_ps(r + i);
    __m128 ov = _mm_mul_ps(lv, rv);
    resv       = _mm_add_ps(ov, resv);
  }
  resv = _mm_hadd_ps(resv, resv);
  resv = _mm_hadd_ps(resv, resv);

  return _mm_cvtss_f32(resv);
}

float run_vector_256_reduce(float *l, float *r, int size) {
  __m256 resv = _mm256_setzero_ps();
  for (int i = 0; i < size; i += 8) {
    __m256 lv = _mm256_loadu_ps(l + i);
    __m256 rv = _mm256_loadu_ps(r + i);
    __m256 ov = _mm256_mul_ps(lv, rv);
    resv       = _mm256_add_ps(ov, resv);
  }
  __m128 low = _mm256_extractf128_ps(resv, 0);
  __m128 high = _mm256_extractf128_ps(resv, 0);
  __m128 hres = _mm_add_ps(low, high);
  hres = _mm_hadd_ps(hres, hres);
  hres = _mm_hadd_ps(hres, hres);

  return _mm_cvtss_f32(hres);
}

float run_vector_512_reduce(float *l, float *r, int size) {
  float res = 0.0;
  __m512 resv = _mm512_setzero_ps();
  for (int i = 0; i < size; i += 16) {
    __m512 lv = _mm512_loadu_ps(l + i);
    __m512 rv = _mm512_loadu_ps(r + i);
    __m512 ov = _mm512_mul_ps(lv, rv);
    resv      = _mm512_add_ps(ov, resv);
  }
  return _mm512_reduce_add_ps(resv);
}

void run_bench(int size, int iters, FILE *f) {
  float *left = (float*) malloc(sizeof(float) * size);
  float *right = (float*) malloc(sizeof(float) * size);
  for (int i = 0 ; i < size; i++) {
    left[i] = 2;
    right[i] = 2;
  }

  float res = 0;
  double *data = (double*) malloc(sizeof(double) * (iters-NDISCARD));

  for (int i = 0; i < iters; i++) {
    auto begin = std::chrono::steady_clock::now();
		for (int i = 0; i < SAMPLES; i++)
			res = run_no_vector_reduce(left, right, size);
    auto end = std::chrono::steady_clock::now();
    auto elapsed = std::chrono::duration_cast<std::chrono::microseconds>(end - begin);
    if (i >= NDISCARD) {
      data[i-NDISCARD] = (double) elapsed.count() / (double) SAMPLES;
		}
    if (i + 1 == iters)
      printf("no_vector_reduce: %f\n", res);
  }
  record_result("NoVector", size, data, iters-NDISCARD, f);

  for (int i = 0; i < iters; i++) {
    auto begin = std::chrono::steady_clock::now();
		for (int i = 0; i < SAMPLES; i++)
			res = run_vector_128_reduce(left, right, size);
    auto end = std::chrono::steady_clock::now();
    auto elapsed = std::chrono::duration_cast<std::chrono::microseconds>(end - begin);
    if (i >= NDISCARD)
      data[i-NDISCARD] = (double) elapsed.count() / (double) SAMPLES;
    if (i + 1 == iters)
      printf("vector_128_reduce: %f\n", res);
  }
  record_result("Vector128", size, data, iters-NDISCARD, f);

  for (int i = 0; i < iters; i++) {
    auto begin = std::chrono::steady_clock::now();
		for (int i = 0; i < SAMPLES; i++)
			res = run_vector_256_reduce(left, right, size);
    auto end = std::chrono::steady_clock::now();
    auto elapsed = std::chrono::duration_cast<std::chrono::microseconds>(end - begin);
    if (i >= NDISCARD)
      data[i-NDISCARD] = (double) elapsed.count() / (double) SAMPLES;
    if (i + 1 == iters)
      printf("vector_256_reduce: %f\n", res);
  }
  record_result("Vector256", size, data, iters-NDISCARD, f);

  for (int i = 0; i < iters; i++) {
    auto begin = std::chrono::steady_clock::now();
		for (int i = 0; i < SAMPLES; i++)
			res = run_vector_512_reduce(left, right, size);
    auto end = std::chrono::steady_clock::now();
    auto elapsed = std::chrono::duration_cast<std::chrono::microseconds>(end - begin);
    if (i >= NDISCARD)
      data[i-NDISCARD] = (double) elapsed.count() / (double) SAMPLES;
    if (i + 1 == iters)
      printf("vector_512_reduce: %f\n", res);
  }
  record_result("Vector512", size, data, iters-NDISCARD, f);

  for (int i = 0; i < iters; i++) {
    auto begin = std::chrono::steady_clock::now();
		for (int i = 0; i < SAMPLES; i++)
			res = cblas_sdot(size, left, 1, right, 1);
    auto end = std::chrono::steady_clock::now();
    auto elapsed = std::chrono::duration_cast<std::chrono::microseconds>(end - begin);
    if (i >= NDISCARD)
      data[i-NDISCARD] = (double) elapsed.count() / (double) SAMPLES;
    if (i + 1 == iters)
      printf("cblas_sdot: %f\n", res);
  }
  record_result("MKL_sdot", size, data, iters-NDISCARD, f);

  free(data);
  free(left);
  free(right);
}


int main(int argc, char **argv) {
  if (argc < 2) {
    printf("%s usage: [ITERATIONS]\n", argv[0]);
    return 1;
  }
  
  int iters = strtod(argv[1], NULL) + NDISCARD;

  // sanity check
  for (int i = 0; i < sizeof(sizes)/sizeof(sizes[0]); i++) {
    if (sizes[i] % 4 != 0 || sizes[i] % 8 != 0 || sizes[i] % 16 != 0) {
      printf("error: benchmark size %d does not evenly divide into vectors\n", sizes[i]);
      return 1;
    }
  }


  FILE *f = fopen("results.csv", "w+");
  if (!f) {
    perror("fopen");
    return 1;
  }
  fprintf(f, "name,size,mean (us)\n");

  for (int i = 0; i < sizeof(sizes)/sizeof(sizes[0]); i++) {
    run_bench(sizes[i], iters, f);
  }

  return 0;
}
