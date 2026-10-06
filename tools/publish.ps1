param(
    [switch]$SelfContained = $false,
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$rootDir = Split-Path -Parent $PSScriptRoot
$dotnetRoot = "$env:LOCALAPPDATA\Microsoft\dotnet"
if (Test-Path $dotnetRoot) {
    $env:DOTNET_ROOT = $dotnetRoot
    $env:PATH = "$dotnetRoot;$env:PATH"
}

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host " Building & Publishing Strata Scene 0.1.0    " -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan

$publishDir = Join-Path $rootDir "publish"
$distDir = Join-Path $rootDir "dist"

if (Test-Path $publishDir) { Remove-Item -Recurse -Force $publishDir }
if (-not (Test-Path $distDir)) { New-Item -ItemType Directory -Force $distDir | Out-Null }

$selfContainedArg = if ($SelfContained) { "--self-contained true" } else { "--no-self-contained" }

Write-Host "[1/3] Running dotnet publish (win-x64)..." -ForegroundColor Yellow
$projectPath = Join-Path $rootDir "src\StrataScene.App\StrataScene.App.csproj"
& dotnet publish $projectPath -c $Configuration -r win-x64 $selfContainedArg -o $publishDir
if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host "[2/3] Checking Inno Setup compiler..." -ForegroundColor Yellow
$isccPath = "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $isccPath)) {
    $isccPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
}

if (Test-Path $isccPath) {
    Write-Host "[3/3] Compiling Inno Setup installer..." -ForegroundColor Yellow
    $issFile = Join-Path $rootDir "installer\StrataScene.iss"
    & "$isccPath" $issFile
    if ($LASTEXITCODE -ne 0) {
        Write-Error "ISCC failed with exit code $LASTEXITCODE"
        exit $LASTEXITCODE
    }

    $setupFile = Join-Path $distDir "StrataScene-Setup-0.1.0.exe"
    if (Test-Path $setupFile) {
        $sizeMb = [Math]::Round((Get-Item $setupFile).Length / 1MB, 2)
        Write-Host "=============================================" -ForegroundColor Green
        Write-Host " Installer created successfully!" -ForegroundColor Green
        Write-Host " Output: $setupFile ($sizeMb MB)" -ForegroundColor Green
        Write-Host "=============================================" -ForegroundColor Green
    }
} else {
    Write-Warning "Inno Setup compiler (ISCC.exe) was not found. Installer was not built."
}
