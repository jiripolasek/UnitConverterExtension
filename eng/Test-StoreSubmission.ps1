<#
.SYNOPSIS
Checks Store submission automation using local packages and mocked HTTP calls.
#>

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$submitScript = Join-Path $PSScriptRoot 'Submit-StorePackages.ps1'
. (Join-Path $PSScriptRoot 'Test-PublishingFixture.ps1')
$testRoot = Join-Path ([IO.Path]::GetTempPath()) "Store Tests-$([guid]::NewGuid().ToString('N'))"
$names = @('PARTNER_CENTER_TENANT_ID', 'PARTNER_CENTER_CLIENT_ID', 'PARTNER_CENTER_CLIENT_SECRET', 'STORE_PRODUCT_ID')
$originalEnvironment = @{}
foreach ($name in $names) { $originalEnvironment[$name] = [Environment]::GetEnvironmentVariable($name) }
New-Item -ItemType Directory -Path $testRoot | Out-Null
$configPath = New-TestWorkflowRepository -Directory $testRoot
$config = & (Join-Path $PSScriptRoot 'Get-WorkflowConfig.ps1') -ConfigPath $configPath

function Assert-True([bool] $Condition, [string] $Message) {
    if (-not $Condition) { throw $Message }
}

function Reset-TestCase([string] $Mode = 'success') {
    $env:PARTNER_CENTER_TENANT_ID = '00000000-0000-0000-0000-000000000001'
    $env:PARTNER_CENTER_CLIENT_ID = '00000000-0000-0000-0000-000000000002'
    $env:PARTNER_CENTER_CLIENT_SECRET = 'test-secret'
    $env:STORE_PRODUCT_ID = '9N0000000000'
    Get-ChildItem -LiteralPath $testRoot -File | Where-Object Extension -In @('.msixupload', '.zip') | Remove-Item -Force
    foreach ($platform in @('x64', 'ARM64')) {
        New-TestMsixUpload -Path (Join-Path $testRoot "Test_0.4.0.0_$platform.msixupload") -Architecture $platform -IdentityName $config.IdentityName -Publisher $config.Publisher
    }
    $script:state = @{
        Mode = $Mode
        Requests = [Collections.Generic.List[string]]::new()
        Updated = $null
        Uploaded = $false
        Committed = $false
        Polls = 0
        HasPending = $Mode -in @('pending', 'draft')
        ExpectedFiles = @('Test_0.4.0.0_ARM64.msixupload', 'Test_0.4.0.0_x64.msixupload')
    }
    $state.Submission = @'
{
  "id": "new-submission", "status": "PendingCommit", "fileUploadUrl": "https://upload.invalid/packages?sig=stale",
  "pricing": { "priceId": "Tier1012", "marketSpecificPricings": { "CZ": "Tier1013" } },
  "targetPublishMode": "Manual", "listings": { "en-us": { "description": "Keep this listing" } },
  "applicationPackages": [
    { "fileName": "old-x64.msix", "fileStatus": "Uploaded", "version": "0.3.0.0" },
    { "fileName": "old-arm64.msix", "fileStatus": "Uploaded", "version": "0.3.0.0" }
  ]
}
'@ | ConvertFrom-Json
    if ($state.HasPending) { $state.Submission.id = 'existing' }
    if ($Mode -eq 'pending') { $state.Submission.status = 'Certification' }
    $script:testVersion = '0.4.0.0'
}

