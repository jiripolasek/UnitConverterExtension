<#
.SYNOPSIS
Creates local MSIX fixtures for publishing checks, without building or signing.
#>

function New-TestMsix {
    param(
        [string] $Path,
        [string] $Architecture,
        [string] $IdentityName,
        [string] $Publisher,
        [string] $Version = '0.4.0.0',
        [switch] $Signed
    )
    $archive = [IO.Compression.ZipFile]::Open($Path, [IO.Compression.ZipArchiveMode]::Create)
    try {
        $entry = $archive.CreateEntry('AppxManifest.xml')
        $writer = [IO.StreamWriter]::new($entry.Open())
        try {
            $identityNameXml = [Security.SecurityElement]::Escape($IdentityName)
            $publisherXml = [Security.SecurityElement]::Escape($Publisher)
            $writer.Write("<Package><Identity Name='$identityNameXml' Publisher='$publisherXml' Version='$Version' ProcessorArchitecture='$Architecture' /></Package>")
        }
        finally { $writer.Dispose() }
        if ($Signed) {
            $signature = $archive.CreateEntry('AppxSignature.p7x')
            $stream = $signature.Open()
            $stream.Dispose()
        }
    }
    finally { $archive.Dispose() }
}

function New-TestMsixUpload {
    param([string] $Path, [string] $Architecture, [string] $IdentityName, [string] $Publisher, [string] $Version = '0.4.0.0')
    $packagePath = Join-Path ([IO.Path]::GetTempPath()) "$([guid]::NewGuid().ToString('N')).msix"
    try {
        New-TestMsix -Path $packagePath -Architecture $Architecture -IdentityName $IdentityName -Publisher $Publisher -Version $Version
        $archive = [IO.Compression.ZipFile]::Open($Path, [IO.Compression.ZipArchiveMode]::Create)
        try { [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $packagePath, 'App.msix') | Out-Null }
        finally { $archive.Dispose() }
    }
    finally { Remove-Item -LiteralPath $packagePath }
}

function New-TestWorkflowRepository {
    param([string] $Directory, [string[]] $Architectures = @('x64', 'ARM64'))
    foreach ($file in @('Example Extension.csproj', 'Example.Package.wapproj', 'Example.Tests.csproj')) {
        Set-Content -LiteralPath (Join-Path $Directory $file) -Value '<Project />'
    }
    Set-Content -LiteralPath (Join-Path $Directory 'Package.appxmanifest') -Value "<Package><Identity Name='Contoso.ExampleExtension' Publisher='CN=ExamplePublisher' Version='0.4.0.0' /></Package>"
    $runtimeEntries = ($Architectures | ForEach-Object { "$_ = 'win-$($_.ToLowerInvariant())'" }) -join '; '
    $configPath = Join-Path $Directory 'Package.config.psd1'
    @"
@{
    RepositoryRoot = '.'
    AppProjectPath = 'Example Extension.csproj'
    WapProjectPath = 'Example.Package.wapproj'
    PackageManifestPath = 'Package.appxmanifest'
    ArtifactsPath = 'output packages'
    DefaultConfiguration = 'Release'
    RuntimeIdentifiers = @{ $runtimeEntries }
    Workflow = @{
        DotNetVersion = '9.0.x'
        WindowsSdkVersion = '10.0.26100.0'
        WingetPackageIdentifier = 'Contoso.ExampleExtension'
        TestProjects = @('Example.Tests.csproj')
    }
}
"@ | Set-Content -LiteralPath $configPath
    return $configPath
}
