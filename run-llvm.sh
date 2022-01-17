#!/bin/bash

export CORE_ROOT=/home/sdp/Projects/dotnet/runtime/artifacts/bin/mono/Linux.x64.Release/
export CORE_LIBRARIES=/home/sdp/Projects/dotnet/runtime/artifacts/bin/runtime/net7.0-Linux-Release-x64

if [[ -e out/coreclr ]]; then
  mkdir -p out/coreclr
fi

/home/sdp/Projects/dotnet/runtime/artifacts/bin/coreclr/Linux.x64.Release/corerun bin/Release/net7.0/linux-x64/publish/v512-bench.dll run llvm > log.txt
mv *.csv log.txt out/llvm
