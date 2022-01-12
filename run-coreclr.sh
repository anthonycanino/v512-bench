#!/bin/bash

export COMPlus_JitTieredCompilation=0
export COMPlus_JitDumpTier0=1

if [[ -e out/coreclr ]]; then
  mkdir -p out/coreclr
fi

/home/acanino/Projects/dotnet/runtime/artifacts/bin/coreclr/Linux.x64.Release/corerun bin/Release/net7.0/linux-x64/publish/v512-bench.dll run coreclr 
mv results.csv out/coreclr/