function Invoke-RestMethod {
    param($Method, $Uri, $ContentType, $TimeoutSec, $Body, $Headers)
    $state.Requests.Add("$Method $Uri")
    if ($Uri -like 'https://login.microsoftonline.com/*/oauth2/token' -and $Method -eq 'Post') {
        return @{ access_token = if ($state.Mode -eq 'no-token') { '' } else { 'test-access-token' } }
    }
    Assert-True ($Headers.Authorization -eq 'Bearer test-access-token') 'Missing Store authorization.'
    Assert-True ($Headers.TenantId -ceq '00000000-0000-0000-0000-000000000001') 'Missing Store tenant header.'
    Assert-True ($ContentType -eq 'application/json; charset=utf-8') 'Store requests require UTF-8 JSON.'
    Assert-True ($Uri -cmatch '/applications/9N0000000000(?:/|$)') 'Store requests must use the canonical uppercase product ID.'
    if ($Uri -eq 'https://manage.devcenter.microsoft.com/v1.0/my/applications/9N0000000000' -and $Method -eq 'Get') {
        if ($state.Mode -eq 'application-error') { throw $state.ApplicationError }
        $application = [pscustomobject]@{
            packageIdentityName = if ($state.Mode -eq 'wrong-product') { 'Wrong.Extension' } else { $config.IdentityName }
            publisherName = if ($state.Mode -eq 'wrong-publisher') { 'CN=WrongPublisher' } else { $config.Publisher }
            pendingApplicationSubmission = if ($state.HasPending) { @{ id = $state.Submission.id } } else { $null }
            lastPublishedApplicationSubmission = if ($state.Mode -eq 'first-publication') { $null } else { @{ id = 'published' } }
        }
        if ($state.Mode -eq 'no-pending-property') { $application.PSObject.Properties.Remove('pendingApplicationSubmission') }
        return $application
    }
    $submissionUri = "https://manage.devcenter.microsoft.com/v1.0/my/applications/9N0000000000/submissions/$($state.Submission.id)"
    if ($Uri -like '*/submissions' -and $Method -eq 'Post') {
        Assert-True (-not $state.HasPending) 'Created another submission while a draft existed.'
        return ($state.Submission | ConvertTo-Json -Depth 100 | ConvertFrom-Json)
    }
    if ($Uri -eq $submissionUri -and $Method -eq 'Get') {
        Assert-True $state.HasPending 'Read a nonexistent draft.'
        return ($state.Submission | ConvertTo-Json -Depth 100 | ConvertFrom-Json)
    }
    if ($Uri -eq $submissionUri -and $Method -eq 'Put') {
        if ($state.Mode -eq 'update-error') { throw 'Store update rejected.' }
        $state.Updated = $Body | ConvertFrom-Json
        $state.Updated.fileUploadUrl = if ($state.Mode -eq 'no-upload-url') { '' } else { 'https://upload.invalid/packages?sig=refreshed' }
        return $state.Updated
    }
    if ($Uri -eq "$submissionUri/commit" -and $Method -eq 'Post') {
        Assert-True $state.Uploaded 'Committed before uploading both packages.'
        if ($state.Mode -eq 'commit-error') { throw 'Store commit rejected.' }
        $state.Committed = $true
        return
    }
    if ($Uri -eq "$submissionUri/status" -and $Method -eq 'Get') {
        $state.Polls++
        $status = if ($state.Mode -eq 'failed') { 'CommitFailed' }
            elseif ($state.Mode -eq 'timeout' -or $state.Polls -eq 1) { 'CommitStarted' }
            else { 'PreProcessing' }
        return @{ status = $status; statusDetails = @{ errors = @() } }
    }
    throw "Unexpected Store request: $Method $Uri."
}

function Invoke-WebRequest {
    param($Method, $Uri, $InFile, $ContentType, $Headers, $TimeoutSec)
    if ($state.Mode -eq 'upload-error') { throw 'Blob upload rejected.' }
    Assert-True ($Method -eq 'Put' -and $Uri -eq 'https://upload.invalid/packages?sig=refreshed') 'The refreshed upload destination was not used.'
    Assert-True ($ContentType -eq 'application/zip' -and $Headers['x-ms-blob-type'] -eq 'BlockBlob') 'Incorrect blob upload headers.'
    $archive = [IO.Compression.ZipFile]::OpenRead($InFile)
    try {
        $entries = @($archive.Entries.FullName | Sort-Object)
        Assert-True (($entries -join ',') -eq ($state.ExpectedFiles -join ',')) 'Upload does not contain the configured packages.'
    }
    finally { $archive.Dispose() }
    $state.Uploaded = $true
}

