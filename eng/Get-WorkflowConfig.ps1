<#
.SYNOPSIS
Reads the repository's packaging and workflow settings from Package.config.psd1.
#>

[CmdletBinding()]
param([string] $ConfigPath = (Join-Path $PSScriptRoot 'Package.config.psd1'))

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$ConfigPath = [IO.Path]::GetFullPath($ConfigPath)
$config = Import-PowerShellDataFile -LiteralPath $ConfigPath
foreach ($key in @('RepositoryRoot', 'AppProjectPath', 'WapProjectPath', 'PackageManifestPath', 'ArtifactsPath', 'DefaultConfiguration', 'RuntimeIdentifiers', 'Workflow')) {
    if (-not $config.ContainsKey($key)) { throw "Package configuration is missing '$key'." }
}
$repositoryRoot = [IO.Path]::GetFullPath($config.RepositoryRoot.Replace('\', '/'), (Split-Path -Parent $ConfigPath))

function Get-RepositoryPath([string] $Path, [switch] $MustExist) {
    if ([string]::IsNullOrWhiteSpace($Path)) { throw 'Configured paths must not be empty.' }
    $absolutePath = [IO.Path]::GetFullPath($Path.Replace('\', '/'), $repositoryRoot)
    $relativePath = [IO.Path]::GetRelativePath($repositoryRoot, $absolutePath).Replace('\', '/')
    if ([IO.Path]::IsPathRooted($relativePath) -or $relativePath -eq '..' -or $relativePath.StartsWith('../') -or $relativePath -eq '.') {
        throw "Configured path must stay inside the repository: '$Path'."
    }
    if ($MustExist -and -not (Test-Path -LiteralPath $absolutePath -PathType Leaf)) {
        throw "Configured file was not found: '$Path'."
    }
    return $relativePath
}

foreach ($key in @('DotNetVersion', 'WindowsSdkVersion', 'WingetPackageIdentifier', 'TestProjects')) {
    if (-not $config.Workflow.ContainsKey($key)) { throw "Workflow configuration is missing '$key'." }
}
foreach ($key in @('DotNetVersion', 'WindowsSdkVersion', 'WingetPackageIdentifier')) {
    if ([string]::IsNullOrWhiteSpace($config.Workflow[$key])) { throw "Workflow setting '$key' must not be empty." }
}
if ([string]::IsNullOrWhiteSpace($config.DefaultConfiguration)) { throw 'DefaultConfiguration must not be empty.' }
if ($config.RuntimeIdentifiers.Count -eq 0) { throw 'At least one runtime identifier is required.' }
$matrix = @(
    foreach ($platform in $config.RuntimeIdentifiers.Keys | Sort-Object) {
        if ($platform -notin @('x64', 'ARM64') -or $config.RuntimeIdentifiers[$platform] -ne "win-$($platform.ToLowerInvariant())") {
            throw "Unsupported platform/runtime mapping: '$platform'."
        }
        @{ platform = $platform; runtime = $config.RuntimeIdentifiers[$platform] }
    }
)
$manifestPath = Get-RepositoryPath $config.PackageManifestPath -MustExist
[xml] $manifest = Get-Content -LiteralPath (Join-Path $repositoryRoot $manifestPath) -Raw
if ([string]::IsNullOrWhiteSpace($manifest.Package.Identity.Name) -or [string]::IsNullOrWhiteSpace($manifest.Package.Identity.Publisher)) {
    throw 'The package manifest must define an identity and publisher.'
}
$testProjects = @($config.Workflow.TestProjects | ForEach-Object { Get-RepositoryPath $_ -MustExist })
if ($testProjects.Count -gt 0 -and 'x64' -notin $config.RuntimeIdentifiers.Keys) {
    throw 'The Windows CI test host requires the x64 runtime when test projects are configured.'
}

[pscustomobject]@{
    AppProjectPath = Get-RepositoryPath $config.AppProjectPath -MustExist
    WapProjectPath = Get-RepositoryPath $config.WapProjectPath -MustExist
    PackageManifestPath = $manifestPath
    ArtifactsPath = Get-RepositoryPath $config.ArtifactsPath
    DefaultConfiguration = $config.DefaultConfiguration
    DotNetVersion = $config.Workflow.DotNetVersion
    WindowsSdkVersion = $config.Workflow.WindowsSdkVersion
    WingetPackageIdentifier = $config.Workflow.WingetPackageIdentifier
    TestProjects = $testProjects
    Matrix = @{ include = $matrix }
    Architectures = @($matrix.platform | ForEach-Object { $_.ToLowerInvariant() })
    IdentityName = [string] $manifest.Package.Identity.Name
    Publisher = [string] $manifest.Package.Identity.Publisher
}
