param(
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$RootDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$OutputDir = Join-Path $RootDir "bin\$Configuration"

Write-Host "=== Zen Zakura Macro Build ===" -ForegroundColor Magenta
Write-Host "Configuration: $Configuration" -ForegroundColor Cyan

# Step 0: Setup VC environment
Write-Host "[0/4] Setting up VC++ x64 environment..." -ForegroundColor Yellow
$vcvars = "C:\Program Files (x86)\Microsoft Visual Studio\18\BuildTools\VC\Auxiliary\Build\vcvars64.bat"
cmd.exe /c "`"$vcvars`" && set > $env:TEMP\vcvars_env.txt"
Get-Content "$env:TEMP\vcvars_env.txt" | ForEach-Object {
    $kvp = $_ -split '=', 2
    if ($kvp.Count -eq 2) { Set-Item -Path "env:$($kvp[0])" -Value $kvp[1] }
}

# Step 1: Build C++ Core DLL
Write-Host "[1/4] Building ZenZakuraCore.dll..." -ForegroundColor Yellow
$coreProj = Join-Path $RootDir "ZenZakuraCore\ZenZakuraCore.vcxproj"
& msbuild $coreProj /p:Configuration=$Configuration /p:Platform=x64 /nologo /verbosity:minimal
if ($LASTEXITCODE -ne 0) { throw "C++ build failed" }
# Copy DLL to output
$coreDll = Join-Path $RootDir "ZenZakuraCore\x64\$Configuration\ZenZakuraCore.dll"
if (Test-Path $coreDll) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    Copy-Item $coreDll $OutputDir -Force
}

# Step 2: Restore NuGet + Build C# UI
Write-Host "[2/4] Building ZenZakuraUI.exe..." -ForegroundColor Yellow
$uiProj = Join-Path $RootDir "ZenZakuraUI\ZenZakuraUI.csproj"
& dotnet restore $uiProj
if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed" }
& dotnet publish $uiProj -c $Configuration -o "$OutputDir" --self-contained false
if ($LASTEXITCODE -ne 0) { throw "C# build failed" }




Write-Host "[4/4] Done!" -ForegroundColor Green
Write-Host "Output: $OutputDir\ZenZakuraUI.exe" -ForegroundColor Green
