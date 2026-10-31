# Development guide

Start here to build, test, and contribute to Unit Converter. Run commands from
the repository root in PowerShell 7 on Windows.

## Start here

| Task | Guide |
|:--|:--|
| Set up a fresh VM and test the extension | [Hyper-V quick start](HyperVDeployment.md#quick-start-tldr) |
| Build the package and run automated tests | [Build and test](#build-and-test) |
| Try changes in your local Command Palette | [Local development loop](#local-development-loop) |
| Configure VM targets, certificates, or checkpoints | [Hyper-V deployment reference](HyperVDeployment.md#configure-your-machines-and-certificate) |
| Understand packaging, resources, and Native AOT | [WAP reference](WapPackaging.md) |
| Change category icons or their attribution | [Icon reference](Icons.md) |
| Understand CI, release artifacts, manual Store uploads, and WinGet | [GitHub Actions reference](GitHubActions.md) |
| Report a bug or prepare a contribution | [Contributing](../../CONTRIBUTING.md) |
| Read community expectations or report a conduct issue | [Code of conduct](../../CODE_OF_CONDUCT.md) |

## Requirements

- .NET 10 SDK and PowerShell 7.
- Visual Studio MSBuild with DesktopBridge/MSIX Packaging Tools and a Windows
  SDK containing MakeAppx and SignTool. See the [packaging requirements](WapPackaging.md#build-requirements).
- PowerToys on the machine where you test the extension UI.
- Hyper-V only when using the VM testing workflow. The [quick start](HyperVDeployment.md#quick-start-tldr)
  covers host setup, a guest administrator account, certificate creation,
  saved environment defaults, first deployment, and manual checks.
- C++ desktop tools for the target architecture when publishing with Native AOT.

Package paths and supported architectures come from
[`eng/CmdPal.Extension.props`](../../eng/CmdPal.Extension.props); script defaults
and workflow options come from [`eng/Package.config.psd1`](../../eng/Package.config.psd1).
[`Get-PackageConfig.ps1`](../../eng/Get-PackageConfig.ps1) combines them using
MSBuild property evaluation. Each contributor uses
their own development certificate and VM credentials. Machine-specific settings
can live in environment variables or the ignored `eng/HyperV.config.psd1` file.

## Repository layout

| Location | Responsibility |
|:--|:--|
| [`src/UnitConverter.Core`](../../src/UnitConverter.Core) | Parsing, conversion modules, unit metadata, and localized conversion text |
| [`src/UnitConverterExtension`](../../src/UnitConverterExtension) | Command Palette pages, commands, settings, and runtime assets |
| [`src/UnitConverterExtension.Package`](../../src/UnitConverterExtension.Package) | MSIX manifest, package assets, and localized package resources |
| [`src/UnitConverter.PrintUnits`](../../src/UnitConverter.PrintUnits) | Unit catalog generation |
| [`tests/UnitConverter.Core.Tests`](../../tests/UnitConverter.Core.Tests) | Core behavior, parser, localization, and documentation tests |
| [`tests/UnitConverterExtension.Tests`](../../tests/UnitConverterExtension.Tests) | In-process page, command, and presentation tests |
| [`eng`](../../eng) | Packaging, deployment, and publishing scripts |

## Build and test

Build the unsigned MSIX and run the managed tests from the repository root.

```powershell
$package = .\Build.ps1
$package.FullName
.\Test.ps1
```

Both scripts default to the Windows host architecture (x64 or ARM64) and the
configured build configuration (currently Release). Pass `-Configuration Debug`
to either script for a Debug build.

Use `.\Build.ps1 -Platform ARM64 -Aot` to cross-build an ARM64 Native AOT package
on x64. On Windows on ARM64, run `.\Test.ps1` with the ARM64 .NET SDK to execute
the managed tests natively. Both scripts accept `-Platform x64` or `-Platform ARM64`.
The test script stops on the first failing project.

Add `-BinLog` to either command to save MSBuild binary logs in `artifacts/logs`
for Structured Log Viewer. Test runs with `-BinLog` also save per-project TRX results there.

```powershell
.\Build.ps1 -BinLog
.\Test.ps1 -BinLog
```

The extension tests exercise page and command objects. Check installation, discovery,
clipboard behavior, rendering, and accessibility in the actual Command Palette
when your change affects those paths.

### Script checks

For packaging, deployment, or publishing changes, run the corresponding checks:

```powershell
$config = .\eng\Get-WorkflowConfig.ps1
foreach ($entry in $config.Matrix.include) {
    .\eng\Test-WapProject.ps1 -WapProjectPath $entry.config.WapProjectPath -Configuration $entry.config.DefaultConfiguration -Platform $entry.platform
    if ($LASTEXITCODE -ne 0) { throw "WAP validation failed: $($entry.platform)" }
}
.\eng\Test-WorkflowConfig.ps1
.\eng\Test-WapInitialization.ps1
.\eng\Test-ReleaseTag.ps1
.\eng\Test-WinGetWorkflow.ps1
.\eng\Test-HyperVDeployment.ps1
```

The script tests use local fixtures and mocks for GitHub, package publishing,
and Hyper-V deployment.

## Local development loop

Enable [Developer Mode](https://learn.microsoft.com/en-us/windows/apps/dev-tools/winapp-cli/security#developer-mode)
on the machine where you register an unpacked development package.

To update the current Windows user's extension installation and restart Command
Palette after a Debug build:

```powershell
.\eng\Test-Package.ps1 -Configuration Debug -AfterDeploy Restart
```

For a native deployment on Windows on ARM64, add `-Platform ARM64`.

`Test-Package.ps1` builds, deploys, and refreshes the host; it does not run unit
tests. Local deployment registers an unpacked development package. Use the
[Hyper-V workflow](HyperVDeployment.md#quick-start-tldr) to validate a signed
package installation on a clean system.

## Changing behavior or assets

Follow [`.editorconfig`](../../.editorconfig), preserve the line-ending
conventions in [`.gitattributes`](../../.gitattributes), and keep changes focused.
Add regression coverage when changing conversion or presentation behavior.
Keep public examples in the [main README](../../readme.md) and the
[unit catalog](../../units.md) consistent with the implementation. Update
[third-party notices](../../NOTICE.md) when adding or changing attributed assets.

Read [CONTRIBUTING.md](../../CONTRIBUTING.md) before preparing a contribution.
Report vulnerabilities privately using [SECURITY.md](../../SECURITY.md).
