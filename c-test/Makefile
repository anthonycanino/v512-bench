all: main mainasm

main: main.cpp
	clang++ -O1 -mavx2 -mavx512f -g main.cpp -o main

mainasm: main.cpp
	clang++ -O1 -mavx2 -mavx512f -c -S -O2 main.cpp -o main.s

clean:
	rm main.s main
