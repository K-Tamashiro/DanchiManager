[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$project = Join-Path $repoRoot 'DanchiManager\DanchiManager.csproj'
[xml]$projectXml = Get-Content -LiteralPath $project -Raw
$version = [string]$projectXml.Project.PropertyGroup.Version
if ($version -notmatch '^\d+\.\d+\.\d+([.-][A-Za-z0-9.-]+)?$') { throw 'Invalid package version.' }
$packageName = "DanchiManager-v$version-win-x64"
$releaseRoot = Join-Path $repoRoot 'release'
# Each publish uses a new folder so files from previous releases cannot enter the ZIP.
$stage = Join-Path $releaseRoot ($packageName + '-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $stage -Force | Out-Null
& dotnet publish $project -p:PublishProfile=Release-win-x64 -c Release -r win-x64 --self-contained false -o $stage
if ($LASTEXITCODE -ne 0) { throw "Publish failed: $LASTEXITCODE" }
foreach ($name in @('DanchiManager.exe', 'DanchiManager.dll', 'DanchiManager.runtimeconfig.json', 'e_sqlite3.dll')) {
    if (!(Test-Path -LiteralPath (Join-Path $stage $name) -PathType Leaf)) { throw "Missing package file: $name" }
}
Copy-Item -LiteralPath (Join-Path $repoRoot 'README.md') -Destination $stage
$expected = @(
    'DanchiManager.exe', 'DanchiManager.dll', 'DanchiManager.deps.json',
    'DanchiManager.runtimeconfig.json', 'CommunityToolkit.Mvvm.dll',
    'Microsoft.Data.Sqlite.dll', 'SQLitePCLRaw.batteries_v2.dll',
    'SQLitePCLRaw.core.dll', 'SQLitePCLRaw.provider.e_sqlite3.dll',
    'e_sqlite3.dll', 'README.md'
)
$actual = @(Get-ChildItem -LiteralPath $stage -Recurse -File | ForEach-Object {
    $_.FullName.Substring($stage.Length + 1).Replace('\', '/')
})
if (Compare-Object $expected $actual) { throw 'Unexpected package files. Review published dependencies before distributing.' }
$runtime = Get-Content -LiteralPath (Join-Path $stage 'DanchiManager.runtimeconfig.json') -Raw | ConvertFrom-Json
if ($runtime.runtimeOptions.includedFrameworks -or
    !($runtime.runtimeOptions.frameworks | Where-Object { $_.name -eq 'Microsoft.WindowsDesktop.App' -and $_.version.StartsWith('10.') })) {
    throw 'Expected a framework-dependent .NET 10 Desktop Runtime package.'
}
$archive = Join-Path $releaseRoot ($packageName + '.zip')
if (Test-Path -LiteralPath $archive) {
    $archive = Join-Path $releaseRoot ($packageName + '-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N').Substring(0,8) + '.zip')
}
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($stage, $archive)
Write-Output "Package: $archive"
Write-Output "Published directory: $stage"
