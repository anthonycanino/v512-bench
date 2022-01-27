all: dot dotasm matmul matmulasm

dot: dot.cpp
	clang++-10 -I/home/acanino/intel/oneapi/mkl/latest/include/ -L/home/acanino/intel/oneapi/mkl/latest/lib/intel64 -lmkl_rt -O3 -march=native dot.cpp -o dot

dotasm: dot.cpp
	clang++-10 -I/home/acanino/intel/oneapi/mkl/latest/include/ -O3 -march=native -c -S dot.cpp -o dot.s

matmul: matmul.cpp
	clang++-10 -I/home/acanino/intel/oneapi/mkl/latest/include/ -L/home/acanino/intel/oneapi/mkl/latest/lib/intel64 -lmkl_rt -O3 -march=native matmul.cpp -o matmul

matmulasm: matmul.cpp
	clang++-10 -I/home/acanino/intel/oneapi/mkl/latest/include/ -O3 -march=native -c -S matmul.cpp -o matmul.s


clean:
	rm dot.s dot
