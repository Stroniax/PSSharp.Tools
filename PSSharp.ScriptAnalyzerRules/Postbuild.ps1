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

# $DebugPreference = 'Continue'
$ErrorActionPreference = 'Stop'
$DebugPreference = 'Continue'
Write-Host "Executing Post-Build: creating module '$ModuleName'."


[hashtable]$Config = @{}
$Config['ModuleName'] = $ModuleName
$Config['RootModule'] = $TargetName
$Config['ModuleDirectory'] = Join-Path $TargetDirectory -ChildPath $Config['ModuleName']

# Identify files not in the output directory that should be copied upon module build
# for each item here, Get-ChildItem is run with the parameters of an entry in this list.
$Config['ImportFiles'] = @(
	@{ 'Path' = $TargetDirectory; 'Recurse' = $true }
	@{ 'Path' = Join-Path $ProjectDirectory -ChildPath 'Functions' ; 'Filter' = '*.psm1' }
	@{ 'Path' = Join-Path $ProjectDirectory -ChildPath 'Types' ; 'Filter' = '*.types.ps1xml' }
	@{ 'Path' = Join-Path $ProjectDirectory -ChildPath 'Formats'; 'Filter' = '*.format.ps1xml' }
)
# Identify where a file should be placed upon module build
$Config['FileMap'] = @(
    @{
		# Priority determines the sort order so that you can make the comparer evaluate one property before another
		# For example, evaluating the FullName of a [FileInfo] object before evaluating the Extension. Lowest value
		# is compared first.
		'Priority' = 0;
		# The property to be evaluated.
		'Property' = 'FullName';
		# The collection of objects to compare, mapping to the output directory of the object.
		# Should be an array of hashtables with 'Value' and 'DestinationDirectory' keys, the value being
		# the value to match to the $FileInfo.$Property value.
		# Default comparison uses the -eq operator: add a ('ComparisonOperator' = 'Like') key/value pair
		# to the hashtable to compare with the 'Like' comparison.
		'Collection' = @(

		)
    }
    @{
		'Priority' = 2;
		'Property' = 'Name';
		'Collection' = @(
			@{ 'Value' = 'System.Management.Automation.dll' ; 'DestinationDirectory' = $null } # This will prevent the SMA assembly from being copied into the module
            @{ 'Value' = $TargetName ; 'DestinationDirectory' = $Config['ModuleDirectory'] ; 'Rename' = $Config['ModuleName'] }
			@{ 'Value' = '*.types.ps1xml' ; 'DestinationDirectory' = $Config['ModuleDirectory']; 'ComparisonOperator' = 'Like' }
			@{ 'Value' = '*.format.ps1xml' ; 'DestinationDirectory' = $Config['ModuleDirectory'] ; 'ComparisonOperator' = 'Like' }
			@{ 'Value' = '*.dll-Help.xml' ; 'DestinationDirectory' = $Config['ModuleDirectory'] ; 'ComparisonOperator' = 'Like' }
        )
    }
    @{
		'Priority' = 10;
		'Property' = 'Extension';
		'Collection' = @(
            @{ 'Value' = '.dll'; 'DestinationDirectory' = $Config['ModuleDirectory'] }
			@{ 'Value' = '.psm1'; 'DestinationDirectory' = $Config['ModuleDirectory'] }
        )
    }
)

