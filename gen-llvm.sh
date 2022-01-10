#!/bin/bash

export CORE_ROOT=/home/acanino/Projects/dotnet/runtime/artifacts/bin/mono/Linux.x64.Debug/
export CORE_LIBRARIES=/home/acanino/Projects/dotnet/runtime/artifacts/bin/runtime/net7.0-Linux-Debug-x64
export MONO_VERBOSE_METHOD="RenderVector;RenderVector128;RenderVector256"

if [[ -e out/coreclr ]]; then
  mkdir -p out/coreclr
fi

rm *.txt
rm -f out/llvm/*

/home/acanino/Projects/dotnet/runtime/artifacts/bin/coreclr/Linux.x64.Debug/corerun bin/Release/net7.0/linux-x64/publish/v512-bench.dll 2>&1 1>llvm.txt

mv *.txt coreclr.dasm out/llvm
