param(
    [string]$Version = '0.9.0-beta.2',
    [string]$InnoCompiler
)

$ErrorActionPreference = 'Stop'

$workspaceRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
& (Join-Path $workspaceRoot 'publish.ps1') -Runtime 'win-x64' -Version $Version

if (-not $InnoCompiler) {
    $candidates = @(
        $env:INNO_SETUP_COMPILER,
        (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 7\ISCC.exe'),
        'C:\Program Files\Inno Setup 7\ISCC.exe',
        'C:\Program Files (x86)\Inno Setup 6\ISCC.exe'
    ) | Where-Object { $_ -and (Test-Path -LiteralPath $_) }
    $InnoCompiler = $candidates | Select-Object -First 1
}

if (-not $InnoCompiler -or -not (Test-Path -LiteralPath $InnoCompiler)) {
    throw 'Inno Setup compiler was not found. Set INNO_SETUP_COMPILER or pass -InnoCompiler.'
}

$script = Join-Path $workspaceRoot 'packaging\ScreenInk.iss'
& $InnoCompiler "/DMyAppVersion=$Version" $script
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup failed with exit code $LASTEXITCODE."
}

$installer = Join-Path $workspaceRoot "artifacts\installer\ScreenInk-Setup-$Version-win-x64.exe"
if (-not (Test-Path -LiteralPath $installer)) {
    throw "Installer not found: $installer"
}

Write-Host "PASS Built installer $installer"
