#region TypeDataAttribute.SetParameters() PowerShell Overrides

# These ScriptMethods override the SetParameters methods of some classes derived from
# Stroniax.PowerShell.TypeDataAttribute where setting the parameters must be done in PowerShell
# either due to type casting, requiring PowerShell types, or dynamically generating code.

Update-TypeData -TypeName 'Stroniax.PowerShell.PSScriptMethodAttribute' -MemberType 'ScriptMethod' -MemberName 'SetParameters' -Value {
	param([Hashtable]$parameters, [System.Reflection.ICustomAttributeProvider]$attributeAppliedTo)
	$this.PSBase.SetParameters($parameters, $attributeAppliedTo)
	if ($this.Script) {
		$parameters['Value'] = [ScriptBlock]::Create($this.Script)
	}
} -Force
Update-TypeData -TypeName 'Stroniax.PowerShell.PSScriptPropertyAttribute' -MemberType 'ScriptMethod' -MemberName 'SetParameters' -Value {
	param([Hashtable]$parameters, [System.Reflection.ICustomAttributeProvider]$attributeAppliedTo)
	$this.PSBase.SetParameters($parameters, $attributeAppliedTo)
	if ($this.GetScript) {
		$parameters['Value'] = [ScriptBlock]::Create($this.GetScript)
	}
	if ($this.SetScript) {
		$parameters['SecondValue'] = [ScriptBlock]::Create($this.SetScript)
	}
} -Force
Update-TypeData -TypeName 'Stroniax.PowerShell.PSCodeMethodAttribute' -MemberType 'ScriptMethod' -MemberName 'SetParameters' -Value {
	param([Hashtable]$parameters, [System.Reflection.ICustomAttributeProvider]$attributeAppliedTo)
	$this.PSBase.SetParameters($parameters, $attributeAppliedTo)
	$Method = Get-MethodReference -TypeName $this.ReferencedTypeName -MethodName $this.ReferencedMethodName -MethodUse Method -ErrorAction Ignore
	if ($null -ne $Method) {
		$parameters['Value'] = $Method
	}
} -Force
Update-TypeData -TypeName 'Stroniax.PowerShell.PSCodePropertyAttribute' -MemberType 'ScriptMethod' -MemberName 'SetParameters' -Value {
	param([Hashtable]$parameters, [System.Reflection.ICustomAttributeProvider]$attributeAppliedTo)
	$this.PSBase.SetParameters($parameters, $attributeAppliedTo)
	[System.Reflection.MethodInfo]$GetMethod = Get-MethodReference -TypeName $this.ReferencedGetTypeName -MethodName $this.ReferencedGetMethodName -MethodUse GetProperty -ErrorAction Ignore
	if ($null -ne $GetMethod) {
		$parameters['Value'] = $GetMethod
	}
	[System.Reflection.MethodInfo]$SetMethod = Get-MethodReference -TypeName $this.ReferencedSetTypeName -MethodName $this.ReferencedSetMethodName -MethodUse SetProperty -ErrorAction Ignore
	if ($null -ne $SetMethod) {
		$parameters['SecondValue'] = $SetMethod
	}
} -Force
Update-TypeData -TypeName 'Stroniax.PowerShell.PSCodePropertyFromExtensionMethodAttribute' -MemberType 'ScriptMethod' -MemberName 'SetParameters' -Value {
	param([Hashtable]$parameters, [System.Reflection.ICustomAttributeProvider]$attributeAppliedTo)
	$this.PSBase.SetParameters($parameters, $attributeAppliedTo)

	# Create type and set method reference
	[System.String]$MethodName = $Params['MemberName']
	[System.String]$ReturnType = $attributeAppliedTo.ReturnType.FullName
	if ($ReturnType -eq "System.Void") { $ReturnType = "void" }
	[System.String]$ReferencedMethodPath = $attributeAppliedTo.DeclaringType.FullName + "." + $attributeAppliedTo.Name
	[System.String]$InParameters = ''
	[System.String]$InvokeParameters = ''
	[System.String[]]$InParameterDefinitions = @()
	[System.String[]]$InvokeParameterDefinitions = @()
	[System.Boolean]$FirstParameter = $true
	foreach ($param in $attributeAppliedTo.GetParameters()) {
		if ($FirstParameter) {
			$InParameterDefinitions += "System.Management.Automation.PSObject $($param.Name)"
			$InvokeParameterDefinitions += "$($param.ParameterType.FullName)$($param.Name).BaseObject"
			$FirstParameter = $false
			$Params['TypeName'] = $param.ParameterType.FullName
		}
		else {
			$InParameterDefinitions += "$($param.ParameterType.FullName) $($param.Name)"
			$InvokeParameterDefinitions += $param.Name
		}
	}
	[System.Int16]$i = 0
	do {
		[System.String]$DynamicTypeName = $Params['TypeName'].Replace(".", "_") + "_$($i++)"
		$script:PSExtensionCodeMethodTypes += $DynamicTypeName
	}
	while ($script:PSExtensionCodeMethodTypes -contains $DynamicTypeName)
	$InParameters = $InParameterDefinitions -join ', '
	$InvokeParameters = $InvokeParameterDefinitions -join ', '
	$TypeDefinition = "`n" +
	"namespace PSDynamicCodeExtensionMethods`n" +
	"{`n" +
	"    public static class $DynamicTypeName`n" +
	"    {`n" +
	"        public static $ReturnType $MethodName($InParameters)`n" +
	"        {`n" +
	"            return $ReferencedMethodPath($InvokeParameters);`n" +
	"        }`n" +
	"    }`n" +
	"}`n"
	Add-Type -TypeDefinition $TypeDefinition -ReferencedAssemblies $Definitions[$Key].DeclaringType.Assembly.Location
	$GetMethod = Get-MethodReference -TypeName "PSDynamicCodeExtensionMethods.$DynamicTypeName" -MethodName "$MethodName" -MethodUse GetProperty -ErrorAction Ignore
	if ($null -ne $GetMethod) {
		$Params['Value'] = $GetMethod
	}
} -Force
Update-TypeData -TypeName 'Stroniax.PowerShell.PSCodeMethodFromExtensionMethodAttribute' -MemberType 'ScriptMethod' -MemberName 'SetParameters' -Value {
	param([Hashtable]$parameters, [System.Reflection.ICustomAttributeProvider]$attributeAppliedTo)
	$this.PSBase.SetParameters($parameters, $attributeAppliedTo)

	# Create type and set method reference
	[System.String]$MethodName = $Params['MemberName']
	[System.String]$ReturnType = $Definitions[$Key].ReturnType.FullName
	if ($ReturnType -eq "System.Void") { $ReturnType = "void" }
	[System.String]$ReferencedMethodPath = $Definitions[$Key].DeclaringType.FullName + "." + $Definitions[$Key].Name
	[System.String]$InParameters = ''
	[System.String]$InvokeParameters = ''
	[System.String[]]$InParameterDefinitions = @()
	[System.String[]]$InvokeParameterDefinitions = @()
	[System.Boolean]$FirstParameter = $true
	foreach ($param in $Definitions[$Key].GetParameters()) {
		if ($FirstParameter) {
			$InParameterDefinitions += "System.Management.Automation.PSObject $($param.Name)"
			$InvokeParameterDefinitions += "$($param.ParameterType.FullName)$($param.Name).BaseObject"
			$FirstParameter = $false
			$Params['TypeName'] = $param.ParameterType.FullName
		}
		else {
			$InParameterDefinitions += "$($param.ParameterType.FullName) $($param.Name)"
			$InvokeParameterDefinitions += $param.Name
		}
	}
	[System.Int16]$i = 0
	do {
		[System.Int16]$i++
		[System.String]$DynamicTypeName = $Params['TypeName'].Replace(".", "_") + "_$i"
		$script:PSExtensionCodeMethodTypes += $DynamicTypeName
	}
	while ($script:PSExtensionCodeMethodTypes -contains $DynamicTypeName)
	$InParameters = $InParameterDefinitions -join ', '
	$InvokeParameters = $InvokeParameterDefinitions -join ', '
	$TypeDefinition = "`n" +
	"namespace PSDynamicCodeExtensionMethods`n" +
	"{`n" +
	"    public static class $DynamicTypeName`n" +
	"    {`n" +
	"        public static $ReturnType $MethodName($InParameters)`n" +
	"        {`n" +
	"            return $ReferencedMethodPath($InvokeParameters);`n" +
	"        }`n" +
	"    }`n" +
	"}`n"
	Add-Type -TypeDefinition $TypeDefinition -ReferencedAssemblies $Definitions[$Key].DeclaringType.Assembly.Location
	
	$Method = Get-MethodReference -TypeName "PSDynamicCodeExtensionMethods.$DynamicTypeName" -MethodName "$MethodName" -MethodUse Method -ErrorAction Ignore
	if ($null -ne $Method) {
		$Params['Value'] = $Method
	}
} -Force
#endregion

