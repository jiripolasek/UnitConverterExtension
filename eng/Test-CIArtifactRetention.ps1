<#
.SYNOPSIS
Checks repository-wide CI package retention using mocked GitHub API calls.
#>

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$workflowPath = Join-Path $PSScriptRoot '../.github/workflows/cleanup-ci-artifacts.yml'
$workflow = Get-Content -LiteralPath $workflowPath -Raw
$match = [regex]::Match($workflow, '(?ms)^        run: \|\r?\n(?<script>.*)\z')
if (-not $match.Success) { throw 'CI artifact cleanup script was not found.' }
$cleanup = [scriptblock]::Create(($match.Groups['script'].Value -replace '(?m)^          ', ''))
$originalRepository = $env:GITHUB_REPOSITORY
$originalExitCode = Get-Variable LASTEXITCODE -ValueOnly -ErrorAction SilentlyContinue

function Reset-TestCase([object[]] $Pages) {
    $script:state = @{ Pages = $Pages; Deleted = [Collections.Generic.List[long]]::new(); ListExitCode = 0; DeleteExitCode = 0 }
}

function gh {
    if ($args -contains 'DELETE') {
        if ($args[-1] -notmatch '^repos/example/Extension/actions/artifacts/([0-9]+)$') { throw 'Unexpected deletion endpoint.' }
        $global:LASTEXITCODE = $state.DeleteExitCode
        if ($LASTEXITCODE -eq 0) { $state.Deleted.Add([long] $Matches[1]) }
        return
    }
    if ($args -notcontains '--paginate' -or $args -notcontains '--slurp' -or
        $args[-1] -ne 'repos/example/Extension/actions/artifacts?per_page=100') {
        throw 'Artifact listing must include every API page.'
    }
    $global:LASTEXITCODE = $state.ListExitCode
    ConvertTo-Json -InputObject $state.Pages -Depth 10 -Compress
}

function Assert-Rejected([string] $ExpectedMessage) {
    $failure = $null
    try { & $cleanup | Out-Null }
    catch { $failure = $_.Exception.Message }
    if ($null -eq $failure -or $failure -notlike "*$ExpectedMessage*") {
        throw "Expected '$ExpectedMessage', received '$failure'."
    }
}

try {
    $env:GITHUB_REPOSITORY = 'example/Extension'
    $packages = @(
        foreach ($run in @(8, 9, 10, 11, 12)) {
            foreach ($platform in @('x64', 'ARM64')) {
                @{ id = $run * 10 + [int] ($platform -eq 'ARM64'); name = "ci-store-packages-$run-$platform"; expired = $false; workflow_run = @{ id = $run } }
            }
        }
    )
    $pages = @(
        @{ artifacts = @($packages[0..4]) },
        @{ artifacts = @($packages[5..9]) + @(
            @{ id = 1; name = 'unsigned-store-packages-v1.0.0-x64'; expired = $false; workflow_run = @{ id = 100 } },
            @{ id = 2; name = 'test-results'; expired = $false; workflow_run = @{ id = 101 } },
            @{ id = 3; name = 'ci-store-packages-expired-x64'; expired = $true; workflow_run = @{ id = 102 } }
        ) }
    )
    Reset-TestCase $pages
    & $cleanup
    if (($state.Deleted | Sort-Object) -join ',' -ne '80,81,90,91') { throw 'Cleanup did not preserve both packages from the latest three builds and unrelated artifacts.' }

    Reset-TestCase @(@{ artifacts = @($packages[6..9]) })
    & $cleanup
    if ($state.Deleted.Count -ne 0) { throw 'Cleanup removed packages when fewer than three builds existed.' }

    Reset-TestCase @(@{ artifacts = @() })
    & $cleanup
    if ($state.Deleted.Count -ne 0) { throw 'Cleanup failed on an empty repository.' }

    Reset-TestCase $pages
    $state.ListExitCode = 1
    Assert-Rejected 'Unable to list'
    if ($state.Deleted.Count -ne 0) { throw 'Cleanup deleted artifacts after an API listing failure.' }

    Reset-TestCase $pages
    $state.DeleteExitCode = 1
    Assert-Rejected 'Unable to delete'
    if ($state.Deleted.Count -ne 0) { throw 'Cleanup treated a rejected deletion as successful.' }

    Write-Output 'Passed CI artifact retention checks for paginated builds, architecture pairs, protected artifacts, expired artifacts, small repositories, and API failures.'
}
finally {
    $env:GITHUB_REPOSITORY = $originalRepository
    $global:LASTEXITCODE = if ($null -eq $originalExitCode) { 0 } else { $originalExitCode }
}
