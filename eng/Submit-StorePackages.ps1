<#
.SYNOPSIS
Uploads the configured architecture packages through the Microsoft Store API.

.DESCRIPTION
Requires PARTNER_CENTER_TENANT_ID, PARTNER_CENTER_CLIENT_ID,
PARTNER_CENTER_CLIENT_SECRET, and STORE_PRODUCT_ID environment variables.
Reuses an existing editable draft or creates one from the published submission.
Preserves listing and settings. Certification starts only when -Submit is specified.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $PackageDirectory,

    [Parameter(Mandatory)]
    [version] $Version,

    [string] $ConfigPath = (Join-Path $PSScriptRoot 'Package.config.psd1'),

    [switch] $Submit
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

foreach ($name in @('PARTNER_CENTER_TENANT_ID', 'PARTNER_CENTER_CLIENT_ID', 'PARTNER_CENTER_CLIENT_SECRET', 'STORE_PRODUCT_ID')) {
    if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($name))) {
        throw "Required GitHub Actions secret or variable is missing: $name."
    }
}
$tenantId = [guid] $env:PARTNER_CENTER_TENANT_ID
$clientId = [guid] $env:PARTNER_CENTER_CLIENT_ID
$productId = $env:STORE_PRODUCT_ID.ToUpperInvariant()
if ($productId -cnotmatch '^[A-Z0-9]{12}$') {
    throw 'STORE_PRODUCT_ID must be the 12-character Store product ID.'
}
if ($Version.Build -lt 0 -or $Version.Revision -ne 0 -or
    $Version.Major -gt 65535 -or $Version.Minor -gt 65535 -or $Version.Build -gt 65535) {
    throw 'A four-part MSIX version ending in .0 is required, with components between 0 and 65535.'
}

$PackageDirectory = [IO.Path]::GetFullPath($PackageDirectory)
$config = & (Join-Path $PSScriptRoot 'Get-WorkflowConfig.ps1') -ConfigPath $ConfigPath
$packages = @(Get-ChildItem -LiteralPath $PackageDirectory -File -Filter '*.msixupload')
if ($packages.Count -ne $config.Architectures.Count) {
    throw 'Expected one Store upload package per configured architecture.'
}
$architectures = @()
foreach ($package in $packages) {
    $identity = & (Join-Path $PSScriptRoot 'Get-MsixIdentity.ps1') -Path $package.FullName
    if ($identity.Name -ne $config.IdentityName -or $identity.Publisher -ne $config.Publisher -or $identity.Version -ne $Version) {
        throw 'Store upload identity, publisher, or version does not match this extension and release.'
    }
    if ($identity.Architecture -notin $config.Architectures -or $identity.Architecture -in $architectures) {
        throw 'Store uploads must cover each configured architecture exactly once.'
    }
    $architectures += $identity.Architecture
}
$uploadPath = Join-Path $PackageDirectory 'submission.zip'
Compress-Archive -LiteralPath $packages.FullName -DestinationPath $uploadPath -CompressionLevel NoCompression -Force

