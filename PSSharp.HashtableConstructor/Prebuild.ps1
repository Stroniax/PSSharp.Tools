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

Write-Host "Executing Pre-Build" -ForegroundColor Green -BackgroundColor Black

Get-ChildItem -Path $TargetDirectory -Recurse | Remove-Item -Recurse -Force