if (Test-Path $Config['ModuleDirectory']) {
	Remove-Item $Config['ModuleDirectory'] -Force -Recurse
}
function Copy-ModuleItem {
	[CmdletBinding()]
	param (
		[Parameter(Position = 0, Mandatory, ValueFromPipeline)]
		[Alias("FilePath","Path")]
		[FileSystemInfo]$FileInfo
	)
	process {
		[string]$Destination = $null
		[string]$RenameTo = $null
		$Break = $False
		foreach ($FileMapSource in $Config['FileMap'] | Sort-Object { $_['Priority'] }) {
			foreach ($FileMapItem in $FileMapSource['Collection']) {
				[string]$Operator = $FileMapItem['ComparisonOperator']
				if (!$Operator) { $Operator = 'eq' }
				$ComparisonExpression = '$FileInfo.($FileMapSource[''Property'']) -' + $Operator + ' $FileMapItem[''Value'']'
				if (Invoke-Expression $ComparisonExpression) {
					Write-Debug "Using comparison '-$Operator' matched property $($FileMapSource['Property']) value '$($FileInfo.($FileMapSource['Property']))' to target '$($FileMapItem['Value'])'. The file '$($FileInfo.Name)' will be copied to destination '$($FileMapItem['DestinationDirectory'])'."
					$Destination = $FileMapItem['DestinationDirectory']
					$RenameTo = $FileMapItem['Rename']
					$Break = $true;
				}
				if ($Break) {
					break;
				}
			}
			if ($Break) {
				break;
			}
		}

		if ([string]::IsNullOrWhitespace($Destination)) {
			Write-Debug "File $($FileInfo.Name) has no destination and will not be exported."
			return
		}
		if ([string]::IsNullOrWhitespace($RenameTo)) {
			$DestinationPath = Join-Path $Destination -ChildPath $FileInfo.Name
		}
		else {
			Write-Debug "Renaming target '$($FileInfo.Name)' to '$($RenameTo + $FileInfo.Extension)' while exporting contents."
			$DestinationPath = Join-Path $Destination -ChildPath ($RenameTo + $FileInfo.Extension)
		}
		if ($FileInfo.FullName -eq $DestinationPath) {
			Write-Debug "The destination of file $($FileInfo.Name) is itself. The file will not be replaced."
			return;
		}
		if (!(Test-Path $Destination)) { New-Item -Path $Destination -ItemType 'Directory' | Out-Null }

		if ((Test-Path $DestinationPath) -and !$Append) {
			Write-Error -Message "Multiple contents have been directed to path $DestinationPath. Set the 'Append' key to '$true' in the FileMap item to append the content of files, or change the destination path of one or more of these items."
		}
		elseif (Test-Path $DestinationPath) {
			Write-Error -Exception ([System.NotImplementedException]::new())
		}
		else {
			Copy-Item -Path $FileInfo.FullName -Destination $DestinationPath -PassThru
		}
	}
}

foreach ($targetSource in $Config['ImportFiles']) {
	if ($targetSource.ContainsKey('Path') -and !(Test-Path $targetSource['Path'])) {
		Write-Debug "Import path not found. Path: $($targetSource['Path'])"
		continue
	}
	Write-Debug "Importing contents in path $($targetSource['Path'])"
	Get-ChildItem @TargetSource | ForEach-Object {
		Write-Debug "Importing file $($_.Name)."
		$_ | Copy-ModuleItem
	}
}

$ModuleDirectoryContents = Get-ChildItem -Path $Config['ModuleDirectory'] -Recurse
$ModuleManifestParameters = @{
	Path = Join-Path $Config['ModuleDirectory'] -ChildPath "$($Config['ModuleName']).psd1"
	RootModule = Join-Path $Config['ModuleDirectory'] -ChildPath $Config['RootModule'] | ForEach-Object { $_.Replace($Config['ModuleDirectory'], '').Trim('/\') }
	RequiredAssemblies = $ModuleDirectoryContents | Where-Object { $_.Extension -eq '.dll' } | ForEach-Object { $_.FullName.Replace($Config['ModuleDirectory'], '').Trim('/\') }
	FileList = $ModuleDirectoryContents | ForEach-Object { $_.FullName.Replace($Config['ModuleDirectory'], '').Trim('/\') }
	NestedModules = $ModuleDirectoryContents | Where-Object { $_.Extension -eq '.psm1' -or $_.Extension -eq '.cdxml' } | ForEach-Object  { $_.FullName.Replace($Config['ModuleDirectory'], '').Trim('/\') }
	FormatsToProcess = $ModuleDirectoryContents | Where-Object { $_.Name -like '*.format.ps1xml' } | ForEach-Object { $_.FullName.Replace($Config['ModuleDirectory'], '').Trim('/\') }
	TypesToProcess = $ModuleDirectoryContents | Where-Object { $_.Name -like '*.types.ps1xml' } | ForEach-Object { $_.FullName.Replace($Config['ModuleDirectory'], '').Trim('/\') }
}
$RemoveKeys = @()
foreach ($key in $ModuleManifestParameters.Keys) {
	if ($null -eq $ModuleManifestParameters[$key]) {
		$RemoveKeys += $Key
	}
}
foreach ($key in $RemoveKeys) {
	$ModuleManifestParameters.Remove($key)
}
Write-Host "Creating module manifest:"
$ModuleManifestParameters | Out-String | Write-Host
New-ModuleManifest @ModuleManifestParameters