#!/usr/bin/env pwsh
# Fallout build bootstrapper. Runs the build project directly (no global tool needed).
#   ./build.ps1 Test      # run the spec suite
#   ./build.ps1 Pack      # test + pack the NuGet package
#   ./build.ps1           # default target (Pack)
[CmdletBinding()]
Param([Parameter(ValueFromRemainingArguments = $true)] [string[]] $BuildArguments)

$ErrorActionPreference = 'Stop'
dotnet run --project "$PSScriptRoot/build/_build.csproj" -- @BuildArguments
exit $LASTEXITCODE
