@{
    RepositoryRoot = '..'

    AppProjectPath      = 'src\UnitConverterExtension\JPSoftworks.UnitConverterExtension.csproj'
    WapProjectPath      = 'src\UnitConverterExtension.Package\JPSoftworks.UnitConverterExtension.Package.wapproj'
    PackageManifestPath = 'src\UnitConverterExtension.Package\Package.appxmanifest'
    ArtifactsPath       = 'artifacts'

    PackageExecutablePath = 'JPSoftworks.UnitConverterExtension\JPSoftworks.UnitConverterExtension.exe'

    DefaultConfiguration = 'Release'
    DefaultPlatform      = 'x64'

    RuntimeIdentifiers = @{
        x64   = 'win-x64'
        ARM64 = 'win-arm64'
    }

    Workflow = @{
        DotNetVersion = '10.0.x'
        WindowsSdkVersion = '10.0.26100.0'
        WingetPackageIdentifier = 'JiriPolasek.UnitConverterforCommandPalette'
        TestProjects = @(
            'tests\JPSoftworks.UnitConverterExtension.UnitTest\JPSoftworks.UnitConverterExtension.UnitTest.csproj'
            'tests\JPSoftworks.UnitConverterExtension.AppTests\JPSoftworks.UnitConverterExtension.AppTests.csproj'
        )
    }

    Host = @{
        ProcessName = 'Microsoft.CmdPal.UI'
        LaunchUri   = 'x-cmdpal://'
        ReloadUri   = 'x-cmdpal://reload'
    }
}
