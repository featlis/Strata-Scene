param(
    [int]$WaitSeconds = 5,
    [switch]$KeepRunning = $false
)

$rootDir = Split-Path -Parent $PSScriptRoot
$dotnetRoot = "$env:LOCALAPPDATA\Microsoft\dotnet"
if (Test-Path $dotnetRoot) {
    $env:DOTNET_ROOT = $dotnetRoot
    $env:PATH = "$dotnetRoot;$env:PATH"
}

$exePath = Join-Path $rootDir "publish\StrataScene.App.exe"
if (-not (Test-Path $exePath)) {
    $exePath = Join-Path $rootDir "src\StrataScene.App\bin\Release\net10.0-windows\StrataScene.App.exe"
}

if (-not (Test-Path $exePath)) {
    Write-Error "StrataScene.App.exe not found. Please build or publish first."
    exit 1
}

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host " Strata Scene v0.1.0 Performance Benchmark  " -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "Executable: $exePath"
Write-Host "Starting process for measurement..."

$proc = Start-Process -FilePath $exePath -PassThru

try {
    Write-Host "Waiting $WaitSeconds seconds for idle stabilization..."
    Start-Sleep -Seconds $WaitSeconds

    $proc.Refresh()
    if ($proc.HasExited) {
        Write-Warning "Process exited prematurely with code $($proc.ExitCode)."
        exit 1
    }

    $workingSetMb = [Math]::Round($proc.WorkingSet64 / 1MB, 2)
    $privateMemoryMb = [Math]::Round($proc.PrivateMemorySize64 / 1MB, 2)
    $pagedMemoryMb = [Math]::Round($proc.PagedMemorySize64 / 1MB, 2)

    # Acceptance threshold (Task Manager Physical Working Set <= 40MB)
    $memoryPass = $workingSetMb -le 40.0

    Write-Host ""
    Write-Host "--- Benchmark Results ---" -ForegroundColor Yellow
    Write-Host ("Working Set (RAM):    {0,6} MB (Target <= 40 MB)" -f $workingSetMb)
    Write-Host ("Committed Virtual:    {0,6} MB" -f $privateMemoryMb)
    Write-Host ("Paged Memory:         {0,6} MB" -f $pagedMemoryMb)
    Write-Host ""

    if ($memoryPass) {
        Write-Host "PASS: Idle memory consumption meets performance target (<= 40 MB)." -ForegroundColor Green
    } else {
        Write-Host "WARN: Memory consumption exceeded target (> 40 MB)." -ForegroundColor Magenta
    }
}
finally {
    if (-not $KeepRunning -and -not $proc.HasExited) {
        Write-Host "Stopping measured process..."
        $proc.Kill()
    }
}
