<#
.SYNOPSIS
Checks portable workflow configuration, matrices, paths, release versions, and explicit Store submission.
#>

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Test-PublishingFixture.ps1')
$testRoot = Join-Path ([IO.Path]::GetTempPath()) "Workflow Settings-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $testRoot | Out-Null
$readConfig = Join-Path $PSScriptRoot 'Get-WorkflowConfig.ps1'
$readVersion = Join-Path $PSScriptRoot 'Get-ReleaseVersion.ps1'
$readNotes = Join-Path $PSScriptRoot 'Get-ReleaseNotes.ps1'

function Assert-Rejected([scriptblock] $Action, [string] $ExpectedMessage) {
    $failure = $null
    try { & $Action | Out-Null }
    catch { $failure = $_.Exception.Message }
    if ($null -eq $failure -or $failure -notlike "*$ExpectedMessage*") {
        throw "Expected '$ExpectedMessage', received '$failure'."
    }
}

try {
    $configPath = New-TestWorkflowRepository -Directory $testRoot
    $config = & $readConfig -ConfigPath $configPath
    if ($config.AppProjectPath -ne 'Example Extension.csproj' -or $config.ArtifactsPath -ne 'output packages' -or
        $config.DotNetVersion -ne '9.0.x' -or $config.WingetPackageIdentifier -ne 'Contoso.ExampleExtension' -or
        $config.TestProjects.Count -ne 1 -or $config.Matrix.include.Count -ne 2) {
        throw 'Repository-specific settings were not preserved.'
    }
    $roundTrip = $config | ConvertTo-Json -Depth 10 -Compress | ConvertFrom-Json
    if ($roundTrip.Matrix.include.Count -ne 2 -or $roundTrip.Architectures.Count -ne 2) { throw 'Invalid GitHub matrix JSON.' }

    $configPath = New-TestWorkflowRepository -Directory $testRoot -Architectures @('x64')
    $config = & $readConfig -ConfigPath $configPath
    if ($config.Matrix.include.Count -ne 1 -or $config.Architectures[0] -ne 'x64') { throw 'Single architecture matrix failed.' }
    $singleJson = $config | ConvertTo-Json -Depth 10 -Compress | ConvertFrom-Json
    if ($singleJson.Matrix.include -isnot [array] -or $singleJson.Matrix.include.Count -ne 1) { throw 'Single architecture matrix was not a JSON array.' }

    $text = Get-Content -LiteralPath $configPath -Raw
    $text.Replace("@('Example.Tests.csproj')", '@()') | Set-Content -LiteralPath $configPath
    if ((& $readConfig -ConfigPath $configPath).TestProjects.Count -ne 0) { throw 'Explicitly empty test projects were not supported.' }

    $text.Replace("'Example Extension.csproj'", "'../outside.csproj'") | Set-Content -LiteralPath $configPath
    Assert-Rejected { & $readConfig -ConfigPath $configPath } 'inside the repository'
    $text.Replace("'Example Extension.csproj'", "'missing.csproj'") | Set-Content -LiteralPath $configPath
    Assert-Rejected { & $readConfig -ConfigPath $configPath } 'not found'
    $text.Replace("'win-x64'", "'win-arm64'") | Set-Content -LiteralPath $configPath
    Assert-Rejected { & $readConfig -ConfigPath $configPath } 'Unsupported platform'
    $configPath = New-TestWorkflowRepository -Directory $testRoot -Architectures @('ARM64')
    Assert-Rejected { & $readConfig -ConfigPath $configPath } 'requires the x64 runtime'

    foreach ($tag in @('v0.4.0', 'v0.4.0.0')) {
        if ((& $readVersion -Tag $tag).ToString() -ne '0.4.0.0') { throw 'Release version normalization failed.' }
    }
    foreach ($tag in @('master', 'v0.4', 'v0.4.0.1', 'v0.4.0-beta')) {
        Assert-Rejected { & $readVersion -Tag $tag } 'Select a vMAJOR'
    }
    foreach ($tag in @('v65536.0.0', 'v0.65536.0', 'v0.0.65536')) {
        Assert-Rejected { & $readVersion -Tag $tag } 'between 0 and 65535'
    }

    $changelogPath = Join-Path $testRoot 'CHANGELOG.md'
    $changelog = "# Changelog`n`n## [Unreleased]`n`nFuture notes.`n`n## [1.2.3] - 2026-11-01`n`nCurrent notes.`n`n### Fixed`n`n- A fix.`n`n## [1.2.2] - 2026-10-01`n`nOlder notes.`n"
    foreach ($newLine in @("`n", "`r`n")) {
        Set-Content -LiteralPath $changelogPath -Value $changelog.Replace("`n", $newLine)
        foreach ($tag in @('v1.2.3', 'v1.2.3.0')) {
            $notes = & $readNotes -Tag $tag -ChangelogPath $changelogPath
            if ($notes -cne "Current notes.$newLine$newLine### Fixed$newLine$newLine- A fix.") { throw 'Release notes included the wrong changelog section.' }
        }
        if ((& $readNotes -Tag v1.2.2 -ChangelogPath $changelogPath) -cne 'Older notes.') { throw 'The final changelog section was not read.' }
    }
    Assert-Rejected { & $readNotes -Tag v9.9.9 -ChangelogPath $changelogPath } 'No release notes found'
    Set-Content -LiteralPath $changelogPath -Value "## [1.2.3]`n`n## [1.2.2]`nOlder notes."
    Assert-Rejected { & $readNotes -Tag v1.2.3 -ChangelogPath $changelogPath } 'No release notes found'

    $releaseWorkflow = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../.github/workflows/release.yml') -Raw
    $tagWorkflow = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../.github/workflows/tag-release.yml') -Raw
    foreach ($inputName in @('upload_to_store', 'submit_to_store')) {
        $inputDefinition = [regex]::Match($releaseWorkflow, '(?ms)^      ' + $inputName + ':\r?\n(?<input>(?:        [^\r\n]*\r?\n)+)').Groups['input'].Value
        $tagInputDefinition = [regex]::Match($tagWorkflow, '(?ms)^      ' + $inputName + ':\r?\n(?<input>(?:        [^\r\n]*\r?\n)+)').Groups['input'].Value
        if ($inputDefinition.Replace("`r`n", "`n") -cne $tagInputDefinition.Replace("`r`n", "`n")) {
            throw "Tag release must expose the same '$inputName' input as Release."
        }
        if ($inputDefinition -notmatch '(?m)^        type: boolean\r?$' -or
            $inputDefinition -notmatch '(?m)^        default: false\r?$') {
            throw "Store action '$inputName' must be an explicit boolean input that defaults to false."
        }
    }
    $storeJob = [regex]::Match($releaseWorkflow, '(?ms)^  store-submit:\r?\n(?<job>.*?)(?=^  [\w-]+:|\z)').Groups['job'].Value
    $condition = [regex]::Match($storeJob, '(?m)^    if: (?<condition>[^\r\n]+)').Groups['condition'].Value
    if ([string]::IsNullOrWhiteSpace($condition)) { throw 'Store submission must have a job-level guard.' }
    $expression = $condition.Replace('github.event_name', '$eventName').Replace('github.ref_type', '$refType')
    $expression = $expression.Replace('inputs.submit_to_store', '$submit').Replace('inputs.upload_to_store', '$upload')
    $expression = $expression.Replace('==', '-eq').Replace('&&', '-and').Replace('||', '-or').Replace('true', '$true').Replace('false', '$false')
    $guard = [scriptblock]::Create($expression)
    foreach ($case in @(
        @{ Event = 'push'; Ref = 'tag'; Submit = $null; Upload = $null; Expected = $false },
        @{ Event = 'push'; Ref = 'tag'; Submit = $true; Upload = $false; Expected = $false },
        @{ Event = 'push'; Ref = 'tag'; Submit = $false; Upload = $true; Expected = $false },
        @{ Event = 'release'; Ref = 'tag'; Submit = $true; Upload = $false; Expected = $false },
        @{ Event = 'release'; Ref = 'tag'; Submit = $false; Upload = $true; Expected = $false },
        @{ Event = 'workflow_dispatch'; Ref = 'tag'; Submit = $null; Upload = $null; Expected = $false },
        @{ Event = 'workflow_dispatch'; Ref = 'tag'; Submit = $false; Upload = $false; Expected = $false },
        @{ Event = 'workflow_dispatch'; Ref = 'branch'; Submit = $true; Upload = $false; Expected = $false },
        @{ Event = 'workflow_dispatch'; Ref = 'branch'; Submit = $false; Upload = $true; Expected = $false },
        @{ Event = 'workflow_dispatch'; Ref = 'tag'; Submit = $true; Upload = $false; Expected = $true },
        @{ Event = 'workflow_dispatch'; Ref = 'tag'; Submit = $false; Upload = $true; Expected = $true },
        @{ Event = 'workflow_dispatch'; Ref = 'tag'; Submit = $null; Upload = $true; Expected = $true },
        @{ Event = 'workflow_dispatch'; Ref = 'tag'; Submit = $true; Upload = $true; Expected = $true }
    )) {
        $eventName = $case.Event
        $refType = $case.Ref
        $submit = $case.Submit
        $upload = $case.Upload
        if ([bool] (& $guard) -ne $case.Expected) {
            throw "Unexpected Store permission for event '$eventName', ref '$refType', submit '$submit', and upload '$upload'."
        }
    }
    Write-Output 'Workflow configuration, alternate repository, matrix JSON, release version, changelog notes, matching workflow inputs, and explicit Store submission checks passed.'
}
finally {
    Get-ChildItem -LiteralPath $testRoot -File | Remove-Item -Force
    Remove-Item -LiteralPath $testRoot
}
