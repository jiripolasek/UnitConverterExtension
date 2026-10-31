# GitHub Actions reference

[Development guide](README.md)

Workflow configuration is read from
[`eng/Package.config.psd1`](../../eng/Package.config.psd1) by
[`Get-WorkflowConfig.ps1`](../../eng/Get-WorkflowConfig.ps1).
The packaging project follows the [WAP contract](WapPackaging.md).

## Workflows

| Workflow | Trigger | Output |
|:--|:--|:--|
| [`ci.yml`](../../.github/workflows/ci.yml) | Branch push, pull request, manual dispatch, or reusable workflow call | Unsigned Store upload packages, validation results, and x64 test reports |
| [`cleanup-ci-artifacts.yml`](../../.github/workflows/cleanup-ci-artifacts.yml) | CI or Release workflow completion | Keeps CI packages from the latest three builds across the repository |
| [`tag-release.yml`](../../.github/workflows/tag-release.yml) | Manual dispatch on a branch | Creates a tag from the package manifest version and starts Release; skips existing tags |
| [`release.yml`](../../.github/workflows/release.yml) | `v*` tag push or manual dispatch on an existing release tag | Unsigned Store upload packages, optional Store draft upload or submission, and GitHub draft release |
| [`winget.yml`](../../.github/workflows/winget.yml) | Release `released` event or manual dispatch with `release_tag` | WinGet manifest PR and its URL in the workflow summary |

## Configuration

| Setting | Purpose |
|:--|:--|
| `RepositoryRoot` | Base directory for configured paths, relative to the configuration file |
| `AppProjectPath` | SDK-style application project |
| `WapProjectPath` | Windows Application Packaging project |
| `PackageManifestPath` | Source of package identity, publisher, and version for tagging |
| `ArtifactsPath` | Package and test output directory |
| `DefaultConfiguration` | Build configuration; currently `Release` |
| `RuntimeIdentifiers` | Build matrix; supported mappings are `x64 = 'win-x64'` and `ARM64 = 'win-arm64'` |
| `Workflow.DotNetVersion` | .NET SDK version supplied to `setup-dotnet` |
| `Workflow.WindowsSdkVersion` | Packaging SDK supplied as `CmdPalTargetPlatformVersion` |
| `Workflow.WingetPackageIdentifier` | Package identifier supplied to the WinGet action |
| `Workflow.TestProjects` | Test project paths; an empty array disables extension tests |

Project, manifest, test, and artifact paths must remain inside the repository.
Application, WAP, manifest, and test project files must exist. Configurations
with test projects must include x64 because the CI test host is x64.

The Windows jobs use `windows-2025-vs2026`; packaging selects MSBuild
18.x. The current configuration selects .NET `10.0.x`, packaging SDK
`10.0.26100.0`, x64, and ARM64. The packaging SDK does not change the manifest's
minimum Windows version.

## CI execution

The `settings` job validates configuration, WAP wiring, and publishing scripts.
Its `config` output contains the architecture matrix and normalized paths.

The dependent `build-and-test` job builds and packages each configured
architecture with the same unsigned Store packaging settings as Release and
`SkipKillRunningExecutable=true`. Packages use Release with Native AOT and
trimming. Unit tests run as managed code on x64 and stop at the first
failing project. TRX reports are uploaded with seven-day retention. ARM64 is
built but its tests are not executed on the x64 runner.

After successful builds and tests, each architecture uploads its `.msixupload`
as `ci-store-packages-<commit SHA>-<platform>` with seven-day retention.
Download these packages from the CI run's Artifacts section for manual Store
submission. CI keeps the checked-in manifest version and requires no signing
or Partner Center credentials. It does not submit packages to the Store.
CI artifact names are separate from those used by Release submission.

After each CI or Release run completes, `cleanup-ci-artifacts.yml` keeps the
CI packages from the three newest workflow runs across all branches and pull
requests, up to six packages for the current architecture matrix. Older CI
packages are deleted. Release packages and test reports are excluded. The
seven-day expiry still applies to retained packages. Cleanup is serialized
and runs without checking out code or downloading artifacts from the build.
The cleanup workflow must exist on the repository's default branch to run.

