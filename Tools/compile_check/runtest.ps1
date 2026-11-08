# Windows version of runtest.sh: compiles the scripts + test/Program.cs against stubs.cs and runs every check.
# Needs the .NET 8 SDK (winget install Microsoft.DotNet.SDK.8). Must end with "ALL CHECKS PASSED".
#   powershell -ExecutionPolicy Bypass -File Tools\compile_check\runtest.ps1
#   ... runtest.ps1 -HarnessArgs eelsump,frames.json     (extra harness commands: heldframes, meleeframes, eelsump ...)
# Builds into %TEMP%\agora_harness and runs from there (offline_squads.json and the dumps land in that folder, not in the repo).
param(
    [string]$Src = (Join-Path $PSScriptRoot "..\..\Assets\Scripts"),
    [string[]]$HarnessArgs = @()
)
$ErrorActionPreference = "Stop"
$HarnessArgs = @($HarnessArgs | ForEach-Object { $_ -split ',' })   # with powershell -File a list arrives as one comma-joined string
$dotnet = (Get-Command dotnet -ErrorAction SilentlyContinue).Source
if (-not $dotnet) { $dotnet = "C:\Program Files\dotnet\dotnet.exe" }
$root = Split-Path $dotnet
$sdk = Get-ChildItem (Join-Path $root "sdk") -Directory | Where-Object Name -like "8.0.*" | Sort-Object { [version]($_.Name -replace '-.*$','') } | Select-Object -Last 1
$pack = Get-ChildItem (Join-Path $root "packs\Microsoft.NETCore.App.Ref") -Directory -ErrorAction SilentlyContinue | Where-Object Name -like "8.0.*" | Sort-Object { [version]($_.Name -replace '-.*$','') } | Select-Object -Last 1
if (-not $sdk -or -not $pack) { Write-Error ".NET 8 SDK not found. Install it: winget install Microsoft.DotNet.SDK.8"; exit 2 }
$ref = Join-Path $pack.FullName "ref\net8.0"
$Src = (Resolve-Path $Src).Path
$out = Join-Path $env:TEMP "agora_harness"; New-Item -ItemType Directory -Force $out | Out-Null
# Editor/ and Net/ are Unity-only (editor tools, Netcode, Services): left out, as in runtest.sh
$files = Get-ChildItem $Src -Recurse -Filter *.cs | Where-Object { $_.FullName -notmatch '\\(Editor|Net)\\' } | ForEach-Object { $_.FullName }
$rsp = @("-nologo", "-noconfig", "-nostdlib", "-t:exe", "-langversion:9.0", "-out:$out\test.dll", "-nowarn:0168,0169,0219,0414,0649,0162,8981,0108,0114,0067")
$rsp += Get-ChildItem "$ref\*.dll" | ForEach-Object { "-r:`"$($_.FullName)`"" }
$rsp += "`"$PSScriptRoot\stubs.cs`""; $rsp += "`"$PSScriptRoot\test\Program.cs`""; $rsp += $files | ForEach-Object { "`"$_`"" }
Set-Content "$out\build.rsp" $rsp -Encoding utf8
$errs = & $dotnet "$($sdk.FullName)\Roslyn\bincore\csc.dll" "@$out\build.rsp" | Select-String "error"
if ($errs) { $errs | ForEach-Object { $_.Line }; Write-Host "BUILD FAILED"; exit 1 }
Set-Content "$out\test.runtimeconfig.json" '{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.NETCore.App","version":"8.0.0"}}}' -Encoding ascii
Push-Location $out
try { & $dotnet "$out\test.dll" @HarnessArgs; exit $LASTEXITCODE } finally { Pop-Location }