#region internal classes, functions, & variables

# Type names that have been used for dynamically generated types to construct CodeMethod wrappers around extension methods.
$script:PSExtensionCodeMethodTypes = @()

# A class with two singleton instances to configure the settings for reading TypeData definitions from assemblies or types.
class PSTypeDataDefinitionSettings {
	static PSTypeDataDefinitionSettings() {
		[PSTypeDataDefinitionSettings]::SettingsFilePath = Join-Path $env:APPDATA -ChildPath 'Stroniax\Stroniax.PowerShell.TypeData\Configuration.PS1XML'
		[PSTypeDataDefinitionSettings]::Persistent = [PSTypeDataDefinitionSettings]::Import()
		[PSTypeDataDefinitionSettings]::Current = [PSTypeDataDefinitionSettings]::Import()
	}
	# Should be called before updating the persistent scope in case any changes have been made in another process.
	hidden static [void] ReloadPersistentScope() {
		[PSTypeDataDefinitionSettings]::Persistent = [PSTypeDataDefinitionSettings]::Import()
	}
	hidden static [PSTypeDataDefinitionSettings] Import() {
		$ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
		if (Test-Path ([PSTypeDataDefinitionSettings]::SettingsFilePath)) {
			return [PSTypeDataDefinitionSettings](Import-Clixml ([PSTypeDataDefinitionSettings]::SettingsFilePath))
		}
		else {
			return [PSTypeDataDefinitionSettings]::new()
		}
	}
	# Should be called on the Persistent instance after making any changes to the instance.
	# Do not call this method on the Current instance.
	[void] Save() {
		[System.String]$ParentPath = Split-Path -Path ([PSTypeDataDefinitionSettings]::SettingsFilePath) -Parent
		if (!(Test-Path $ParentPath)) {
			New-Item -Path $ParentPath -ItemType Directory
		}
		$this | Export-Clixml -Path ([PSTypeDataDefinitionSettings]::SettingsFilePath) -Force
	}
	hidden static [PSTypeDataDefinitionSettings]$Persistent;
	hidden static [PSTypeDataDefinitionSettings]$Current;
	hidden static [System.String]$SettingsFilePath;

