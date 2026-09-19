param(
    [string]$Runtime = 'win-x64',
    [string]$Version = '0.9.0-beta.2'
)

$ErrorActionPreference = 'Stop'

$workspaceRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $workspaceRoot 'src\ScreenInk.App\ScreenInk.App.csproj'
$publishNuGetConfig = Join-Path $workspaceRoot 'packaging\NuGet.Publish.Config'
$artifactRoot = Join-Path $workspaceRoot 'artifacts\publish'
$output = Join-Path $artifactRoot $Runtime
$env:DOTNET_CLI_HOME = Join-Path $workspaceRoot '.dotnet_cli'
$env:NUGET_PACKAGES = Join-Path $workspaceRoot '.nuget\packages'
$env:APPDATA = Join-Path $workspaceRoot '.appdata'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'

if (Test-Path -LiteralPath $output) {
    $resolvedOutput = [System.IO.Path]::GetFullPath($output)
    $resolvedArtifactRoot = [System.IO.Path]::GetFullPath($artifactRoot) + [System.IO.Path]::DirectorySeparatorChar
    if (-not $resolvedOutput.StartsWith($resolvedArtifactRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clean publish output outside artifacts: $resolvedOutput"
    }

    Remove-Item -LiteralPath $resolvedOutput -Recurse -Force
}

dotnet publish $project `
    --configuration Release `
    --runtime $Runtime `
    --self-contained true `
    --output $output `
    --configfile $publishNuGetConfig `
    -p:Version=$Version `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None `
    -p:DebugSymbols=false

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

$executable = Join-Path $output 'ScreenInk.exe'
if (-not (Test-Path -LiteralPath $executable)) {
    throw "Published executable not found: $executable"
}

Write-Host "PASS Published Screen Ink $Version to $output"
