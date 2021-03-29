using namespace System.IO;

[CmdletBinding()]
param (
	[Parameter(Mandatory)]
	[string]$Configuration,
	[Parameter(Mandatory)]
	[string]$ProjectName,
	[Parameter(Mandatory)]
	[string]$TargetName,
	[Parameter(Mandatory)]
	[string]$TargetPath,
	[Parameter(Mandatory)]
	[string]$TargetDirectory,
	[Parameter(Mandatory)]
	[string]$ProjectDirectory,
	[Parameter(Mandatory)]
	[string]$ModuleName
)


if ($Configuration -eq 'Debug') {
	$global:DebugPreference = 'Continue'
	$global:VerbosePreference = 'Continue'
}
Write-Host "Executing debug script." -ForegroundColor Green -BackgroundColor Black
Write-Host "Importing module." -ForegroundColor Green -BackgroundColor Black
Import-Module (Join-Path $TargetDirectory -ChildPath "$ModuleName\$ModuleName.psd1")

New-PSDrive -Name "Project" -PSProvider FileSystem -Root $TargetDirectory -Scope Global | Out-Null
Set-Location "Project:"