	# True indicates that assemblies and types should by default NOT be read for TypeData definition attributes.
	[System.Boolean]$BlockUnlessAllowed = $true;
	# True indicates that an assembly that does not contain TypeData definition attributes should be added to a
	# list of assemblies to avoid scanning. This can cause significant performance improvements.
	[System.Boolean]$BlockAfterFirstScan = $true;
	# The assemblies to allow TypeData to be imported from when (BlockUnlessAllowed -eq $true).
	[System.String[]]$AssembliesAllowed = @();
	# The assemblies specifically excluded from being read for TypeData definition attributes. Note that the
	# data in these assemblies will still be read if the assembly is passed directly to Import-TypeData, but
	# will not be read by default when this module is imported or Import-TypeData is invoked with no arguments.
	[System.String[]]$AssembliesBlocked = @();
	# Assemblies that were previously determined to have no TypeData definition attributes and therefore
	# do not need to be scanned next time the assembly is imported. This value is ignored when 
	# ($BlockAfterFirstScan -eq $false).
	[System.String[]]$AssembliesOmitted = @();
}


class PSTypeDataConfigurationItem {
	PSTypeDataConfigurationItem([System.String]$type, [System.String]$assembly, [System.Boolean]$blocked, [System.String]$reason) {
		$This.TypeName = $type
		$This.AssemblyName = $assembly
		$This.Reason = $reason
	}
	[System.String]$TypeName
	[System.String]$AssemblyName
	[System.Boolean]$Blocked
	[System.String]$Reason
}


<#
.SYNOPSIS
	Returns a [System.Reflection.MethodInfo] instance that references a method for a CodeProperty or CodeMethod type extension.
.OUTPUTS
	[System.Reflection.MethodInfo]
.NOTES
	Intended only for internal use by this module.
#>
function Get-MethodReference {
	[CmdletBinding()]
	[OutputType([System.Reflection.MethodInfo])]
	param(
		[Parameter(Mandatory)]
		[AllowEmptyString()] # avoid exception when calling function
		[AllowNull()]
		[System.String]
		$TypeName,

		[Parameter(Mandatory)]
		[System.String]
		[AllowEmptyString()] # avoid exception when calling function
		[AllowNull()]
		$MethodName,

		[Parameter(Mandatory)]
		[ValidateSet('GetProperty', 'SetProperty', 'Method')]
		[System.String]$MethodUse
	)
	process {
		if ([System.String]::IsNullOrWhiteSpace($TypeName) -and [System.String]::IsNullOrWhiteSpace($MethodName)) {
			return $null
		}
		[System.Type] $Type = [System.Type]$TypeName
		[System.Reflection.MethodInfo[]]$Methods = $Type.GetMethods() | Where-Object { $_.Name -eq $MethodName }
		[System.Int16]$ExpectedParameterCount = if ($MethodUse -eq 'GetProperty') { 1 }
		elseif ($MethodUse -eq 'SetProperty') { 2 }
		else { -1 }
		[System.Reflection.MethodInfo]$Method = $Methods `
		| Where-Object { 
			$Parameters = $_.GetParameters();
			return (
				($ExpectedParameterCount -eq -1 -and $Parameters.Count -ge 1) -or 
				($Parameters.Count -eq $ExpectedParameterCount)
			) -and 
			$Parameters[0].ParameterType -eq [System.Management.Automation.PSObject] -and 
			$_.IsStatic
		} `
		| Select-Object -First 1
		return $Method
	}
}

#endregion

#region Functions

<#
.SYNOPSIS
	Configure settings for how TypeData definitions are scanned and imported from assemblies.
.DESCRIPTION
	Configure settings for how TypeData definitions are scanned and imported from assemblies.
.EXAMPLE
	PS C:\> Set-TypeDataDefinitionSettings -BlockByDefault $false -BlockAfterFirstScan $true
	
	In the next PowerShell session this module is imported into, all assemblies loaded into
	the application domain will be scanned unless the have previously been scanned for TypeData
	definitions and found without any.
.INPUTS
	
.OUTPUTS
	
.NOTES
	
