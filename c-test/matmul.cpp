#include <cstdio>
#include <cstdlib>
#include <chrono>
#include <cmath>
#include <string>
#include <assert.h>

#include <immintrin.h>

#include "mkl.h"

int sizes[] = { 512, 1024 } ;
//int sizes[] = { 16 };

#define NDISCARD 	1
#define SAMPLES 	10

double stddev(double *num, int size) {
	double sum = 0.0;
	double mean = 0.0;
	double sd = 0.0;

	for (int i = 0; i < size; i++) {
		mean += num[i];
	}
	mean /= (double) size;

	for (int i = 0; i < size; i++) {
		sd += pow(num[i] - mean, 2);
	}

	sd = sqrt(sd / (double) size);
	return sd;
}


void record_result(const std::string& name, int size, double* data, int iters, FILE *f) {
  double mean = 0.0;
  for (int i = 0; i < iters; i++) {
    mean += data[i];
  }
  mean /= iters;

	double dev = stddev(data, iters);

  fprintf(f, "%s,%d,%lf,%lf\n", name.c_str(), size, mean, dev);
}

typedef struct {
  float* val;
  int M;
  int N;
} matrix_t;

#define MAT(m,i,j) (m->val[i*(m->M)+j])

matrix_t make_matrix(int M, int N) {
  matrix_t mat;
  mat.M = M;
  mat.N = N;
  mat.val = (float*) malloc(sizeof(float) * mat.M * mat.N);
  return mat;
}

void release_matrix(matrix_t *mat) {
  if (mat->val)
    free(mat->val);
  mat->val = NULL;
}

void dump_matrix(matrix_t *mat) {
  for (int i = 0; i < mat->M; i++) {
    for (int j = 0; j < mat->N; j++) {
      printf("%.2f ", MAT(mat,i,j));
    }
    printf("\n");
  }
}

void bcast_matrix(matrix_t *mat, int v) {
  for (int i = 0 ; i < mat->M; i++) {
    for (int j = 0; j < mat->N; j++) {
      MAT(mat,i,j) = v;
    }
  }
}

// 3x2 2x3 3x3

void run_no_vector_matmul(matrix_t *a, matrix_t *b, matrix_t *c) {
  assert(a->M == c->M && b->N == c->N && a->N == b->M);
  for (int i = 0; i < c->M; i++) {
    for (int k = 0; k < a->N; k++) {
      for (int j = 0; j < c->N; j++) {
        MAT(c,i,j) += MAT(a,i,k) * MAT(b,k,j);
      }
    }
  }
}

void run_vector_128_matmul(matrix_t *a, matrix_t *b, matrix_t *c) {
  assert(a->M == c->M && b->N == c->N && a->N == b->M);
  for (int i = 0; i < c->M; i++) {
    for (int k = 0; k < a->N; k++) {
      __m128 sv = _mm_set1_ps(MAT(a,i,k));
      for (int j = 0; j < c->N; j += 4) {
        __m128 cv = _mm_loadu_ps(&MAT(c,i,j));
        __m128 bv = _mm_loadu_ps(&MAT(b,i,j));
        __m128 tv = _mm_mul_ps(bv, sv);
        cv = _mm_add_ps(tv, cv);
        _mm_storeu_ps(&MAT(c,i,j), cv);
      }
    }
  }
}

void run_vector_256_matmul(matrix_t *a, matrix_t *b, matrix_t *c) {
  assert(a->M == c->M && b->N == c->N && a->N == b->M);
  for (int i = 0; i < c->M; i++) {
    for (int k = 0; k < a->N; k++) {
      __m256 sv = _mm256_set1_ps(MAT(a,i,k));
      for (int j = 0; j < c->N; j += 8) {
        __m256 cv = _mm256_loadu_ps(&MAT(c,i,j));
        __m256 bv = _mm256_loadu_ps(&MAT(b,i,j));
        __m256 tv = _mm256_mul_ps(bv, sv);
        cv = _mm256_add_ps(tv, cv);
        _mm256_storeu_ps(&MAT(c,i,j), cv);
      }
    }
  }
}

void run_vector_512_matmul(matrix_t *a, matrix_t *b, matrix_t *c) {
  assert(a->M == c->M && b->N == c->N && a->N == b->M);
  for (int i = 0; i < c->M; i++) {
    for (int k = 0; k < a->N; k++) {
      __m512 sv = _mm512_set1_ps(MAT(a,i,k));
      for (int j = 0; j < c->N; j += 16) {
        __m512 cv = _mm512_loadu_ps(&MAT(c,i,j));
        __m512 bv = _mm512_loadu_ps(&MAT(b,i,j));
        __m512 tv = _mm512_mul_ps(bv, sv);
        cv = _mm512_add_ps(tv, cv);
        _mm512_storeu_ps(&MAT(c,i,j), cv);
      }
    }
  }
}



/*
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
*/