## Create a release tag

Open **Actions > Tag release > Run workflow**, select the branch to release,
choose the Store options, and run it. It exposes the same `upload_to_store`
and `submit_to_store` checkboxes as Release, both unchecked by default.
The workflow must first exist on the default branch for the manual
trigger to appear. It reads `Identity.Version` from the manifest configured by
`PackageManifestPath`, currently
[`src/UnitConverterExtension.Package/Package.appxmanifest`](../../src/UnitConverterExtension.Package/Package.appxmanifest).
A version such as `0.6.0.0` becomes `v0.6.0`. The manifest must use
`MAJOR.MINOR.PATCH.0`, with each component at most 65535.

The tag points to the selected branch's commit captured when the workflow was
started. If the exact tag already exists, the workflow reports that and stops
without changing it or starting Release. Commit a version bump before creating
the next release tag. A tag creation failure also stops the workflow.

After creating a tag, the workflow explicitly dispatches `release.yml` on that
tag with the selected Store options. It builds packages and prepares a draft
GitHub release from the matching `CHANGELOG.md` section. Enable only
`upload_to_store` to stage packages in a Store draft while keeping certification
and publication manual. No extra token is needed: GitHub's built-in
`GITHUB_TOKEN` can
[dispatch workflows, but its tag pushes do not trigger another workflow](https://docs.github.com/en/actions/how-tos/write-workflows/choose-when-workflows-run/trigger-a-workflow).
If dispatch fails after the tag was created, run **Actions > Release > Run
workflow** on that tag with the same Store options. Rerunning Tag release
leaves the existing tag alone.

## Release execution

1. `version` validates the tag using `Get-ReleaseVersion.ps1`.
2. `validate` runs the reusable CI workflow.
3. `package` updates the runner's manifest version and builds each architecture.
4. `store-submit` uploads the completed package set only for a manual dispatch
   on a tag with `upload_to_store` or `submit_to_store` explicitly enabled.
   Only `submit_to_store` commits the submission for certification.
5. `draft-release` creates a draft after packaging succeeds, independently of
   `store-submit`, using the matching `CHANGELOG.md` section read by
   [`Get-ReleaseNotes.ps1`](../../eng/Get-ReleaseNotes.ps1). Missing or empty
   release notes fail the job. An existing release is left unchanged on reruns.

Accepted tag formats are `vMAJOR.MINOR.PATCH` and `vMAJOR.MINOR.PATCH.0`.
Both produce `MAJOR.MINOR.PATCH.0`; each component must be at most 65535.
Branch refs and prerelease suffixes are rejected. Version changes apply only
to the runner's checkout.

Both Store options are disabled by default. Tag pushes and manual runs with
both checkboxes left unchecked build packages and prepare the GitHub draft
without contacting Partner Center. Open Actions > Release > Run workflow
and select an existing release tag to choose either action:

- Enable `upload_to_store` to upload packages to an existing editable draft
  or create one. Leave `submit_to_store` unchecked to stop before committing.
- Enable `submit_to_store` to upload and commit the packages for certification
  and publication under the existing Store settings. This explicit option
  takes precedence if both checkboxes are checked.

Tags use the workflow stored in their target commit. This opt-in applies to
commits containing this change; historical commits retain their original
submission behavior.

CI and Release set `PublishAot=true` and `PublishTrimmed=true` during both
redirected application restore and WAP packaging. The repository configuration
selects Release. Packaging uses self-contained native output with
`UapAppxPackageBuildMode=StoreUpload`, `AppxBundle=Never`, and
`AppxPackageSigningEnabled=false`.

Managed unit tests do not exercise the native executable. Before release,
repeat the [VM smoke tests](HyperVDeployment.md) with Release+AOT packages.
Earlier managed or Debug VM results do not validate the native release payload.

Each architecture produces an unsigned `.msixupload` Actions artifact with
30-day retention. These artifacts are not attached as release installers.
The subsequent WinGet workflow requires signed `.msix` assets on a published
stable GitHub release; the release workflow does not attach those assets.

## Store submission contract

`Submit-StorePackages.ps1` requires one upload per configured architecture.
Before any Store request, it checks the embedded MSIX identity, publisher,
version, and architecture against the configuration and release version.
Duplicate, missing, or unexpected architectures are rejected.

Store product IDs are normalized to uppercase. API requests include both the
bearer token and the `TenantId` header, matching
[Microsoft's Store CLI](https://github.com/microsoft/msstore-cli/blob/main/MSStore.API/Packaged/StorePackagedAPI.cs).

The Store application must have a published submission, and its identity and
publisher must match the configuration. An existing submission is reused only
in `PendingCommit` state. Submissions already in processing or certification
are left untouched. Without an existing draft, a new submission inherits the
last published listing and settings.

The script replaces packages while retaining the draft's other fields. Stale
pending package entries are replaced directly. Drafts with pending listing
images or other non-package uploads are rejected to avoid replacing a ZIP
that still contains those assets. The updated submission supplies a refreshed
upload URL. Failures retain the pending submission for inspection or retry.

The script defaults to upload-only; `-Submit` is required to call `/commit`.
Upload-only success means the ZIP has been staged and the submission remains
awaiting commit. Store ingestion, certification, and publication have not
started. With `-Submit`, success means ingestion accepted the commit;
certification, signing, and publication continue under the existing settings.

Microsoft warns that editing an API-managed submission in Partner Center can
prevent later API updates or commits, and may leave the submission unusable.
See [Microsoft's API editing restrictions](https://learn.microsoft.com/en-us/windows/uwp/monetize/manage-app-submissions).
The upload-only path does not verify that packages have been ingested or are
ready for final review in the Partner Center UI.

## WinGet validation contract

`Get-WinGetRelease.ps1` rejects drafts and prereleases. It requires one `.msix`
asset per configured architecture and checks identity, publisher, version,
architecture coverage, and signature presence. Asset matching is case
insensitive; architecture is read from the package rather than its filename.
Signature presence does not verify certificate trust or Store provenance.

The validated identifier, version, tag, and release URL are passed to
`vedantmgoyal9/winget-releaser` at commit
`b7370d4d152efdce803a26cc6ca9eee58cb2ad54`. Its
[composite action](https://github.com/vedantmgoyal9/winget-releaser/blob/b7370d4d152efdce803a26cc6ca9eee58cb2ad54/action.yml)
uses `cargo-bins/cargo-binstall@main` and the latest `komac`, so the wrapper pin
does not lock those transitive dependencies.

## Credentials and concurrency

| Repository setting | Type | Use |
|:--|:--|:--|
| `PARTNER_CENTER_TENANT_ID` | Secret | OAuth tenant GUID |
| `PARTNER_CENTER_CLIENT_ID` | Secret | OAuth application GUID |
| `PARTNER_CENTER_CLIENT_SECRET` | Secret | OAuth client credential |
| `STORE_PRODUCT_ID` | Variable | Store application ID; 12 alphanumeric characters |
| `WINGET_TOKEN` | Secret | WinGet manifest submission credential |

The built-in repository token is read-only except in `draft-release`, which
has `contents: write`, the Tag release workflow, which has `contents: write`
and `actions: write`, and the separate CI artifact cleanup workflow, which
has `actions: write`. WinGet validation receives a read-only token and a
publishing-token availability flag. Only the submission action receives
`WINGET_TOKEN`. Checkouts disable persisted credentials.

CI runs for the same workflow/ref cancel older runs. Tag creation runs are
serialized across branches; Release runs for the same ref are serialized.
Store submissions share a repository-wide concurrency
group; WinGet submissions use a separate serialized group.

## Local validation

Use the [local script checks in the development guide](README.md#script-checks).

The Store and WinGet tests use local package fixtures and mocked network calls.
The retention test uses paginated artifact fixtures and mocked GitHub CLI calls.
The configuration test checks changelog extraction and matching workflow inputs.
The tagging test reads fixture manifests and mocks GitHub CLI calls for new
tags, Store input forwarding, existing tags, invalid versions, and API failures.
These checks exercise validation and failure paths without creating remote tags,
submitting packages, creating PRs, or deleting GitHub artifacts.