#>
function Set-TypeDataDefinitionsSettings {
	[CmdletBinding(SupportsShouldProcess)]
	param(
		# Set whether assemblies and types should by default be blocked, and only be scanned and have data imported
		# if explicitly allowed.
		[Parameter()]
		[System.Boolean]$BlockByDefault,

		# Set whether types and assemblies should be blocked (preventing TypeData definitions from being searched 
		# for and imported on these members) after the type or assembly has been scanned once and found to have
		# no TypeData definitions.
		[Parameter()]
		[System.Boolean]$BlockAfterFirstScan,

		# Determines whether the configured settings should be persisted between sessions.
		[Parameter()]
		[System.Management.Automation.SwitchParameter]$Persist
	)
	process {
		Set-StrictMode -Version 2
		[System.Boolean]$PersistentScopeModified = $false;
		[PSTypeDataDefinitionSettings]::ReloadPersistentScope();
		if ($PSBoundParameters.ContainsKey("BlockByDefault")) {
			[System.String]$action = $null
			if ($BlockByDefault) {
				$action = if ($Persist) { "Block automatic scanning of assemblies and types for type data to import. (This setting will be persistent.)" }
				else { "Block automatic scanning of assemblies and types for type data to import. (This setting will apply only to the current session.)" }
			}
			else {
				$action = if ($Persist) { "Allow automamtic scanning of assemblies and types for type data to import. (This setting will be persistent and may cause undesired code to execute.)" }
				else { "Allow automatic scanning of assemblies and types for type data to import. (This setting will apply only to the current session and may cause undesired code to execute.)" }
			}
			if ($PSCmdlet.ShouldProcess($action, $action, "Block Automatic TypeData Scan/Import")) {
				[PSTypeDataDefinitionSettings]::Current.BlockUnlessAllowed = $BlockByDefault
				if ($Persist -and [PSTypeDataDefinitionSettings]::Persistent.BlockUnlessAllowed -ne $BlockByDefault) {
					[PSTypeDataDefinitionSettings]::Persistent.BlockUnlessAllowed = $BlockByDefault
					$PersistentScopeModified = $true
				}
			}
		}
		if ($PSBoundParameters.ContainsKey("BlockAfterFirstScan")) {
			[System.String]$action = $null
			if ($BlockAfterFirstScan) {
				$action = if ($Persist) { "Avoid scanning assemblies for type data if an assembly has been scanned in the past without type data. (This setting will be persistent.)" }
				else { "Avoid scanning assemblies for type data if an assembly has been scanned in the past without type data. (This setting will apply only to the current session.)" }
			}
			else {
				$action = if ($Persist) { "Scan assemblies for type data even if the assembly was formerly determined not to define any TypeData definitions. (This setting will be persistent and may decrease performance.)" }
				else { "Scan assemblies for type data even if the assembly was formerly determined not to define any TypeData definitions. (This setting will apply only to the current session and may decrease performance.)" }
			}
			if ($PSCmdlet.ShouldProcess($action, $action, "Block After First Scan if No Type Definitions")) {
				[PSTypeDataDefinitionSettings]::Current.BlockAfterFirstScan = $BlockAfterFirstScan
				if ($Persist -and [PSTypeDataDefinitionSettings]::Persistent.BlockAfterFirstScan -ne $BlockAfterFirstScan) {
					[PSTypeDataDefinitionSettings]::Persistent.BlockAfterFirstScan = $BlockAfterFirstScan
					$PersistentScopeModified = $true
				}
			}
		}

		if ($PersistentScopeModified) {
			$ConfirmPreference = [System.Management.Automation.ConfirmImpact]::None
			$WhatIfPreference = [System.Management.Automation.ConfirmImpact]::None
			[PSTypeDataDefinitionSettings]::Persistent.Save();
		}
	}
}


<#
.SYNOPSIS
	Retrieves the names of the types and assemblies are excluded from being scanning for type data.
.DESCRIPTION
	
.EXAMPLE
	PS C:\> <example usage>
	Explanation of what the example does
.INPUTS
	Inputs (if any)
.OUTPUTS
	Output (if any)
.NOTES
	General notes
#>
function Get-TypeDataDefinitionsExclusion {
	[CmdletBinding()]
	[OutputType([PSTypeDataConfigurationItem])]
	param()
	process {
		foreach ($assembly in [PSTypeDataDefinitionSettings]::Current.AssembliesBlocked) {
			[PSTypeDataConfigurationItem]::new(
				$null,
				$assembly,
				$true,
				"Block-TypeDataDefinitions"
			)
		}
		foreach ($type in [PSTypeDataDefinitionSettings]::Current.TypesBlocked) {
			[PSTypeDataConfigurationItem]::new(
				$type,
				$null,
				$true,
				"Block-TypeDataDefinitions"
			)
		}
		foreach ($assembly in [PSTypeDataDefinitionSettings]::Current.AssembliesOmitted) {
			[PSTypeDataConfigurationItem]::new(
				$null,
				$assembly,
				$true,
				"BlockAfterFirstScan"
			)
		}
		foreach ($assembly in [PSTypeDataConfigurationItem]::Current.AssembliesAllowed) {
			[PSTypeDataConfigurationItem]::new(
				$null,
				$assembly,
				$false,
				"Unblock-TypeDataDefinitions"
			)
		}
	}
}


function Clear-TypeDataDefinitionsExclusion {
	[CmdletBinding(SupportsShouldProcess)]
	param(
		[Parameter()]
		[System.Management.Automation.SwitchParameter]
		$Persist
	)
	process {
		if ($Persist -and $PSCmdlet.ShouldProcess("all exclusion and permittance settings")) {
			[PSTypeDataDefinitionSettings]::ReloadPersistentScope()
			[PSTypeDataDefinitionSettings]::Persistent.AssembliesBlocked = @()
			[PSTypeDataDefinitionSettings]::Persistent.AssembliesAllowed = @()
			[PSTypeDataDefinitionSettings]::Persistent.AssembliesOmitted = @()
			[PSTypeDataDefinitionSettings]::Persistent.Save()
		}
		[PSTypeDataDefinitionSettings]::Current.AssembliesBlocked = @()
		[PSTypeDataDefinitionSettings]::Current.AssembliesAllowed = @()
		[PSTypeDataDefinitionSettings]::Current.AssembliesOmitted = @()
	}
}


