$ErrorActionPreference = 'Stop'

$workspaceRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$env:DOTNET_CLI_HOME = Join-Path $workspaceRoot '.dotnet_cli'
$env:NUGET_PACKAGES = Join-Path $workspaceRoot '.nuget\packages'
$env:APPDATA = Join-Path $workspaceRoot '.appdata'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'

dotnet run --project (Join-Path $workspaceRoot 'tests\ScreenInk.Core.Tests\ScreenInk.Core.Tests.csproj') --configuration Debug --no-restore
