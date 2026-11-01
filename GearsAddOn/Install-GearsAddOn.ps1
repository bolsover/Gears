<#
.SYNOPSIS
    Registers (or removes) the Gears add-on in Alibre Design.

.DESCRIPTION
    Alibre Design finds an add-on through a value under HKLM\SOFTWARE\Alibre Design Add-Ons. The value name is the
    add-on identifier from GearsAddOn.adc and the value data is the folder that holds the .adc file.
    Writing to HKLM needs an elevated PowerShell. Close Alibre Design before running this script.

.PARAMETER Source
    Folder that holds the build output (GearsAddOn.dll, GearsAddOn.adc, ...). Defaults to the folder of this script.

.PARAMETER Destination
    Folder the add-on is copied to. Defaults to C:\ProgramData\Alibre AddOns\GearsAddOn.

.PARAMETER InPlace
    Registers the Source folder as it is, without copying. Handy while developing: rebuild, then restart Alibre.

.PARAMETER Uninstall
    Removes the registry value. The copied files are removed as well unless -InPlace is given.

.EXAMPLE
    .\Install-GearsAddOn.ps1
    .\Install-GearsAddOn.ps1 -InPlace
    .\Install-GearsAddOn.ps1 -Uninstall
#>
[CmdletBinding()]
param(
    [string] $Source = $PSScriptRoot,
    [string] $Destination = 'C:\ProgramData\Alibre AddOns\GearsAddOn',
    [switch] $InPlace,
    [switch] $Uninstall
)

$ErrorActionPreference = 'Stop'

$addOnId = '{05A0F2F2-61D4-48B3-A737-BD23152F6FFF}'
$registryPath = 'HKLM:\SOFTWARE\Alibre Design Add-Ons'

$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Run this script from an elevated (administrator) PowerShell: it writes to HKEY_LOCAL_MACHINE.'
}

if (Get-Process -Name 'Alibre Design' -ErrorAction SilentlyContinue) {
    Write-Warning 'Alibre Design is running. Restart it after this script finishes so that it reloads its add-ons.'
}

if ($Uninstall) {
    if (Get-ItemProperty -Path $registryPath -Name $addOnId -ErrorAction SilentlyContinue) {
        Remove-ItemProperty -Path $registryPath -Name $addOnId
        Write-Host "Removed the registry value $addOnId."
    }
    else {
        Write-Host 'The add-on was not registered.'
    }

    if (-not $InPlace -and (Test-Path -LiteralPath $Destination)) {
        Remove-Item -LiteralPath $Destination -Recurse -Force
        Write-Host "Removed $Destination."
    }

    return
}

$adc = Join-Path $Source 'GearsAddOn.adc'
if (-not (Test-Path -LiteralPath $adc)) {
    throw "GearsAddOn.adc was not found in '$Source'. Build the GearsAddOn project first and run the script from its output folder."
}

if (-not (Test-Path -LiteralPath (Join-Path $Source 'GearsAddOn.dll'))) {
    throw "GearsAddOn.dll was not found in '$Source'."
}

if ($InPlace) {
    $folder = (Resolve-Path -LiteralPath $Source).Path
}
else {
    New-Item -ItemType Directory -Path $Destination -Force | Out-Null
    Copy-Item -Path (Join-Path $Source '*') -Destination $Destination -Recurse -Force -Exclude '*.pdb', 'Install-GearsAddOn.ps1'
    $folder = (Resolve-Path -LiteralPath $Destination).Path
}

if (-not (Test-Path $registryPath)) {
    New-Item -Path $registryPath -Force | Out-Null
}

New-ItemProperty -Path $registryPath -Name $addOnId -Value $folder -PropertyType String -Force | Out-Null
Write-Host "Registered the Gears add-on $addOnId"
Write-Host "  folder: $folder"
Write-Host 'Start Alibre Design and look for the Gears menu.'