function Block-TypeDataDefinitions {
	[CmdletBinding(SupportsShouldProcess, DefaultParameterSetName = "AssemblyNameSet")]
	param(
		[Parameter(ParameterSetName = "AssemblyNameSet", Mandatory)]
		[System.String[]]
		[ArgumentCompleter( {
				[PSTypeDataDefinitionSettings]::Current.AssembliesBlocked + [PSTypeDataDefinitionSettings]::Current.AssembliesOmitted | Where-Object { $_ -like "$($args[2])*" }
			})]
		$AssemblyName,

		[Parameter()]
		[System.Management.Automation.SwitchParameter]
		$CurrentSessionOnly
	)
	process {
		if ($PSCmdlet.ShouldProcess("Block type definitions", [System.String]::Join(", ", $AssemblyName))) {
			[PSTypeDataDefinitionSettings]::Current.AssembliesBlocked += $AssemblyName
			if (!$CurrentSessionOnly) {
				[PSTypeDataDefinitionSettings]::ReloadPersistentScope()
				[PSTypeDataDefinitionSettings]::Persistent.AssembliesBlocked += $AssemblyName
				[PSTypeDataDefinitionSettings]::Persistent.Save()
			}
		}
	}
}


function Unblock-TypeDataDefinitions {
	[CmdletBinding(SupportsShouldProcess, DefaultParameterSetName = "AssemblyNameSet")]
	param(
		[Parameter(ParameterSetName = "AssemblyNameSet", Mandatory)]
		[System.String[]]
		[ArgumentCompleter( {
				[PSTypeDataDefinitionSettings]::Current.AssembliesBlocked + [PSTypeDataDefinitionSettings]::Current.AssembliesOmitted | Where-Object { $_ -like "$($args[2])*" }
			})]
		$AssemblyName,

		[Parameter(ParameterSetName = "TypeNameSet", Mandatory)]
		[System.String[]]
		[ArgumentCompleter( {
				[PSTypeDataDefinitionSettings]::Current.TypesBlocked | Where-Object { $_ -like "$($args[2])*" }
			})]
		$TypeName,

		[Parameter()]
		[System.Management.Automation.SwitchParameter]
		$CurrentSessionOnly
	)
	process {
		$target = $AssemblyName
		if ($null -eq $target -or $target.Count -eq 0) { $target = $TypeName }
		if ($PSCmdlet.ShouldProcess("Unblock type definitions", [System.String]::Join(", ", $target))) {
			if ($TypeName) {
				[PSTypeDataDefinitionSettings]::Current.TypesBlocked = [PSTypeDataDefinitionSettings]::Current.TypesBlocked -ne $TypeName
			}
			if ($AssemblyName) {
				[PSTypeDataDefinitionSettings]::Current.AssembliesBlocked = [PSTypeDataDefinitionSettings]::Current.AssembliesBlocked -ne $AssemblyName
			}
			if (!$CurrentSessionOnly) {
				[PSTypeDataDefinitionSettings]::ReloadPersistentScope()
				if ($TypeName) {
					[PSTypeDataDefinitionSettings]::Persistent.TypesBlocked = [PSTypeDataDefinitionSettings]::Persistent.TypesBlocked -ne $TypeName
				}
				if ($AssemblyName) {
					[PSTypeDataDefinitionSettings]::Persistent.AssembliesBlocked = [PSTypeDataDefinitionSettings]::Persistent.AssembliesBlocked -ne $AssemblyName
				}
				[PSTypeDataDefinitionSettings]::Persistent.Save()
			}
		}
	}
}


<#
.SYNOPSIS
	Imports all TypeData defined by attributes applied to a given assembly or type.
.DESCRIPTION
	Imports PowerShell Extended Type System TypeData definitions.
.EXAMPLE
	PS C:\> <example usage>
	Explanation of what the example does
.INPUTS
	Inputs (if any)
.OUTPUTS
	Output (if any)
.NOTES
	General notes