void run_debug(int size, int iters) {
  matrix_t a = make_matrix(size, size);
  matrix_t b = make_matrix(size, size);
  matrix_t c = make_matrix(size, size);

  bcast_matrix(&a, 2.0f);
  bcast_matrix(&b, 3.0f);

  printf("== matrix a (%d, %d)==\n", a.M, a.N);
  dump_matrix(&a);

  printf("== matrix b (%d, %d)==\n", b.M, b.N);
  dump_matrix(&b);

  bcast_matrix(&c, 0.0f);
  run_no_vector_matmul(&a, &b, &c);

  printf("== (no vector) matrix c (%d, %d)==\n", c.M, c.N);
  dump_matrix(&c);

  bcast_matrix(&c, 0.0f);
  run_vector_128_matmul(&a, &b, &c);

  printf("== (vector 128) matrix c (%d, %d)==\n", c.M, c.N);
  dump_matrix(&c);

  bcast_matrix(&c, 0.0f);
  run_vector_256_matmul(&a, &b, &c);

  printf("== (vector 256) matrix c (%d, %d)==\n", c.M, c.N);
  dump_matrix(&c);

  bcast_matrix(&c, 0.0f);
  run_vector_512_matmul(&a, &b, &c);

  printf("== (vector 512) matrix c (%d, %d)==\n", c.M, c.N);
  dump_matrix(&c);

	bcast_matrix(&c, 0.0f);
  cblas_sgemm(CblasRowMajor, CblasNoTrans, CblasNoTrans, a.M, b.N, a.N, 1, a.val, a.M, b.val, b.M, 0.0, c.val, c.M);

  printf("== (MKL_sgemm) matrix c (%d, %d)==\n", c.M, c.N);
  dump_matrix(&c);

  release_matrix(&a);
  release_matrix(&b);
  release_matrix(&c);
}

void run_bench(int size, int iters, FILE *f) {
  printf("Running %d iters with %d samples on size %d\n", iters, SAMPLES, size);

  matrix_t a = make_matrix(size, size);
  matrix_t b = make_matrix(size, size);
  matrix_t c = make_matrix(size, size);

  bcast_matrix(&a, 2.0f);
  bcast_matrix(&b, 3.0f);

  double *data = (double*) malloc(sizeof(double) * (iters-NDISCARD));

  bcast_matrix(&c, 0.0f);
  for (int i = 0; i < iters; i++) {
    auto begin = std::chrono::steady_clock::now();
		for (int i = 0; i < SAMPLES; i++)
			run_no_vector_matmul(&a, &b, &c);
    auto end = std::chrono::steady_clock::now();
    auto elapsed = std::chrono::duration_cast<std::chrono::milliseconds>(end - begin);
    if (i >= NDISCARD) {
      data[i-NDISCARD] = (double) elapsed.count() / (double) SAMPLES;
		}
  }
  record_result("NoVector", size, data, iters-NDISCARD, f);

  bcast_matrix(&c, 0.0f);
  for (int i = 0; i < iters; i++) {
    auto begin = std::chrono::steady_clock::now();
		for (int i = 0; i < SAMPLES; i++)
			run_vector_128_matmul(&a, &b, &c);
    auto end = std::chrono::steady_clock::now();
    auto elapsed = std::chrono::duration_cast<std::chrono::milliseconds>(end - begin);
    if (i >= NDISCARD) {
      data[i-NDISCARD] = (double) elapsed.count() / (double) SAMPLES;
		}
  }
  record_result("Vector128", size, data, iters-NDISCARD, f);

  bcast_matrix(&c, 0.0f);
  for (int i = 0; i < iters; i++) {
    auto begin = std::chrono::steady_clock::now();
		for (int i = 0; i < SAMPLES; i++)
			run_vector_256_matmul(&a, &b, &c);
    auto end = std::chrono::steady_clock::now();
    auto elapsed = std::chrono::duration_cast<std::chrono::milliseconds>(end - begin);
    if (i >= NDISCARD) {
      data[i-NDISCARD] = (double) elapsed.count() / (double) SAMPLES;
		}
  }
  record_result("Vector256", size, data, iters-NDISCARD, f);

	bcast_matrix(&c, 0.0f);
  for (int i = 0; i < iters; i++) {
    auto begin = std::chrono::steady_clock::now();
		for (int i = 0; i < SAMPLES; i++)
			run_vector_512_matmul(&a, &b, &c);
    auto end = std::chrono::steady_clock::now();
    auto elapsed = std::chrono::duration_cast<std::chrono::milliseconds>(end - begin);
    if (i >= NDISCARD) {
      data[i-NDISCARD] = (double) elapsed.count() / (double) SAMPLES;
		}
  }
  record_result("Vector512", size, data, iters-NDISCARD, f);


	bcast_matrix(&c, 0.0f);
  for (int i = 0; i < iters; i++) {
    auto begin = std::chrono::steady_clock::now();
		for (int i = 0; i < SAMPLES; i++)
			cblas_sgemm(CblasRowMajor, CblasNoTrans, CblasNoTrans, a.M, b.N, a.N, 1, a.val, a.M, b.val, b.M, 0.0, c.val, c.M);
    auto end = std::chrono::steady_clock::now();
    auto elapsed = std::chrono::duration_cast<std::chrono::milliseconds>(end - begin);
    if (i >= NDISCARD) {
      data[i-NDISCARD] = (double) elapsed.count() / (double) SAMPLES;
		}
  }
  record_result("MKL_sgemm", size, data, iters-NDISCARD, f);


  release_matrix(&a);
  release_matrix(&b);
  release_matrix(&c);
}


int main(int argc, char **argv) {
  if (argc < 2) {
    printf("%s usage: [ITERATIONS]\n", argv[0]);
    return 1;
  }

  //run_debug(sizes[0], 1);

  int iters = strtod(argv[1], NULL) + NDISCARD;

  // sanity check
  for (int i = 0; i < sizeof(sizes)/sizeof(sizes[0]); i++) {
    if (sizes[i] % 4 != 0 || sizes[i] % 8 != 0 || sizes[i] % 16 != 0) {
      printf("error: benchmark size %d does not evenly divide into vectors\n", sizes[i]);
      return 1;
    }
  }

  FILE *f = fopen("matmul-results.csv", "w+");
  if (!f) {
    perror("fopen");
    return 1;
  }
  fprintf(f, "name,size,mean (ms),stddev\n");

  for (int i = 0; i < sizeof(sizes)/sizeof(sizes[0]); i++) {
    run_bench(sizes[i], iters, f);
  }

  return 0;
}
