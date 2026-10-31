# Standalone WAP reference

[Development guide](README.md)

## Project responsibilities

The Windows Application Packaging Project (WAP) owns MSIX generation and
Visual Studio deployment. The application remains independently buildable
as an SDK-style executable. Both projects support Debug and Release for x64
and ARM64.

| Component | Definition |
|:--|:--|
| Application | [`JPSoftworks.UnitConverterExtension.csproj`](../../src/UnitConverterExtension/JPSoftworks.UnitConverterExtension.csproj) |
| Packaging and deployment | [`JPSoftworks.UnitConverterExtension.Package.wapproj`](../../src/UnitConverterExtension.Package/JPSoftworks.UnitConverterExtension.Package.wapproj) |
| Identity, version, capabilities, activation, and package resources | [`Package.appxmanifest`](../../src/UnitConverterExtension.Package/Package.appxmanifest) |
| Shared MSBuild properties | [`eng/CmdPal.Extension.props`](../../eng/CmdPal.Extension.props) |
| Script configuration | [`eng/Package.config.psd1`](../../eng/Package.config.psd1) |
| Reusable packaging definitions | [`eng/templates/wap`](../../eng/templates/wap) |

The packaged COM server path is
`JPSoftworks.UnitConverterExtension\JPSoftworks.UnitConverterExtension.exe`.
The manifest's app-extension name, provider CLSID, supported interfaces, and
executable path must agree with the application provider and package payload.

## Build requirements

- Visual Studio MSBuild with DesktopBridge/MSIX Packaging Tools.
- A Windows SDK containing `makeappx.exe`.
- Developer Mode on the machine used for local unpacked package registration.
- `Microsoft.Windows.SDK.BuildTools` from the central package configuration.
- Self-contained publish profiles for `win-x64` and `win-arm64`.
- For Native AOT, the C++ desktop workload and MSVC tools for the target architecture.

`WindowsSdkPackageVersion` selects the application's .NET Windows SDK
projection. `CmdPalTargetPlatformVersion` selects the WAP build SDK.
`Microsoft.Windows.SDK.BuildTools` supplies packaging build tasks. These values
are separate from the manifest's minimum Windows version.

## Shared MSBuild contract

| Property | Purpose |
|:--|:--|
| `CmdPalExtensionProject` / `CmdPalExtensionProjectReference` | Absolute application path and WAP-relative project reference |
| `CmdPalExtensionAssemblyName` | Assembly and executable name |
| `CmdPalPackageProject` / `CmdPalPackageProjectGuid` | WAP path and project identity |
| `CmdPalPackageManifest` / `CmdPalPackageExecutable` | Manifest path and package-relative executable path |
| `CmdPalTargetFramework` | Application target framework |
| `CmdPalTargetPlatformVersion` / `CmdPalTargetPlatformMinVersion` | Build SDK and minimum platform version |
| `CmdPalRuntimeIdentifiers` | Supported Windows runtime identifiers |
| `CmdPalDefaultLanguage` | Default package resource language |
| `CmdPalArtifactsPath` | Package and build-log output directory |
| `CmdPalSupportsNativeAot` | Gate for Native AOT publishing |
| `CmdPalWindowsSdkBuildToolsVersion` | Build-tools version when central package management is disabled |
| `CmdPalPackagingTemplateVersion` | Revision of the shared WAP template contract |

[`Directory.Build.props`](../../Directory.Build.props) imports the contract
before project-specific properties are evaluated:

```xml
<Import
  Project="$(MSBuildThisFileDirectory)eng\CmdPal.Extension.props"
  Condition="'$(CmdPalExtensionProject)' == '' and Exists('$(MSBuildThisFileDirectory)eng\CmdPal.Extension.props')" />
```

C#-specific settings are scoped to `.csproj` files. The WAP imports the
DesktopBridge props and targets, references the application as its entry-point
project, and selects `Properties/PublishProfiles/win-$(Platform).pubxml`.
The referenced core project declares `RuntimeIdentifiers` from
`CmdPalRuntimeIdentifiers` so normal restore assets include both runtime targets.

The WAP and `eng/templates/wap/Package.wapproj.template` share packaging
behavior. `CmdPalPackagingTemplateVersion` records that contract revision.

## Publishing modes and restore

| Mode | `PublishAot` | `PublishTrimmed` | Runtime payload |
|:--|:--|:--|:--|
| Default managed publishing | `false` | `false` | Self-contained .NET runtime, including `coreclr.dll` |
| Native AOT | `true` | `true` | Native executable without `coreclr.dll`, `hostfxr.dll`, or the application `.deps.json` |

The application and WAP default to managed publishing. Local deployment scripts
select both Native AOT and trimming with `-Aot`; Hyper-V deployment enables
them by default. CI and Release explicitly enable both for packaging. Direct WAP
builds accept `/p:PublishAot=true /p:PublishTrimmed=true`. Native AOT requires
`CmdPalSupportsNativeAot=true`. `PublishSingleFile=true` is rejected by the WAP.