#>
function Import-TypeDataDefinitions {
	[CmdletBinding(DefaultParameterSetName = "ImportAssemblyDefinitions")]
	param(
		[Parameter(ValueFromPipeline, ParameterSetName = "ImportAssemblyDefinitions")]
		[System.Reflection.Assembly]$Assembly,

		[Parameter(Mandatory, ParameterSetName = "ImportTypeDefinitions")]
		[System.Type]$Type
	)
	process {
		[System.Collections.Generic.Dictionary[Stroniax.PowerShell.TypeDataAttribute, System.Reflection.ICustomAttributeProvider]]$Definitions = $null;
		if ($PSBoundParameters.ContainsKey("Type")) {
			Write-Debug "Importing TypeData definitions from type '$type'."
			$Definitions = [Stroniax.PowerShell.TypeDataAttribute]::GetTypeDataDefinitions($Type)
		}
		elseif ($PSBoundParameters.ContainsKey("Assembly")) {
			Write-Debug "Importing TypeData definitions from assembly '$( $Assembly.GetName().Name )'."
			$Definitions = [Stroniax.PowerShell.TypeDataAttribute]::GetTypeDataDefinitions($Assembly)
			Write-Debug ("Identified $( $Definitions.Count ) TypeData definitions in the " +
						"assembly $( $Assembly.GetName().Name ).")
		}
		else {
			Write-Debug "Importing TypeData definitions from all loaded assemblies."
			$Definitions = [System.Collections.Generic.Dictionary[Stroniax.PowerShell.TypeDataAttribute, System.Reflection.ICustomAttributeProvider]]::new();
			foreach ($asm in [System.AppDomain]::CurrentDomain.GetAssemblies()) {
				[System.String]$Name = $asm.GetName().Name
				if ([PSTypeDataDefinitionSettings]::Current.BlockUnlessAllowed -and 
					[PSTypeDataDefinitionSettings]::Current.AllowedAssemblies -notcontains $Name) {
					Write-Debug "Skipping assembly $Name. Reason: BlockUnlessAllowed."
					continue
				}
				elseif ([PSTypeDataDefinitionSettings]::Current.BlockAfterFirstScan -and
					[PSTypeDataDefinitionSettings]::Current.AssembliesOmitted -contains $Name) {
					Write-Debug "Skipping assembly $Name. Reason: BlockAfterFirstScan"
					continue
				}
				elseif ([PSTypeDataDefinitionSettings]::Current.AssembliesBlocked -contains $Name) {
					Write-Debug "Skipping assembly $Name. Reason: Blocked."
					continue
				}
				$d = [Stroniax.PowerShell.TypeDataAttribute]::GetTypeDataDefinitions($asm);
				Write-Debug "Identified $( $d.Count ) TypeData definitions in the assembly $Name."
				if ($d.Count -eq 0) {
					if ([PSTypeDataDefinitionSettings]::Current.BlockAfterFirstScan) {
						Write-Debug ("Adding assembly $Name to assemblies that can be skipped when " +
									"importing TypeData definitions (session).")
						[PSTypeDataDefinitionSettings]::Current.AssembliesOmitted += $Name
					}
					if ([PSTypeDataDefinitionSettings]::Persistent.BlockAfterFirstScan) {
						[PSTypeDataDefinitionSettings]::ReloadPersistentScope()
						Write-Debug ("Adding assembly $Name to assemblies that can be skipped when " +
									"importing TypeData definitions (persistent).")
						[PSTypeDataDefinitionSettings]::Persistent.AssembliesOmitted += $Name
						[PSTypeDataDefinitionSettings]::Persistent.Save()
					}
				}
				foreach ($key in $d.Keys) {
					$Definitions[$key] = $d[$key]
				}
			}
		}
		foreach ($Key in $Definitions.Keys) {
			Write-Debug "Importing TypeData definition $( $Key.GetType().FullName ) applied to $( $Definitions[$Key] )."
			# Add type TypeData
			[System.Collections.Hashtable]$Params = @{
				# TypeName
				# MemberName
				# MemberType
				# Value
				# SecondValue
				# DefaultDisplayPropertySet
				# TypeConverter
				# TypeAdapter
				# Force
			}
			
			# Errors will be handled below. SetParameters() provides abnormal invocation info
			try {
				$Key.SetParameters($Params, $Definitions[$Key])
			} catch {}

			if ($Key -is [Stroniax.PowerShell.PSTypeConverterAttribute]) {
				if ($Key.CanConvertTypeNames) {
					$ThisType = $Definitions[$Key].FullName
					foreach ($Type in $Key.CanConvertTypeNames) {
						Update-TypeData -TypeConverter $ThisType -TypeName $Type
					}
					continue
				}
			}
			elseif ($Key -is [Stroniax.PowerShell.PSTypeAdapterAttribute]) {
				if ($Key.CanAdaptTypeNames) {
					$ThisType = $Definitions[$Key].FullName
					foreach ($Type in $Key.CanAdaptTypeNames) {
						Update-TypeData -TypeAdapter $ThisType -TypeName $Type
					}
					continue
				}
			}
			
			#region Error Reporting
			# Validate parameters before passsing to Update-TypeData. I'm likely to be able to provide a more descriptive
			# error based on the attribute's context. Nevertheless, Update-TypeData may throw an error - I must be
			# prepared to wrap that error.

			# To be as descriptive as possible, I'll report all errors identified for a given type at once
			# so that they can *all* be fixed before the developer tries again.
			$Params.Keys | Where-Object { $null -eq $Params[$_] } | ForEach-Object { $Params.Remove($_) } | Out-Null
			[System.Boolean]$IsErrorState = $false
			$TargetObject = [PSCustomObject]@{'Attribute' = $Key; 'AttributeTarget' = $Definitions[$Key] }
			if ([System.String]::IsNullOrWhiteSpace($Params['TypeName'])) {
				# Update-TypeData requires the TypeName parameter.
				$ex = [Stroniax.PowerShell.TypeDataDefinitionException]::new(
					'No type name was provided for the TypeData definition.', 
					$Params['TypeName'], 
					$Params['MemberType'], 
					$Params['MemberName']
				)
				$er = [System.Management.Automation.ErrorRecord]::new(
					$ex, 
					'TypeNameNotProvided', 
					[System.Management.Automation.ErrorCategory]::MetadataError,
					$TargetObject
				)
				$er.ErrorDetails = [System.Management.Automation.ErrorDetails]::new(
					"The TypeData definition for the $($params['MemberType']) member '$($params['MemberName'])' " +
					"of type '$($params['TypeName'])' cannot be processed. $ex"
				)
				Write-Error -ErrorRecord $er
				$IsErrorState = $true
			}
			if ($null -eq $Params['MemberType']) {
				# Check for atypical cases
				# 1 - TypeConverter
				# 2 - TypeAdapter
				# 3 - DefaultDisplayPropertySet
				# If none of these situations are met, report an error.

				if (-not (
						$Params.ContainsKey('TypeConverter') -or 
						$Params.ContainsKey('TypeAdapter') -or
						$Params.ContainsKey('DefaultDisplayPropertySet'))) {
					$ex = [Stroniax.PowerShell.TypeDataDefinitionException]::new(
						'A TypeData attribute was present but the TypeData definition could not be determined.',
						'Undefined',
						$Params['TypeName'],
						$Params['MemberName']
					)
					$er = [System.Management.Automation.ErrorRecord]::new(
						$ex,
						'TypeDataNotDefined',
						[System.Management.Automation.ErrorCategory]::MetadataError,
						$TargetObject
					)
					$er.ErrorDetails = [System.Management.Automation.ErrorDetails]::new(
						"Could not determine the TypeData definition defined in the $( $Key.GetType().FullName ) attribute on $( $Dictionary[$Key] ). $ex"
					)
					Write-Error -ErrorRecord $er
					$IsErrorState = $true
				}
			}
			else {
				[System.Boolean]$RequireMemberName = $false
				[System.Boolean]$RequireValue = $false
				[System.Boolean]$RequireCodeReference = $false
				[System.Boolean]$RequireGetCodeReference = $false
				[System.Boolean]$RequireSetCodeReference = $false
				switch ($Params['MemberType']) {
					([System.Management.Automation.PSMemberTypes]::AliasProperty) {
						# Required members:
						# 1 - MemberName
						# 2 - Value
						$RequireMemberName = $true
						$RequireValue = $true
					}
					([System.Management.Automation.PSMemberTypes]::CodeMethod) {
						# Required Members:
						# 1 - MemberName
						# 2 - Value (CodeReference)
						# A - IsStatic
						# B - First parameter accepts PSObject

						$RequireMemberName = $true
						$RequireValue = $true
						$RequireCodeReference = $true
					}
					([System.Management.Automation.PSMemberTypes]::CodeProperty) {
						# Required Members:
						# 1 - MemberName
						# 2 - Value (GetCodeReference)
						# A - Validate IsStatic, first parameter accepts PSObject
						# 3 ? SecondValue (SetCodeReference)
						# A - Validate IsStatic
						# B - First parameter accepts PSObject
						# C - Second parameter accepts return type of GetCodeReference

						$RequireMemberName = $true
						$RequireValue = $true
						$RequireGetCodeReference = $true
						$RequireSetCodeReference = $true
					}
					([System.Management.Automation.PSMemberTypes]::ScriptMethod) {
						# Required Members:
						# 1 - MemberName
						# 2 - Value (Script)

						$RequireMemberName = $true
						$RequireValue = $true
					}
					([System.Management.Automation.PSMemberTypes]::ScriptProperty) {
						# Required Members:
						# 1 - MemberName
						# 2 - Value (GetScript)
						# 3 ? SecondValue (SetScript)

						$RequireMemberName = $true
						$RequireValue = $true
					}
					([System.Management.Automation.PSMemberTypes]::NoteProperty) {
						# Required Members
						# 1 - MemberName
						# 2 - Value (must be present: may be null)

						$RequireMemberName = $true
						if (!$Params.ContainsKey('Value')) {
							$Params['Value'] = $null
						}
					}
					default {
						# Report error for unidentified member type
						$ex = [Stroniax.PowerShell.TypeDataDefinitionException]::new(
							'A TypeData attribute was present but the TypeData definition could not be determined.',
							'Undefined',
							$Params['TypeName'],
							$Params['MemberName']
						)
						$er = [System.Management.Automation.ErrorRecord]::new(
							$ex,
							'TypeDataNotDefined',
							[System.Management.Automation.ErrorCategory]::MetadataError,
							$TargetObject
						)
						$er.ErrorDetails = [System.Management.Automation.ErrorDetails]::new(
							"Could not determine the TypeData definition defined in the " +
							"$( $Key.GetType().FullName ) attribute on $( $Dictionary[$Key] )."
							# Omit $ex.Message; additional data provided by message will be irrelevant
							# with definition of attribute and location
						)
						Write-Error -ErrorRecord $er
						$IsErrorState = $true
					}
				}
				if ($RequireMemberName -and [System.String]::IsNullOrWhiteSpace($Params['MemberName'])) {
					$ex = [Stroniax.PowerShell.TypeDataDefinitionException]::new(
						'No member name was provided for the TypeData definition.',
						$Params['TypeName'],
						$Params['MemberType'],
						$Params['MemberName']
					)
					$er = [System.Management.Automation.ErrorRecord]::new(
						$ex,
						'MemberNameNotProvided',
						[System.Management.Automation.ErrorCategory]::MetadataError,
						$TargetObject
					)
					$er.ErrorDetails = [System.Management.Automation.ErrorDetails]::new(
						"The TypeData definition for the $( $params['MemberType'] ) member '$( $params['MemberName'] )' " +
						"of type '$( $params['TypeName'] )' cannot be processed. $ex"
					)
					Write-Error -ErrorRecord $er
					$IsErrorState = $true
				}
				if ($RequireValue -and $null -eq $Params['Value']) {
					if ($RequireCodeReference) {
						$ex = [Stroniax.PowerShell.TypeDataDefinitionException]::new(
							'No code reference could be identified. The referenced method must be static and ' +
							'the first parameter must be of type [System.Management.Automation.PSObject].',
							$Params['TypeName'],
							$Params['MemberType'],
							$Params['MemberName']
						)
						$er = [System.Management.Automation.ErrorRecord]::new(
							$ex,
							'CodeReferenceNotFound',
							[System.Management.Automation.ErrorCategory]::MetadataError,
							$TargetObject
						)
						$er.ErrorDetails = [System.Management.Automation.ErrorDetails]::new(
							"The TypeData definition for the $( $params['MemberType'] ) member " +
							"'$( $params['MemberName'] )' of type '$( $params['TypeName'] )' " +
							"cannot be processed. $ex"
						)
						Write-Error -ErrorRecord $er
						$IsErrorState = $true
					}
					elseif ($RequireGetCodeReference) {
						$ex = [Stroniax.PowerShell.TypeDataDefinitionException]::new(
							'No code reference could be identified. The referenced method must be static and ' +
							'must have a single parameter of type [System.Management.Automation.PSObject].',
							$Params['TypeName'],
							$Params['MemberType'],
							$Params['MemberName']
						)
						$er = [System.Management.Automation.ErrorRecord]::new(
							$ex,
							'GetCodeReferenceNotFound',
							[System.Management.Automation.ErrorCategory]::MetadataError,
							$TargetObject
						)
						$er.ErrorDetails = [System.Management.Automation.ErrorDetails]::new(
							"The TypeData definition for the $( $params['MemberType'] ) member " +
							"'$( $params['MemberName'] )' of type '$( $params['TypeName'] )' " +
							"cannot be processed. $ex"
						)
						Write-Error -ErrorRecord $er
						$IsErrorState = $true
					}
					else {
						$ex = [Stroniax.PowerShell.TypeDataDefinitionException]::new(
							'No value was provided for the TypeData definition.',
							$Params['TypeName'],
							$Params['MemberType'],
							$Params['MemberName']
						)
						$er = [System.Management.Automation.ErrorRecord]::new(
							$ex,
							'ValueNotProvided',
							[System.Management.Automation.ErrorCategory]::MetadataError,
							$TargetObject
						)
						$er.ErrorDetails = [System.Management.Automation.ErrorDetails]::new(
							"The TypeData definition for the $( $params['MemberType'] ) member '$( $params['MemberName'] )' " +
							"of type '$( $params['TypeName'] )' cannot be processed. $ex"
						)
						Write-Error -ErrorRecord $er
						$IsErrorState = $true
					}
				}
				if ($RequireCodeReference -and $null -ne $Params['Value'] -and (
						!$Params['Value'].IsStatic -or 
						$Params['Value'].GetProperties().Count -lt 1 -or
						$Params['Value'].GetProperties()[0].ParameterType -ne [System.Management.Automation.PSObject]
					)) {
					$ex = [Stroniax.PowerShell.TypeDataDefinitionException]::new(
						'No code reference could be identified. The referenced method must be static and ' +
						'the first parameter must be of type [System.Management.Automation.PSObject].',
						$Params['TypeName'],
						$Params['MemberType'],
						$Params['MemberName']
					)
					$er = [System.Management.Automation.ErrorRecord]::new(
						$ex,
						'CodeReferenceInvalid',
						[System.Management.Automation.ErrorCategory]::MetadataError,
						$TargetObject
					)
					$er.ErrorDetails = [System.Management.Automation.ErrorDetails]::new(
						"The TypeData definition for the $( $params['MemberType'] ) member " +
						"'$( $params['MemberName'] )' of type '$( $params['TypeName'] )' " +
						"cannot be processed. $ex"
					)
					Write-Error -ErrorRecord $er
					$IsErrorState = $true
				}
				if ($RequireGetCodeReference -and $null -ne $Params['Value'] -and (
						!$Params['Value'].IsStatic -or 
						$Params['Value'].GetProperties().Count -ne 1 -or
						$Params['Value'].GetProperties()[0].ParameterType -ne [System.Management.Automation.PSObject]
					)) {
					$ex = [Stroniax.PowerShell.TypeDataDefinitionException]::new(
						'No code reference could be identified. The referenced method must be static and ' +
						'must have a single parameter of type [System.Management.Automation.PSObject].',
						$Params['TypeName'],
						$Params['MemberType'],
						$Params['MemberName']
					)
					$er = [System.Management.Automation.ErrorRecord]::new(
						$ex,
						'GetCodeReferenceNotFound',
						[System.Management.Automation.ErrorCategory]::MetadataError,
						$TargetObject
					)
					$er.ErrorDetails = [System.Management.Automation.ErrorDetails]::new(
						"The TypeData definition for the $( $params['MemberType'] ) member " +
						"'$( $params['MemberName'] )' of type '$( $params['TypeName'] )' " +
						"cannot be processed. $ex"
					)
					Write-Error -ErrorRecord $er
					$IsErrorState = $true
				}
				if ($RequireSetCodeReference -and $null -ne $Params['SecondValue'] -and (
						!$Params['SecondValue'].IsStatic -or 
						$Params['SecondValue'].GetProperties().Count -ne 2 -or
						$Params['SecondValue'].GetProperties()[0].ParameterType -ne 
						[System.Management.Automation.PSObject] -or
						$Params['SecondValue'].GetProperties()[1].ParameterType -ne
						$Params['Value'].ReturnType
					)) {
					$ex = [Stroniax.PowerShell.TypeDataDefinitionException]::new(
						'No code reference could be identified. The referenced method must be static, the first' +
						'parameter must be of type [System.Management.Automation.PSObject], and the second' +
						'parameter must be of the return type of the referenced Get method.',
						$Params['TypeName'],
						$Params['MemberType'],
						$Params['MemberName']
					)
					$er = [System.Management.Automation.ErrorRecord]::new(
						$ex,
						'SetCodeReferenceNotFound',
						[System.Management.Automation.ErrorCategory]::MetadataError,
						$TargetObject
					)
					$er.ErrorDetails = [System.Management.Automation.ErrorDetails]::new(
						"The TypeData definition for the $( $params['MemberType'] ) member " +
						"'$( $params['MemberName'] )' of type '$( $params['TypeName'] )' " +
						"cannot be processed. $ex"
					)
					Write-Error -ErrorRecord $er
					$IsErrorState = $true
				}
			}
			if ($IsErrorState) { continue }
			#endregion

			Write-Debug "Invoking Update-TypeData with the following parameters: $( [System.String]::Join(", ", $Params.Keys) )"
			Update-TypeData @Params
		}
	}
}

#endregion