#!/bin/bash

export COMPlus_JitTieredCompilation=0
export COMPlus_JitDumpTier0=1
export COMPlus_JitDisasm="Vector128Matmul Vector256Matmul"
export COMPlus_JitDiffableDsasm=1

if [[ ! -e out/coreclr ]]; then
  mkdir -p out/coreclr
fi

rm *.txt

rm -f out/coreclr/*

/home/acanino/Projects/dotnet/runtime/artifacts/bin/coreclr/Linux.x64.Debug/corerun bin/Release/net7.0/linux-x64/publish/v512-bench.dll matmul diag coreclr 1 > coreclr.dasm

mv *.txt coreclr.dasm out/coreclr
