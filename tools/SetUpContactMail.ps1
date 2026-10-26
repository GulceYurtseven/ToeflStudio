<#
.SYNOPSIS
Configures Gmail contact delivery in local .NET User Secrets without putting the password in shell history.
.DESCRIPTION
Run from an interactive terminal. This merges only ContactMail settings and sends no email.
User Secrets are outside the repository but are not encrypted; use a managed secret store in production.
#>
[CmdletBinding()]
param(
    [string]$Account = 'gulceyurtseven@gmail.com'
)

$ErrorActionPreference = 'Stop'
$apiProject = Join-Path $PSScriptRoot '../src/ToeflExam.Api'
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'Install the .NET SDK before running this script.' }
if ($Account -match '[\r\n]' -or ([System.Net.Mail.MailAddress]::new($Account)).Address -ne $Account) {
    throw 'Account must be a single email address.'
}

Write-Host 'Create a Google app password with 2-Step Verification enabled: https://myaccount.google.com/apppasswords'
Write-Host 'Enter the app password here, not in chat or an appsettings file. Do not enter your normal Google password.'
$securePassword = Read-Host 'Google app password (hidden)' -AsSecureString
$passwordBuffer = [IntPtr]::Zero
$mailSettings = $null
$plainPassword = $null
try {
    # The password is passed over standard input, never as a visible process argument or temporary file.
    $passwordBuffer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    $plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordBuffer) -replace '\s', ''
    if ($plainPassword -notmatch '^[a-zA-Z0-9]{16}$') { throw 'Enter the 16-character app password provided by Google.' }
    $mailSettings = @{
        'ContactMail:Host' = 'smtp.gmail.com'
        'ContactMail:Port' = '587'
        'ContactMail:UseSsl' = 'false'
        'ContactMail:Username' = $Account
        'ContactMail:From' = $Account
        'ContactMail:Recipient' = 'gulceyurtseven@gmail.com'
        'ContactMail:Password' = $plainPassword
    }
    $mailSettings | ConvertTo-Json -Compress | & dotnet user-secrets set --project $apiProject | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'The Secret Manager command failed. SMTP setup was not confirmed.' }
    Write-Host 'Contact settings saved for local Development. Restart the API, then submit the Contact form to verify delivery.'
    Write-Host 'Messages will be sent to gulceyurtseven@gmail.com. Production needs the equivalent hosting secrets.'
}
finally {
    # Release the unmanaged secret buffer and discard managed references promptly.
    if ($passwordBuffer -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordBuffer) }
    if ($null -ne $mailSettings) { $mailSettings.Clear() }
    $plainPassword = $null
    $securePassword.Dispose()
}
