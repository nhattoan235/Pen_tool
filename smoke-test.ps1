$ErrorActionPreference = 'Stop'

$workspaceRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$env:SCREENINK_DATA_DIRECTORY = Join-Path $workspaceRoot 'artifacts\smoke-data'

$executable = Join-Path $workspaceRoot 'src\ScreenInk.App\bin\Debug\net10.0-windows\ScreenInk.exe'
if (-not (Test-Path -LiteralPath $executable)) {
    throw "Screen Ink executable not found. Run .\build.ps1 first."
}

$process = Start-Process -FilePath $executable -ArgumentList '--smoke-test' -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0) {
    throw "Screen Ink smoke test exited with code $($process.ExitCode)."
}

Write-Host 'PASS Screen Ink tray lifecycle smoke test.'