$tokenResponse = Invoke-RestMethod -Method Post -Uri "https://login.microsoftonline.com/$tenantId/oauth2/token" `
    -ContentType 'application/x-www-form-urlencoded' -TimeoutSec 120 -Body @{
        grant_type = 'client_credentials'
        client_id = $clientId.ToString()
        client_secret = $env:PARTNER_CENTER_CLIENT_SECRET
        resource = 'https://manage.devcenter.microsoft.com'
    }
if ([string]::IsNullOrWhiteSpace($tokenResponse.access_token)) {
    throw 'Partner Center authentication returned no access token.'
}
if ($env:GITHUB_ACTIONS -eq 'true') {
    Write-Host "::add-mask::$($tokenResponse.access_token)"
}
$request = @{
    Headers = @{
        Authorization = "Bearer $($tokenResponse.access_token)"
        TenantId = $tenantId.ToString()
    }
    ContentType = 'application/json; charset=utf-8'
    TimeoutSec = 120
}
$applicationUri = "https://manage.devcenter.microsoft.com/v1.0/my/applications/$productId"
try {
    $application = Invoke-RestMethod -Method Get -Uri $applicationUri @request
}
catch {
    if ($_.ErrorDetails -and -not [string]::IsNullOrWhiteSpace($_.ErrorDetails.Message)) {
        $details = $_.ErrorDetails.Message
        foreach ($secret in @($tokenResponse.access_token, $env:PARTNER_CENTER_CLIENT_SECRET)) {
            $details = $details.Replace($secret, '***')
        }
        Write-Host "Store API error response: $details"
    }
    throw
}
if ($application.packageIdentityName -ne $config.IdentityName -or $application.publisherName -ne $config.Publisher) {
    throw 'STORE_PRODUCT_ID belongs to another extension or publisher. No submission was created.'
}
if ($application.PSObject.Properties.Name -notcontains 'lastPublishedApplicationSubmission' -or $null -eq $application.lastPublishedApplicationSubmission) {
    throw 'The first Store publication must be completed in Partner Center.'
}

if ($application.PSObject.Properties.Name -contains 'pendingApplicationSubmission' -and $null -ne $application.pendingApplicationSubmission) {
    $submission = Invoke-RestMethod -Method Get -Uri "$applicationUri/submissions/$($application.pendingApplicationSubmission.id)" @request
    if ($submission.status -ne 'PendingCommit') {
        throw "A Store submission is already pending in state '$($submission.status)' and cannot be edited."
    }
    Write-Host "Reusing Store submission $($submission.id)."
}
else {
    $submission = Invoke-RestMethod -Method Post -Uri "$applicationUri/submissions" @request
    Write-Host "Created Store submission $($submission.id)."
}
$submissionUri = "$applicationUri/submissions/$($submission.id)"

# Replacing the draft ZIP must not discard pending listing images or other assets.
$nonPackageData = $submission | Select-Object -Property * -ExcludeProperty applicationPackages
if (($nonPackageData | ConvertTo-Json -Depth 100) -match '"fileStatus"\s*:\s*"PendingUpload"') {
    throw 'The Store draft has pending non-package uploads. Complete those uploads before rerunning.'
}

# Pending uploads have no stored package to delete; replace their draft entries directly.
$submission.applicationPackages = @($submission.applicationPackages | Where-Object fileStatus -NE 'PendingUpload')
foreach ($package in $submission.applicationPackages) {
    $package.fileStatus = 'PendingDelete'
}
$submission.applicationPackages = @($submission.applicationPackages) + @(
    foreach ($package in $packages) {
        @{
            fileName = $package.Name
            fileStatus = 'PendingUpload'
            minimumDirectXVersion = 'None'
            minimumSystemRam = 'None'
        }
    }
)
$body = $submission | ConvertTo-Json -Depth 100
$updatedSubmission = Invoke-RestMethod -Method Put -Uri $submissionUri -Body $body @request
if ([string]::IsNullOrWhiteSpace($updatedSubmission.fileUploadUrl)) {
    throw 'The Store draft update returned no package upload URL.'
}
if ($env:GITHUB_ACTIONS -eq 'true') {
    Write-Host "::add-mask::$($updatedSubmission.fileUploadUrl)"
}

try {
    Invoke-WebRequest -Method Put -Uri $updatedSubmission.fileUploadUrl -InFile $uploadPath -ContentType 'application/zip' `
        -Headers @{ 'x-ms-blob-type' = 'BlockBlob'; 'x-ms-version' = '2021-12-02' } -TimeoutSec 600 | Out-Null
}
catch {
    throw 'Store package upload failed. The pending submission was retained; inspect it in Partner Center.'
}
if (-not $Submit) {
    Write-Host "Uploaded packages to Store submission $($submission.id). The draft is awaiting commit; certification and publication have not started."
    return
}
Invoke-RestMethod -Method Post -Uri "$submissionUri/commit" @request | Out-Null

for ($attempt = 0; $attempt -lt 20; $attempt++) {
    $status = Invoke-RestMethod -Method Get -Uri "$submissionUri/status" @request
    Write-Host "Store submission status: $($status.status)"
    if ($status.status -in @('PreProcessing', 'Certification', 'Release', 'PendingPublication', 'Publishing', 'Published')) {
        Write-Host 'Store ingestion accepted the packages. Follow certification and publication in Partner Center.'
        return
    }
    if ($status.status -ne 'CommitStarted') {
        throw "Store submission failed: $($status | ConvertTo-Json -Depth 20 -Compress)"
    }
    Start-Sleep -Seconds 15
}
throw "Timed out waiting for Store submission $($submission.id). Check Partner Center before rerunning."