DesktopBridge publishes from redirected assets under
`obj/wappublish/<runtime>/`. In Visual Studio, `RestoreWapPublishProjects` runs
after `SetProjectReferenceProperties` for package generation and restores the
referenced publish projects. `Deploy-Package.ps1` performs the redirected
restore explicitly before building the WAP.

The WAP generates unsigned, unbundled packages in `CmdPalArtifactsPath`.
Package revision auto-increment is disabled. The application reads its assembly
version from the package manifest before building. Store upload flags and
workflow versioning are documented in [GitHubActions.md](GitHubActions.md).

## Resources and Native AOT dependencies

Manifest assets and package PRI strings belong in
`src/UnitConverterExtension.Package/Assets` and `Strings`. Runtime-loaded assets
belong in `src/UnitConverterExtension/Assets` and are copied to build and publish
output with `PreserveNewest`.

Native AOT and trimming warnings are errors, except `IL2075` from UnitsNet
5.75.1's unused reflective conversion registry. That warning remains visible.
The extension uses typed conversions and its own abbreviation cache. Other
related pinned versions are CsWinRT 2.3.1, Windows SDK projections
10.0.26100.87, and Serilog 4.3.1; package versions are defined in
[`Directory.Packages.props`](../../Directory.Packages.props).

The WAP removes VCLibs framework references from generated manifests. Native
AOT uses the Windows-provided UCRT rather than a packaged VCLibs dependency.

## Script interfaces

| Script | Inputs | Behavior |
|:--|:--|:--|
| [`Initialize-WapProject.ps1`](../../eng/Initialize-WapProject.ps1) | Required `AppProjectPath`; optional output paths, source manifest, `CreateManifest`, identity fields, and `SupportsNativeAot` | Creates the WAP and shared contract, copies or creates a manifest, and copies referenced package assets, PRI strings, and an optional Store association. Supports `-WhatIf`; refuses to overwrite generated outputs. Does not edit or delete the source project or manifest. |
| [`Test-WapProject.ps1`](../../eng/Test-WapProject.ps1) | `RepositoryRoot`, `WapProjectPath`, `ExtensionPropsPath`, `OutputFormat` (`Text` or `Json`) | Static validation of project wiring, identity, activation, assets, profiles, and solution deployment. Does not build or deploy. |
| [`Deploy-Package.ps1`](../../eng/Deploy-Package.ps1) | `ConfigPath`, `Configuration`, `Platform`, `Aot`, `VisualStudioPath`, `BuildOnly` | Restores, builds, unpacks, and registers a development package in the WAP's AppX output directory. Preserves application data when replacing an existing registration and attempts to restore the previous registration after failure. `BuildOnly` returns the unsigned MSIX as a `FileInfo` from an isolated artifact directory without registering it or stopping the local extension. |
| [`Deploy-HyperVPackage.ps1`](../../eng/Deploy-HyperVPackage.ps1) | `ConfigPath`, `VMName`, `Credential`, `Configuration`, `Aot`, `VisualStudioPath`, `TrustCertificate`, `RestoreCheckpoint`, `BootTimeoutSeconds`, `WhatIf` | Builds once per selected architecture, signs a test copy, and installs for the supplied guest users using PowerShell Direct. See [Hyper-V deployment](HyperVDeployment.md). |
| [`Test-Package.ps1`](../../eng/Test-Package.ps1) | Local deployment options (excluding `BuildOnly`) plus `AfterDeploy` (`Reload` or `Restart`) | Deploys the package, then invokes the configured host reload URI or stops and restarts the host. This is a deployment command, not a unit-test runner. |
| [`Uninstall-Package.ps1`](../../eng/Uninstall-Package.ps1) | `ConfigPath`, `PreserveApplicationData`, `WhatIf` | Removes matching registrations for the current user. Application data is removed unless preservation is requested. Generated layout files remain. |

`Configuration` and `Platform` default to values in `Package.config.psd1`.
`Host.ProcessName`, `Host.LaunchUri`, and `Host.ReloadUri` control host refresh.
`AfterDeploy` defaults to `Reload`. Deployment and activation require a system
compatible with the target architecture.

## Validation and failure conditions

```powershell
$config = ./eng/Get-WorkflowConfig.ps1
./eng/Test-WapProject.ps1 -WapProjectPath $config.WapProjectPath -OutputFormat Json
```

Static validation covers package identity, provider activation, executable
paths, manifest assets, PRI resources, publish profiles, and solution deploy
mapping. It does not establish successful installation or host activation.

| Signal | Condition |
|:--|:--|
| `NETSDK1004` under `obj/wappublish` | Redirected NuGet restore assets are missing |
| `NETSDK1047` for a runtime target | Restore assets do not contain the selected framework/runtime target |
| Installed package is not discovered | App-extension metadata, provider CLSID, interfaces, or executable paths may disagree |
| Configured executable is absent after unpacking | Package payload does not match `PackageExecutablePath` |
| Managed deployment has no `coreclr.dll`, or Native AOT deployment contains it | Payload does not match the requested publishing mode |
