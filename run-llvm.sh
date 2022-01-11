#!/bin/bash

export CORE_ROOT=/home/acanino/Projects/dotnet/runtime/artifacts/bin/mono/Linux.x64.Debug/
export CORE_LIBRARIES=/home/acanino/Projects/dotnet/runtime/artifacts/bin/runtime/net7.0-Linux-Debug-x64

if [[ -e out/coreclr ]]; then
  mkdir -p out/coreclr
fi

/home/acanino/Projects/dotnet/runtime/artifacts/bin/coreclr/Linux.x64.Debug/corerun bin/Release/net7.0/linux-x64/publish/v512-bench.dll run llvm > out/llvm/results.txt