function Start-Sleep { param($Seconds) }

function Assert-Rejected([string] $Message, [bool] $Submit = $true) {
    $failure = $null
    try { & $submitScript -PackageDirectory $testRoot -Version $script:testVersion -ConfigPath $configPath -Submit:$Submit }
    catch { $failure = $_.Exception.Message }
    Assert-True ($null -ne $failure -and $failure -like "*$Message*") "Expected '$Message', received '$failure'."
}

try {
    Reset-TestCase
    & $submitScript -PackageDirectory $testRoot -Version $script:testVersion -ConfigPath $configPath -Submit
    Assert-True ($state.Committed -and $state.Polls -eq 2) 'Submission did not reach Store ingestion.'
    $updated = $state.Updated
    Assert-True ($updated.pricing.priceId -eq 'Tier1012' -and $updated.pricing.marketSpecificPricings.CZ -eq 'Tier1013') 'Pricing changed.'
    Assert-True ($updated.targetPublishMode -eq 'Manual' -and $updated.listings.'en-us'.description -eq 'Keep this listing') 'Store metadata changed.'
    Assert-True (@($updated.applicationPackages | Where-Object fileStatus -EQ 'PendingDelete').Count -eq 2) 'Inherited packages were not retired.'
    Assert-True (@($updated.applicationPackages | Where-Object fileStatus -EQ 'PendingUpload').Count -eq 2) 'New packages were not registered.'

    Reset-TestCase
    & $submitScript -PackageDirectory $testRoot -Version $script:testVersion -ConfigPath $configPath
    Assert-True ($state.Uploaded -and -not $state.Committed -and $state.Polls -eq 0) 'Upload-only mode started certification.'
    Assert-True (@($state.Requests | Where-Object { $_ -like 'Post */submissions' }).Count -eq 1) 'Upload-only mode did not create a draft.'

    foreach ($responseBody in @($null, ' ', '{"code":"Forbidden","message":"Application access denied.","token":"test-access-token","secret":"test-secret"}')) {
        Reset-TestCase 'application-error'
        $state.ApplicationError = [Management.Automation.ErrorRecord]::new(
            [UnauthorizedAccessException]::new('403 Forbidden'), 'StoreForbidden',
            [Management.Automation.ErrorCategory]::PermissionDenied, $null)
        if ($null -ne $responseBody) { $state.ApplicationError.ErrorDetails = [Management.Automation.ErrorDetails]::new($responseBody) }
        $failure = $null
        $apiLog = @()
        try {
            & $submitScript -PackageDirectory $testRoot -Version $script:testVersion -ConfigPath $configPath -InformationVariable apiLog 6>$null
        }
        catch { $failure = $_ }
        Assert-True ($null -ne $failure -and $failure.Exception -eq $state.ApplicationError.Exception) 'Store lookup diagnostics replaced or swallowed the original error.'
        Assert-True ($state.Requests.Count -eq 2 -and $null -eq $state.Updated -and -not $state.Uploaded -and -not $state.Committed) 'Failed Store lookup continued to submission or upload.'
        $responses = @($apiLog | ForEach-Object { $_.ToString() } | Where-Object { $_.StartsWith('Store API error response: ') })
        if ([string]::IsNullOrWhiteSpace($responseBody)) {
            Assert-True ($responses.Count -eq 0) 'An empty Store error response was logged.'
        }
        else {
            Assert-True ($responses.Count -eq 1 -and $responses[0] -ceq 'Store API error response: {"code":"Forbidden","message":"Application access denied.","token":"***","secret":"***"}') 'Store error details were lost or credentials were exposed.'
        }
    }

    Reset-TestCase
    $env:STORE_PRODUCT_ID = '9n0000000000'
    & $submitScript -PackageDirectory $testRoot -Version $script:testVersion -ConfigPath $configPath
    Assert-True ($state.Uploaded -and -not $state.Committed) 'A lowercase Store product ID was not normalized for upload.'
    Assert-True ($env:STORE_PRODUCT_ID -ceq '9n0000000000') 'Normalizing the product ID changed the caller environment.'

    foreach ($productId in @('short', '9N000000000/', '9N0000000000 ')) {
        Reset-TestCase
        $env:STORE_PRODUCT_ID = $productId
        Assert-Rejected '12-character Store product ID' $false
        Assert-True ($state.Requests.Count -eq 0) 'An invalid product ID reached the Store.'
    }

    Reset-TestCase 'draft'
    $state.Submission.listings.'en-us'.description = 'Keep this draft listing'
    $state.Submission.pricing.priceId = 'DraftTier'
    $state.Submission.applicationPackages += [pscustomobject]@{ fileName = 'stale.msix'; fileStatus = 'PendingUpload'; version = '0.4.0.0' }
    & $submitScript -PackageDirectory $testRoot -Version $script:testVersion -ConfigPath $configPath
    Assert-True ($state.Uploaded -and -not $state.Committed -and $state.Polls -eq 0) 'Reusing a draft submitted it.'
    Assert-True ($state.Updated.id -eq 'existing' -and $state.Updated.listings.'en-us'.description -eq 'Keep this draft listing' -and $state.Updated.pricing.priceId -eq 'DraftTier') 'Reusing a draft lost its identity or metadata.'
    Assert-True (@($state.Updated.applicationPackages | Where-Object fileStatus -EQ 'PendingUpload').Count -eq 2 -and 'stale.msix' -notin $state.Updated.applicationPackages.fileName) 'Stale pending package entries were not replaced.'
    $state.Submission = $state.Updated
    & $submitScript -PackageDirectory $testRoot -Version $script:testVersion -ConfigPath $configPath
    Assert-True (@($state.Updated.applicationPackages | Where-Object fileStatus -EQ 'PendingUpload').Count -eq 2 -and -not $state.Committed) 'Repeated draft uploads duplicated packages or submitted the draft.'
    & $submitScript -PackageDirectory $testRoot -Version $script:testVersion -ConfigPath $configPath -Submit
    Assert-True ($state.Committed -and $state.Polls -eq 2) 'Explicit submission could not finish a previously uploaded draft.'
    Assert-True (-not @($state.Requests | Where-Object { $_ -like 'Post */submissions' }).Count) 'Reusing a draft created another submission.'

    foreach ($status in @('CommitStarted', 'PreProcessing', 'PendingPublication', 'Published', 'CommitFailed', 'None')) {
        Reset-TestCase 'draft'
        $state.Submission.status = $status
        Assert-Rejected 'cannot be edited' $false
        Assert-True ($null -eq $state.Updated -and -not $state.Uploaded -and -not $state.Committed) 'Upload-only mode changed a non-editable submission.'
    }
    Reset-TestCase 'draft'
    $state.Submission.listings.'en-us' | Add-Member -NotePropertyName images -NotePropertyValue @(@{ fileName = 'draft.png'; fileStatus = 'PendingUpload' })
    Assert-Rejected 'pending non-package uploads' $false
    Assert-True ($null -eq $state.Updated -and -not $state.Uploaded -and -not $state.Committed) 'Upload-only mode discarded pending listing assets.'

    Reset-TestCase 'no-upload-url'
    Assert-Rejected 'upload URL' $false
    Assert-True (-not $state.Uploaded -and -not $state.Committed) 'An invalid upload URL was used.'

    foreach ($case in @(@('pending', 'already pending'), @('first-publication', 'first Store publication'), @('no-token', 'no access token'), @('wrong-product', 'belongs to another extension'), @('wrong-publisher', 'belongs to another extension'))) {
        Reset-TestCase $case[0]
        Assert-Rejected $case[1]
        Assert-True (-not $state.Uploaded -and -not $state.Committed) 'Preflight failure changed the Store submission.'
        Assert-True (-not @($state.Requests | Where-Object { $_ -like 'Post */submissions' }).Count) 'Preflight failure created a submission.'
    }
    Reset-TestCase
    Remove-Item -LiteralPath (Join-Path $testRoot 'Test_0.4.0.0_ARM64.msixupload')
    Assert-Rejected 'one Store upload'
    Assert-True ($state.Requests.Count -eq 0) 'Missing ARM64 package reached the Store.'

    Reset-TestCase
    Remove-Item -LiteralPath (Join-Path $testRoot 'Test_0.4.0.0_ARM64.msixupload')
    New-TestMsixUpload -Path (Join-Path $testRoot 'Test_0.4.0.0_ARM64.msixupload') -Architecture ARM64 -IdentityName $config.IdentityName -Publisher $config.Publisher -Version '0.3.0.0'
    Assert-Rejected 'identity, publisher, or version'
    Assert-True ($state.Requests.Count -eq 0) 'Mixed package versions reached the Store.'

    foreach ($version in @('0.4.0', '0.4.0.1', '65536.0.0.0')) {
        Reset-TestCase
        $script:testVersion = $version
        Assert-Rejected 'four-part MSIX version'
        Assert-True ($state.Requests.Count -eq 0) 'Invalid version reached the Store.'
    }
    Reset-TestCase
    $env:PARTNER_CENTER_CLIENT_SECRET = ''
    Assert-Rejected 'PARTNER_CENTER_CLIENT_SECRET'
    Assert-True ($state.Requests.Count -eq 0) 'Missing credentials reached the Store.'

    foreach ($case in @(@('update-error', 'Store update rejected'), @('upload-error', 'package upload failed'))) {
        Reset-TestCase $case[0]
        Assert-Rejected $case[1]
        Assert-True (-not $state.Committed) 'Failed upload or update was committed.'
    }
    Reset-TestCase 'commit-error'
    Assert-Rejected 'Store commit rejected'
    Assert-True (-not $state.Committed -and $state.Polls -eq 0) 'Rejected commit was treated as successful.'
    Reset-TestCase 'failed'
    Assert-Rejected 'CommitFailed'

    Reset-TestCase 'timeout'
    Assert-Rejected 'Timed out'
    Assert-True ($state.Polls -eq 20) 'Commit polling was not bounded.'
    foreach ($case in @(@('x64', $config.IdentityName, $config.Publisher, 'exactly once'), @('ARM64', 'Wrong.Extension', $config.Publisher, 'identity'), @('ARM64', $config.IdentityName, 'CN=WrongPublisher', 'publisher'))) {
        Reset-TestCase
        $path = Join-Path $testRoot 'Test_0.4.0.0_ARM64.msixupload'
        Remove-Item -LiteralPath $path
        New-TestMsixUpload -Path $path -Architecture $case[0] -IdentityName $case[1] -Publisher $case[2]
        Assert-Rejected $case[3]
        Assert-True ($state.Requests.Count -eq 0) 'Invalid package payload reached the Store.'
    }
    Reset-TestCase 'no-pending-property'
    & $submitScript -PackageDirectory $testRoot -Version $script:testVersion -ConfigPath $configPath -Submit
    Assert-True $state.Committed 'An omitted optional API property blocked the update.'

    Reset-TestCase
    $configPath = New-TestWorkflowRepository -Directory $testRoot -Architectures @('x64')
    Remove-Item -LiteralPath (Join-Path $testRoot 'Test_0.4.0.0_ARM64.msixupload')
    $state.ExpectedFiles = @('Test_0.4.0.0_x64.msixupload')
    & $submitScript -PackageDirectory $testRoot -Version $script:testVersion -ConfigPath $configPath -Submit
    Assert-True ($state.Committed -and @($state.Updated.applicationPackages | Where-Object fileStatus -EQ 'PendingUpload').Count -eq 1) 'Single architecture submission failed.'
    Write-Output 'Passed Store upload, draft reuse, and explicit submission checks using mocked HTTP calls and another extension configuration.'
}
finally {
    foreach ($name in $names) { [Environment]::SetEnvironmentVariable($name, $originalEnvironment[$name]) }
    Get-ChildItem -LiteralPath $testRoot -File | Remove-Item -Force
    Remove-Item -LiteralPath $testRoot
}
