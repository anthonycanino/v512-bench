all: main mainasm

main: main.cpp
	clang++-10 -I/home/acanino/intel/oneapi/mkl/latest/include/ -L/home/acanino/intel/oneapi/mkl/latest/lib/intel64 -lmkl_rt -O3 -march=native main.cpp -o main

mainasm: main.cpp
	clang++-10 -I/home/acanino/intel/oneapi/mkl/latest/include/ -O3 -march=native -c -S main.cpp -o main.s

clean:
	rm main.s main
