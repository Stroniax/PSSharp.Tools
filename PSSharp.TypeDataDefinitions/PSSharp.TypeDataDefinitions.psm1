#region PSTypeDataAttribute.SetParameters() PowerShell Overrides

# These ScriptMethods override the SetParameters methods of some classes derived from
# PSSharp.PSTypeDataAttribute where setting the parameters must be done in PowerShell
# either due to type casting, requiring PowerShell types, or dynamically generating code.

Update-TypeData -TypeName 'PSSharp.PSScriptMethodAttribute' -MemberType 'ScriptMethod' -MemberName 'SetParameters' -Value {
	param([Hashtable]$parameters, [System.Reflection.ICustomAttributeProvider]$attributeAppliedTo)
	$this.PSBase.SetParameters($parameters, $attributeAppliedTo)
	if ($this.Script) {
		$parameters['Value'] = [ScriptBlock]::Create($this.Script)
	}
} -Force
Update-TypeData -TypeName 'PSSharp.PSScriptPropertyAttribute' -MemberType 'ScriptMethod' -MemberName 'SetParameters' -Value {
	param([Hashtable]$parameters, [System.Reflection.ICustomAttributeProvider]$attributeAppliedTo)
	$this.PSBase.SetParameters($parameters, $attributeAppliedTo)
	if ($this.GetScript) {
		$parameters['Value'] = [ScriptBlock]::Create($this.GetScript)
	}
	if ($this.SetScript) {
		$parameters['SecondValue'] = [ScriptBlock]::Create($this.SetScript)
	}
} -Force
Update-TypeData -TypeName 'PSSharp.PSCodeMethodAttribute' -MemberType 'ScriptMethod' -MemberName 'SetParameters' -Value {
	param([Hashtable]$parameters, [System.Reflection.ICustomAttributeProvider]$attributeAppliedTo)
	$this.PSBase.SetParameters($parameters, $attributeAppliedTo)
	$Method = Get-MethodReference -TypeName $this.ReferencedTypeName -MethodName $this.ReferencedMethodName -MethodUse Method -ErrorAction Ignore
	if ($null -ne $Method) {
		$parameters['Value'] = $Method
	}
} -Force
Update-TypeData -TypeName 'PSSharp.PSCodePropertyAttribute' -MemberType 'ScriptMethod' -MemberName 'SetParameters' -Value {
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
Update-TypeData -TypeName 'PSSharp.PSCodePropertyFromExtensionMethodAttribute' -MemberType 'ScriptMethod' -MemberName 'SetParameters' -Value {
	param([Hashtable]$parameters, [System.Reflection.ICustomAttributeProvider]$attributeAppliedTo)
	$this.PSBase.SetParameters($parameters, $attributeAppliedTo)
	if ($attributeAppliedTo.DeclaringType.IsGenericType -or $attributeAppliedTo.IsGenericMethod) {
		throw [System.InvalidOperationException]::new("CodeMethods and CodeProperties cannot be created from generic extension methods or extension methods defined on generic types.")
	}
	# Create type and set method reference
	[System.String]$MethodName = $Params['MemberName']
	[System.String]$ReturnType = Get-CompilerValidTypeName $attributeAppliedTo.ReturnType
	if ($ReturnType -eq "System.Void") { $ReturnType = "void" }
	[System.String]$ReferencedMethodPath = (Get-CompilerValidTypeName $attributeAppliedTo.DeclaringType) + "." + $attributeAppliedTo.Name
	[System.String]$InParameters = ''
	[System.String]$InvokeParameters = ''
	[System.String[]]$InParameterDefinitions = @()
	[System.String[]]$InvokeParameterDefinitions = @()
	[System.Reflection.ParameterInfo]$FirstParameter = $null
	foreach ($param in $attributeAppliedTo.GetParameters()) {
		if ($null -eq $FirstParameter) {
			$InParameterDefinitions += "System.Management.Automation.PSObject $($param.Name)"
			$InvokeParameterDefinitions += "($( Get-CompilerValidTypeName $param.ParameterType ))$($param.Name).BaseObject"
			$FirstParameter = $param
		}
		else {
			$InParameterDefinitions += "$(Get-CompilerValidTypeName $param.ParameterType) $($param.Name)"
			$InvokeParameterDefinitions += $param.Name
		}
	}
	[System.Int16]$i = 0
	do {
		$i++
		[System.String]$DynamicTypeName = (Get-CompilerValidTypeName $firstParameter.ParameterType).Replace(".", "_").Replace("[]","").Replace("<","").Replace(">","").Replace('`','') + "_$i"
	}
	while ($script:PSExtensionCodeMethodTypes -contains $DynamicTypeName)
	$script:PSExtensionCodeMethodTypes += $DynamicTypeName

	$InParameters = $InParameterDefinitions -join ', '
	$InvokeParameters = $InvokeParameterDefinitions -join ', '
	$TypeDefinition = "`n" +
	"namespace PSSharp.Runtime.Dynamic.PSDynamicCodeExtensionMethods`n" +
	"{`n" +
	"    public static class $DynamicTypeName`n" +
	"    {`n" +
	"        public static $ReturnType $MethodName($InParameters)`n" +
	"        {`n" +
	"            return $ReferencedMethodPath($InvokeParameters);`n" +
	"        }`n" +
	"    }`n" +
	"}`n"
	Add-Type -TypeDefinition $TypeDefinition -ReferencedAssemblies $attributeAppliedTo.DeclaringType.Assembly.Location, ([System.Reflection.Assembly]::LoadWithPartialName('netstandard')) -ErrorAction Stop
	$GetMethod = Get-MethodReference -TypeName "PSSharp.Runtime.Dynamic.PSDynamicCodeExtensionMethods.$DynamicTypeName" -MethodName "$MethodName" -MethodUse GetProperty -ErrorAction Ignore
	if ($null -ne $GetMethod) {
		$Params['Value'] = $GetMethod
	}
} -Force
Update-TypeData -TypeName 'PSSharp.PSCodeMethodFromExtensionMethodAttribute' -MemberType 'ScriptMethod' -MemberName 'SetParameters' -Value {
	param([Hashtable]$parameters, [System.Reflection.ICustomAttributeProvider]$attributeAppliedTo)
	$this.PSBase.SetParameters($parameters, $attributeAppliedTo)
	if ($attributeAppliedTo.DeclaringType.IsGenericType -or $attributeAppliedTo.IsGenericMethod) {
		throw [System.InvalidOperationException]::new("CodeMethods and CodeProperties cannot be created from generic extension methods or extension methods defined on generic types.")
	}
	# Create type and set method reference
	[System.String]$MethodName = $Params['MemberName']
	[System.String]$ReturnType = Get-CompilerValidTypeName $attributeAppliedTo.ReturnType
	if ($ReturnType -eq "System.Void") { $ReturnType = "void" }
	[System.String]$ReferencedMethodPath = (Get-CompilerValidTypeName $attributeAppliedTo.DeclaringType) + "." + $attributeAppliedTo.Name
	[System.String]$InParameters = ''
	[System.String]$InvokeParameters = ''
	[System.String[]]$InParameterDefinitions = @()
	[System.String[]]$InvokeParameterDefinitions = @()
	[System.Reflection.ParameterInfo]$FirstParameter = $null
	foreach ($param in $attributeAppliedTo.GetParameters()) {
		if ($null -eq $FirstParameter) {
			$InParameterDefinitions += "System.Management.Automation.PSObject $($param.Name)"
			$InvokeParameterDefinitions += "($(Get-CompilerValidTypeName $param.ParameterType))$($param.Name).BaseObject"
			$FirstParameter = $param
		}
		else {
			$InParameterDefinitions += "$(Get-CompilerValidTypeName $param.ParameterType) $($param.Name)"
			$InvokeParameterDefinitions += $param.Name
		}
	}
	[System.Int16]$i = 0
	do {
		$i++
		[System.String]$DynamicTypeName = (Get-CompilerValidTypeName $FirstParameter.ParameterType).Replace(".", "_").Replace("[]","").Replace("<","_").Replace(">","_").Replace('`','') + "_$i"
	}
	while ($script:PSExtensionCodeMethodTypes -contains $DynamicTypeName)
	$script:PSExtensionCodeMethodTypes += $DynamicTypeName
	$InParameters = $InParameterDefinitions -join ', '
	$InvokeParameters = $InvokeParameterDefinitions -join ', '
	$TypeDefinition = "`n" +
	"namespace PSSharp.Runtime.Dynamic.PSDynamicCodeExtensionMethods`n" +
	"{`n" +
	"    public static class $DynamicTypeName`n" +
	"    {`n" +
	"        public static $ReturnType $MethodName($InParameters)`n" +
	"        {`n" +
	"            return $ReferencedMethodPath($InvokeParameters);`n" +
	"        }`n" +
	"    }`n" +
	"}`n"
	Add-Type -TypeDefinition $TypeDefinition -ReferencedAssemblies $attributeAppliedTo.DeclaringType.Assembly.Location, ([System.Reflection.Assembly]::LoadWithPartialName('netstandard')) -ErrorAction Stop
	$Method = Get-MethodReference -TypeName "PSSharp.Runtime.Dynamic.PSDynamicCodeExtensionMethods.$DynamicTypeName" -MethodName "$MethodName" -MethodUse Method -ErrorAction Ignore
	if ($null -ne $Method) {
		$Params['Value'] = $Method
	}
} -Force

# Pseudo-types defined via type data definitions assist argument completion.
# Properties must be set before the type name is added to a PSCustomObject.
Update-TypeData -TypeName 'PSSharp.Pseudo.PSTypeDataDefinition' -MemberType NoteProperty -MemberName 'AttributeDefinition' -Value $null -Force
Update-TypeData -TypeName 'PSSharp.Pseudo.PSTypeDataDefinition' -MemberType NoteProperty -MemberName 'AttributeTarget' -Value $null -Force

Update-TypeData -TypeName 'PSSharp.Pseudo.PSTypeDataImportPreferenceItem' -MemberType NoteProperty -MemberName 'IsApplied' -Value $false -Force
Update-TypeData -TypeName 'PSSharp.Pseudo.PSTypeDataImportPreferenceItem' -MemberType NoteProperty -MemberName 'CanImport' -Value $false -Force
Update-TypeData -TypeName 'PSSharp.Pseudo.PSTypeDataImportPreferenceItem' -MemberType NoteProperty -MemberName 'Reason' -Value ([System.String]::Empty) -Force
Update-TypeData -TypeName 'PSSharp.Pseudo.PSTypeDataImportPreferenceItem' -MemberType NoteProperty -MemberName 'AssemblyName' -Value ([System.String]::Empty) -Force
#endregion

#region runtime classes, internal functions & variables

# Type names that have been used for dynamically generated types to construct CodeMethod wrappers around extension methods.
$script:PSExtensionCodeMethodTypes = @()


# Preference setting options to indicate from which assemblies TypeData will be imported.
enum PSTypeDataAutoImportPreference {
	# TypeData will be imported from every assembly that is imported into the application domain.
	All
	# TypeData will be imported from every assembly that is imported into the application domain
	# except in cases where the assembly has been explicitly excluded.
	Blocklist
	# TypeData will not be imported from any assembly unless that assembly has been added
	# to an explicit whitelist.
	Allowlist
	# TypeData will not be automatically imported into the session.
	None
}


# Preference settings indicating from which assemblies TypeData may or may not be imported.
class PSTypeDataAutoImportSettings {
	static PSTypeDataAutoImportSettings() {
		[PSTypeDataAutoImportSettings]::SettingsFilePath = Join-Path $env:APPDATA -ChildPath 'PSSharp\TypeDataDefinitions\Configuration.PS1XML'
		[PSTypeDataAutoImportSettings]::Persistent = [PSTypeDataAutoImportSettings]::Import()
		[PSTypeDataAutoImportSettings]::Current = [PSTypeDataAutoImportSettings]::Import()
	}
	# Should be called before updating the persistent scope in case any changes have been made in another process.
	hidden static [void] ReloadPersistentScope() {
		[PSTypeDataAutoImportSettings]::Persistent = [PSTypeDataAutoImportSettings]::Import()
	}
	hidden static [PSTypeDataAutoImportSettings] Import() {
		$ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
		if (Test-Path ([PSTypeDataAutoImportSettings]::SettingsFilePath)) {
			return [PSTypeDataAutoImportSettings](Import-Clixml ([PSTypeDataAutoImportSettings]::SettingsFilePath))
		}
		else {
			return [PSTypeDataAutoImportSettings]::new()
		}
	}
	# Should be called on the Persistent instance after making any changes to the instance.
	# Do not call this method on the Current instance.
	[void] Save() {
		[System.String]$ParentPath = Split-Path -Path ([PSTypeDataAutoImportSettings]::SettingsFilePath) -Parent
		if (!(Test-Path $ParentPath)) {
			New-Item -Path $ParentPath -ItemType Directory
		}
		$this | Export-Clixml -Path ([PSTypeDataAutoImportSettings]::SettingsFilePath) -Force
	}
	hidden static [PSTypeDataAutoImportSettings]$Persistent;
	hidden static [PSTypeDataAutoImportSettings]$Current;
	hidden static [System.String]$SettingsFilePath;

	# Determines from which assemblies TypeData definitions will be imported.
	[PSTypeDataAutoImportPreference]$AutoImportPreference = [PSTypeDataAutoImportPreference]::Allowlist
	# Determines if an assembly can be skipped if it was previously determined that it does not
	# contain TypeData definitions.
	[System.Boolean]$AlwaysCheckAssemblies = $false
	# The assemblies to allow TypeData to be imported from when (BlockUnlessAllowed -eq $true).
	[System.String[]]$AllowList = @();
	# The assemblies specifically excluded from being read for TypeData definition attributes. Note that the
	# data in these assemblies will still be read if the assembly is passed directly to Import-TypeData, but
	# will not be read by default when this module is imported or Import-TypeData is invoked with no arguments.
	[System.String[]]$BlockList = @();
	# Assemblies that were previously determined to have no TypeData definition attributes and therefore
	# do not need to be scanned next time the assembly is imported. This value is ignored when 
	# ($BlockAfterFirstScan -eq $false).
	[System.String[]]$SkipList = @();
}


# Quick & simple parameter transformation. Why does PowerShell not include this by default?
class TransformationScriptAttribute : System.Management.Automation.ArgumentTransformationAttribute {
	[ScriptBlock]$ScriptBlock
	TransformationScriptAttribute([ScriptBlock]$ScriptBlock) {
		$this.ScriptBlock = $ScriptBlock
	}
	[System.Object] Transform([System.Management.Automation.EngineIntrinsics]$engineIntrinsics, [System.Object]$inputData) 
	{
		$collection = $this.ScriptBlock.InvokeWithContext(
			$null, # FunctionsToDefine
			@(
				[psvariable]::new('_', $inputData), # assign $PSItem to allow quick processing without named args,
													# like ArgumentCompleterAttribute or ValidateScriptAttribute.
				[psvariable]::new('engineIntrinsics', $engineIntrinsics)	
				[psvariable]::new('inputData', $inputData)
			),
			$engineIntrinsics,
			$inputData
		)
		if ($collection.Count -eq 1) {
			return $collection[0]
		}
		$output = [object[]]::new($collection.Count)
		for($i=0;$i-lt$collection.Count;$i++) {
			$output[$i] = $collection[$i]
		}
		return $output
	}
}


<#
.SYNOPSIS
	Returns a [System.Reflection.MethodInfo] instance that references a method for a CodeProperty or CodeMethod type extension.

	$null will be returned if the referenced method cannot be found.
.OUTPUTS
	[System.Reflection.MethodInfo]
.NOTES
	This is a helper function and should not be exposed by the module.
#>
function Get-MethodReference {
	[CmdletBinding()]
	[OutputType([System.Reflection.MethodInfo])]
	param(
		 # The attribute may have been passed $null for TypeName and MethodName. Ensure that this will not
		 # throw an exception that can't be concealed by the function.

		 # The name of the type that defines the referenced method.
		[Parameter(Mandatory)]
		[AllowEmptyString()]
		[AllowNull()]
		[System.String]
		$TypeName,

		# The name of the referenced method.
		[Parameter(Mandatory)]
		[System.String]
		[AllowEmptyString()]
		[AllowNull()]
		$MethodName,

		# The intended use of the method. Used to identify the proper method if multiple overloads are defined.
		[Parameter(Mandatory)]
		[ValidateSet('GetProperty', 'SetProperty', 'Method')]
		[System.String]$MethodUse
	)
	process {
		trap {
			$PSCmdlet.ThrowTerminatingError($_)
			break;
		}
		$ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
		if ([System.String]::IsNullOrWhiteSpace($TypeName) -or
			[System.String]::IsNullOrWhiteSpace($MethodName)) {
			return $null
		}
		[System.Type] $Type = [System.Type]$TypeName
		[System.Reflection.MethodInfo[]]$Methods = $Type.GetMethods() | Where-Object { $_.Name -eq $MethodName }
		[System.Int16]$ExpectedParameterCount = if ($MethodUse -eq 'GetProperty') { 1 }
												elseif ($MethodUse -eq 'SetProperty') { 2 }
												else { -1 }
		[System.Reflection.MethodInfo]$Method = $Methods `
		| Where-Object { 
			$Parameters = $_.GetParameters()
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


<#
.SYNOPSIS
	Determines if an assembly should be automatically imported according to the current PSTypeDataAutoImportSettings.
.EXAMPLE
	PS C:\> Test-TypeDataImportPreference -Assembly ([System.String].Assembly)
	False

	The function is used to determine if an assembly should be automatically imported. In the case of the assembly
	in which System.String is defined, in most cases the assembly will be excluded and therefore the result will
	be false.
.INPUTS
	[System.Reflection.Assembly]
.OUTPUTS
	[System.Boolean]
.NOTES
	This is a helper function and should not be exposed by the module.
#>
function Test-TypeDataImportPreference {
	[CmdletBinding()]
	[OutputType([System.Boolean])]
	param(
		[Parameter(Mandatory, ValueFromPipeline)]
		[System.Reflection.Assembly]
		$Assembly
	)
	process {
		trap {
			$PSCmdlet.ThrowTerminatingError($_)
			break;
		}
		$ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
		Set-StrictMode -Version 2
		[PSTypeDataAutoImportSettings]$Settings = [PSTypeDataAutoImportSettings]::Current
		if ($Assembly.FullName -in $Settings.SkipList -and -not $Settings.AlwaysCheckAssemblies) {
			return $false
		}
		switch ($Settings.AutoImportPreference) {
			([PSTypeDataAutoImportPreference]::All) {
				return $true
			}
			([PSTypeDataAutoImportPreference]::Blocklist) {
				return -not [System.Boolean]($Assembly.FullName -in $Settings.BlockList)
			}
			([PSTypeDataAutoImportPreference]::Allowlist) {
				return [System.Boolean]($Assembly.FullName -in $Settings.AllowList)
			}
			([PSTypeDataAutoImportPreference]::None) {
				return $false
			}
			default {
				return $false
			}
		}
	}
}


<#
.SYNOPSIS
	Returns the name of a type in a manner that may be used within a text block to be compiled.
.DESCRIPTION
	Returns the name of a type (particularly a generic type) as it would be called in C#.
.EXAMPLE
	PS C:\> Get-CompilerValidTypeName ([System.Collections.Generic.List[System.String]])
	System.Collections.Generic.List<System.String>

	The name of the type is returned as it would be used in an uncompiled C# statement.
.INPUTS
	Inputs (if any)
.OUTPUTS
	Output (if any)
.NOTES
	This is a helper funciton and should not be exposed by the module.
#>
function Get-CompilerValidTypeName {
	[CmdletBinding()]
	[OutputType([System.String])]
	param (
		[Parameter(Mandatory, Position = 0, ValueFromPipeline)]
		[System.Type]$Type
	) 
	process {
		trap {
			$PSCmdlet.ThrowTerminatingError($_)
			break;
		}
		$ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
		if ($Type.IsGenericType) {
			$TypeName = $Type.GetGenericTypeDefinition().FullName.Split('`')[0] + "<"
			[System.String[]]$GenericTypes = @()
			foreach ($genericType in $Type.GetGenericArguments()) {
				$GenericTypes += Get-CompilerValidTypeName -Type $genericType
			}
			$TypeName += [System.String]::Join(",", $GenericTypes) + ">"
			return $TypeName
		}
		else {
			return $Type.FullName
		}
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
	PS C:\> Set-TypeDataImportSettings -AutoImportPreference Allowlist -AlwaysCheckAssemblies $false -Persist

	Sets the current settings configuration to allow TypeData to be imported only from assemblies that have been
	specifically allowed. If an assembly has already been checked in the past and was determined to not contain
	TypeData definitions, it will be skipped. The Persist parameter sets these settings to also be used in other
	PowerShell processes opened after running this function.
.INPUTS
	None
.OUTPUTS
	None
.NOTES
	Settings are managed via two singleton instances of the [PSTypeDataAutoImportSettings] class.
	One instance represents the "Current" settings, which override the settings of the second instance.
	The second instance represents the "Persistent" settings, and is the value that "Default" will
	initially be set to in a given PowerShell session.

	This function can set both, but will only set the current settings unless the -Persist SwitchParameter
	is $true.
#>
function Set-TypeDataImportSettings {
	[CmdletBinding(SupportsShouldProcess)]
	param(
		# Set the automatic import behavior for TypeData definitions.
		# All       - TypeData will be imported from every assembly that is imported into the application domain.
		# Blocklist - TypeData will be imported from every assembly that is imported into the application domain
		#             except in cases where the assembly has been explicitly excluded.
		# Allowlist - TypeData will not be imported from any assembly unless that assembly has been added
		#             to an explicit whitelist.
		# None      - TypeData will not be automatically imported into the session.
		[Parameter(ValueFromPipelineByPropertyName)]
		[PSTypeDataAutoImportPreference]
		$AutoImportPreference,

		# Determines if assemblies that do not define TypeData should be cached to avoid searching for definitions
		# within a given assembly in the future, or should always be searched for TypeData definitions.
		[Parameter(ValueFromPipelineByPropertyName)]
		[System.Boolean]
		$AlwaysCheckAssemblies,

		# Determines whether the configured settings should be persisted between sessions or only apply to the
		# current session.
		[Parameter()]
		[System.Management.Automation.SwitchParameter]$Persist
	)
	process {
		trap {
			$PSCmdlet.ThrowTerminatingError($_)
			break
		}
		$ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
		Set-StrictMode -Version 2
		[PSTypeDataAutoImportSettings]$Current = [PSTypeDataAutoImportSettings]::Current
		if ($PSBoundParameters.ContainsKey("AutoImportPreference")) {
			[System.String]$WhatIfAction = $null
			[System.String]$ConfirmAction = $null
			switch ($AutoImportPreference) {
				([PSTypeDataAutoImportPreference]::All) {
					$WhatIfAction = 'Setting TypeData AutoImportPreference to ''All'' (TypeData will be automaticallly imported from every assembly).'
					$ConfirmAction = 'Set TypeData AutoImportPreference to ''All''? (TypeData will be able to be automatically imported from every assembly.)'
				}
				([PSTypeDataAutoImportPreference]::Blocklist) {
					$WhatIfAction = 'Setting TypeData AutoImportPreference to ''Blocklist'' (TypeData will be automatically imported from every assembly unless specifically restricted).'
					$ConfirmAction = 'Set TypeData AutoImportPreference to ''Blocklist''? (TypeData will be able to be automatically imported from every assembly unless specifically restricted.)'
				}
				([PSTypeDataAutoImportPreference]::Allowlist) {
					$WhatIfAction = 'Setting TypeData AutoImportPreference to ''Allowlist'' (TypeData will automatically be imported only from specifically enabled assemblies).'
					$ConfirmAction = 'Set TypeData AutoImportPreference to ''Allowlist''? (TypeData will automatically be imported only from specifically enabled assemblies.)'
				}
				([PSTypeDataAutoImportPreference]::None) {
					$WhatIfAction = 'Setting TypeData AutoImportPreference to ''None'' (TypeData will not be automatically imported).'
					$ConfirmAction = 'Set TypeData AutoImportPreference to ''None''? (TypeData will not be automatically imported.)'
				}
			}
			if ($PSCmdlet.ShouldProcess($WhatIfAction, $ConfirmAction, "Set AutoImportPreference")) {
				$Current.AutoImportPreference = $AutoImportPreference
			}
			if ($Persist) {
				[PSTypeDataAutoImportSettings]::ReloadPersistentScope()
				[PSTypeDataAutoImportSettings]::Persistent.AutoImportPreference = $AutoImportPreference
				[PSTypeDataAutoImportSettings]::Persistent.Save()
			}
		}
		if ($PSBoundParameters.ContainsKey("AlwaysCheckAssemblies")) {
			if ($PSCmdlet.ShouldProcess($AlwaysCheckAssemblies, 'set AlwaysCheckAssemblies for TypeData')) {
				$Current.AlwaysCheckAssemblies = $AlwaysCheckAssemblies
			}
			if ($Persist) {
				[PSTypeDataAutoImportSettings]::ReloadPersistentScope()
				[PSTypeDataAutoImportSettings]::Persistent.AlwaysCheckAssemblies = $AlwaysCheckAssemblies
				[PSTypeDataAutoImportSettings]::Persistent.Save()
			}
		}
	}
}


<#
.SYNOPSIS
	Retrieves the assemblies which have import preferences set.
.DESCRIPTION
	
.EXAMPLE
	PS C:\> Get-TypeDataImportPreference -AssemblyName ([System.String].Assembly.FullName)
	
	IsApplied CanImport Reason  		   AssemblyName
	--------- --------- ------     		   --------------
	True	  False		TypeDataNotDefined mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089

	This example demonstrates use of the command to see what the current import preference settings
	result in for the assembly that defines the type [System.String].
.INPUTS
	[System.Reflection.Assembly]
.OUTPUTS
	[PSSharp.Pseudo.PSTypeDataImportPreferenceItem]
.NOTES
	
#>
function Get-TypeDataImportPreference {
	[CmdletBinding()]
	[OutputType('PSSharp.Pseudo.PSTypeDataImportPreferenceItem')]
	param(
		[Parameter(ValueFromPipeline)]
		[System.Reflection.Assembly[]]
		$Assembly,

		[Parameter()]
		[SupportsWildcards()]
		[System.String[]]
		$AssemblyName
	)
	begin {
		function LikeAny([System.String]$value, [System.String[]]$wildcards) {
			if ($null -eq $wildcards -or $wildcards.Count -eq 0) {
				return $true
			}
			foreach ($wc in $wildcards) {
				if ($value -like $wc) {
					return $true
				}
			}
			return $false
		}
	}
	process {
		trap {
			$PSCmdlet.ThrowTerminatingError($_)
			break;
		}
		$ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
		if ($PSBoundParameters.ContainsKey('Assembly')) {
			$AssemblyName = $Assembly | Select-Object -ExpandProperty FullName
		}
		[PSTypeDataAutoImportSettings]::Current.Blocklist.Where({LikeAny $_ $AssemblyName}) | ForEach-Object {
			[PSCustomObject]@{
				IsApplied		=	[PSTypeDataAutoImportSettings]::Current.AutoImportPreference -eq [PSTypeDataAutoImportPreference]::Blocklist
				CanImport 		=	$false
				Reason			=	'Block'
				AssemblyName	=	$_
				PSTypeName = 'PSSharp.Pseudo.PSTypeDataImportPreferenceItem'
			}
		}
		[PSTypeDataAutoImportPreference]::Current.Allowlist.Where({LikeAny $_ $AssemblyName}) | ForEach-Object {
			[PSCustomObject]@{
				IsApplied		=	[PSTypeDataAutoImportSettings]::Current.AutoImportPreference -eq [PSTypeDataAutoImportPreference]::Allowlist
				CanImport 		=	$true
				Reason			=	'Allow'
				AssemblyName	=	$_
				PSTypeName = 'PSSharp.Pseudo.PSTypeDataImportPreferenceItem'
			}
		}
		[PSTypeDataAutoImportPreference]::Current.SkipList.Where({LikeAny $_ $AssemblyName}) | ForEach-Object {
			[PSCustomObject]@{
				IsApplied		=	![PSTypeDataAutoImportSettings]::Current.AlwaysCheckAssemblies
				CanImport 		=	$false
				Reason			=	'Skip'
				AssemblyName	=	$_
				PSTypeName = 'PSSharp.Pseudo.PSTypeDataImportPreferenceItem'
			}
		}
	}
}


<#
.SYNOPSIS
	Resets all allow and block settings for assemblies.
.DESCRIPTION
	TypeData import preferences are used to determine from which assemblies TypeData may be imported. Different
	settings (configured with Set-TypeDataImportSettings) will determine whether an assembly must be explicitly
	added to an allow-list for the assembly to be imported, or whether the assembly must be added to a blocklist
	to prevent importing it, as well as whether or not assemblies should be imported every time or if assemblies
	previously determined to include no TypeData definitions can be skipped.

	This function will reset all assemblies to the initial state which is undefined. To set the import preference
	for a single assembly, use Set-TypeDataImportPreference.
.EXAMPLE
	PS C:\> Clear-TypeDataImportPreference -Persist
	
	After executing this function, opening a new PowerShell session and importing this module will cause all
	assemblies to be imported as if no blocklist or allowlist has been defined, and no assemblies will be
	skipped due to being in a known state.
.INPUTS
	None
.OUTPUTS
	None
.NOTES
#>
function Clear-TypeDataImportPreference {
	[CmdletBinding()]
	param(
		[Parameter()]
		[System.Management.Automation.SwitchParameter]
		$Persist
	)
	process {
		if ($Persist) {
			[PSTypeDataAutoImportSettings]::ReloadPersistentScope()
			[PSTypeDataAutoImportSettings]::Persistent.Blocklist = @()
			[PSTypeDataAutoImportSettings]::Persistent.Allowlist = @()
			[PSTypeDataAutoImportSettings]::Persistent.Skiplist = @()
			[PSTypeDataAutoImportSettings]::Persistent.Save()
		}
		[PSTypeDataAutoImportSettings]::Current.Blocklist = @()
		[PSTypeDataAutoImportSettings]::Current.Allowlist = @()
		[PSTypeDataAutoImportSettings]::Current.Skiplist = @()
	}
}


<#
.SYNOPSIS
	Determines whether or not TypeData is allowed to be imported from a given assembly.
.DESCRIPTION
	TypeData import preferences are used to determine from which assemblies TypeData may be imported. Different
	settings (configured with Set-TypeDataImportSettings) will determine whether an assembly must be explicitly
	added to an allow-list for the assembly to be imported, or whether the assembly must be added to a blocklist
	to prevent importing it, as well as whether or not assemblies should be imported every time or if assemblies
	previously determined to include no TypeData definitions can be skipped.

	This function will defines the instructions for an individual assembly, to determine whehther is must be
	blocked or is allowed. Note that the settings will only be used if the AutoImportPreference matches the
	setting applied. To determine if a setting is applied, use Get-TypeDataImportPreference and observe the
	IsActive property.
.EXAMPLE
	PS C:\> Set-TypeDataImportPreference -AssemblyName ([System.String].Assembly.FullName) -Setting Blocked -Persist
	
	Assuming that the Set-TypeDataImportSettings function was used to set AutoImportPreference to Blocklist,
	the type data of the assembly that defines the [System.String] type will no longer be automatically 
	imported.
.INPUTS
	[System.String]
.OUTPUTS
	None
.NOTES
#>
function Set-TypeDataImportPreference {
	[CmdletBinding(SupportsShouldProcess, DefaultParameterSetName = "AssemblyNameSet")]
	param(
		[Parameter(ParameterSetName = "AssemblyNameSet", Mandatory, ValueFromPipelineByPropertyName)]
		[Alias('FullName')]
		[TransformationScriptAttribute({
			if ($_ -is [System.Reflection.Assembly]) { return $_.FullName }
			else { return $_ }
		})]
		[ArgumentCompleter({
			[System.AppDomain]::AppDomain.GetAssemblies() `
			| Select-Object -ExpandProperty FullName `
			| Where-Object { $_ -like "$( $args[2] )*" } `
			| ForEach-Object { if ($_ -like '* *') { "'$_'" } else { $_ } }
		})]
		[System.String[]]
		$AssemblyName,

		[Parameter(Mandatory)]
		[Alias("Value")]
		[ValidateSet("Blocked","Allowed","None")]
		[System.String]
		$Setting,

		[Parameter()]
		[System.Management.Automation.SwitchParameter]
		$Persist
	)
	process {
		trap {
			$PSCmdlet.ThrowTerminatingError($_)
			break
		}
		$ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
		if ($PSCmdlet.ShouldProcess($AssemblyName, "set import preference to $Setting")) {
			switch ($Settings) {
				('Blocked') {
					[PSTypeDataAutoImportSettings]::Current.Allowlist = [PSTypeDataAutoImportSettings]::Current.Allowlist -ne $AssemblyName
					[PSTypeDataAutoImportSettings]::Current.Skiplist = [PSTypeDataAutoImportSettings]::Current.Skiplist -ne $AssemblyName
					[PSTypeDataAutoImportSettings]::Current.Blocklist = ([PSTypeDataAutoImportSettings]::Current.Blocklist -ne $AssemblyName) + $AssemblyName
				}
				('Allowed') {
					[PSTypeDataAutoImportSettings]::Current.Allowlist = ([PSTypeDataAutoImportSettings]::Current.Allowlist -ne $AssemblyName) + $AssemblyName
					[PSTypeDataAutoImportSettings]::Current.Skiplist = [PSTypeDataAutoImportSettings]::Current.Skiplist -ne $AssemblyName
					[PSTypeDataAutoImportSettings]::Current.Blocklist = [PSTypeDataAutoImportSettings]::Current.Blocklist -ne $AssemblyName
				}
				('None') {
					[PSTypeDataAutoImportSettings]::Current.Allowlist = [PSTypeDataAutoImportSettings]::Current.Allowlist -ne $AssemblyName
					[PSTypeDataAutoImportSettings]::Current.Skiplist = [PSTypeDataAutoImportSettings]::Current.Skiplist -ne $AssemblyName
					[PSTypeDataAutoImportSettings]::Current.Blocklist = [PSTypeDataAutoImportSettings]::Current.Blocklist -ne $AssemblyName
				}
			}
			if ($Persist) {
				[PSTypeDataAutoImportSettings]::ReloadPersistentScope()
				switch ($Settings) {
					('Blocked') {
						[PSTypeDataAutoImportSettings]::Persistent.Allowlist = [PSTypeDataAutoImportSettings]::Persistent.Allowlist -ne $AssemblyName
						[PSTypeDataAutoImportSettings]::Persistent.Skiplist = [PSTypeDataAutoImportSettings]::Persistent.Skiplist -ne $AssemblyName
						[PSTypeDataAutoImportSettings]::Persistent.Blocklist = ([PSTypeDataAutoImportSettings]::Persistent.Blocklist -ne $AssemblyName) + $AssemblyName
					}
					('Allowed') {
						[PSTypeDataAutoImportSettings]::Persistent.Allowlist = ([PSTypeDataAutoImportSettings]::Persistent.Allowlist -ne $AssemblyName) + $AssemblyName
						[PSTypeDataAutoImportSettings]::Persistent.Skiplist = [PSTypeDataAutoImportSettings]::Persistent.Skiplist -ne $AssemblyName
						[PSTypeDataAutoImportSettings]::Persistent.Blocklist = [PSTypeDataAutoImportSettings]::Persistent.Blocklist -ne $AssemblyName
					}
					('None') {
						[PSTypeDataAutoImportSettings]::Persistent.Allowlist = [PSTypeDataAutoImportSettings]::Persistent.Allowlist -ne $AssemblyName
						[PSTypeDataAutoImportSettings]::Persistent.Skiplist = [PSTypeDataAutoImportSettings]::Persistent.Skiplist -ne $AssemblyName
						[PSTypeDataAutoImportSettings]::Persistent.Blocklist = [PSTypeDataAutoImportSettings]::Persistent.Blocklist -ne $AssemblyName
					}
				}	
				[PSTypeDataAutoImportSettings]::Persistent.Save()
			}
		}
	}
}


<#
.SYNOPSIS
	Retrieves TypeData definition attributes and the reflection instance to which they are applied.
.DESCRIPTION
	This function is uses reflection to determine what TypeData definitions are applied to a given type or
	asssembly without importing the TypeData. This can be used to determine what TypeData is being observed
	in an assembly or PowerShell-defined class type during development.
.EXAMPLE
	PS C:\> [PSSharp.PSScriptProperty('FullName', '$this.FirstName + '' '' + $this.LastName')] 
			class MyPSType { [System.String]$FirstName; [System.String]$LastName }
	PS C:\> Get-TypeDataDefinitions -Type ([MyPSType])
	AttributeDefinition               AttributeTarget
	--------------------------------- ---------------
	PSSharp.PSScriptPropertyAttribute MyPSType

	This example demonstrates using this method with a type defined in PowerShell to retrieve TypeData definitions.
.INPUTS
	[System.Type]
	[System.Reflection.Assembly]
.OUTPUTS
	None
.NOTES
#>
function Get-TypeDataDefinitions {
	[CmdletBinding(DefaultParameterSetName = 'AssemblySet')]
	[OutputType('PSSharp.Pseudo.PSTypeDataDefinition')]
	param(
		# The assembly from which to retrieve TypeData definitions. The assembly and all types defined by the
		# assembly will be scanned for TypeData definitions.
		[Parameter(ParameterSetName = "AssemblySet", ValueFromPipeline)]
		[System.Reflection.Assembly[]]$Assembly,

		# The type from which to retrieve TypeData definitions. Types only need to be evaluated individually
		# for PowerShell defined classes; otherwise, use the -Assembly parameter.
		# An object passed to this parameter that is not [System.Type] will be transformed into that object's
		# type.
		[Parameter(Mandatory, ParameterSetName = "TypeSet", ValueFromPipeline)]
		[System.Type[]]$Type
	)
	begin {
		if ($PSCmdlet.MyInvocation.ExpectingInput) {
			[System.Type[]]$Types = @()
			[System.Reflection.Assembly[]]$Assemblies = @()
		}
	}
	process {
		if ($PSBoundParameters.ContainsKey('Type')) {
			$Types += $Type
		}
		if ($PSBoundParameters.ContainsKey('Assembly')) {
			$Assemblies += $Assembly
		}
	}
	end {
		trap {
			$PSCmdlet.ThrowTerminatingError($_)
			break
		}
		$ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
		if ($PSBoundParameters.ContainsKey('Assembly')) {
			foreach ($asm in $Assemblies) {
				[PSSharp.PSTypeDataAttribute]::GetTypeDataDefinitions($asm).GetEnumerator() | ForEach-Object {
					[PSCustomObject]@{
						'AttributeDefinition' = $_.Key
						'AttributeTarget' = $_.Value
						'PSTypeName' = 'PSSharp.Pseudo.PSTypeDataDefinition'
					}
				}
			}
		}
		elseif ($PSBoundParameters.ContainsKey('Type')) {
			foreach ($t in $Types) {
				[PSSharp.PSTypeDataAttribute]::GetTypeDataDefinitions($t).GetEnumerator() | ForEach-Object {
					[PSCustomObject]@{
						'AttributeDefinition' = $_.Key
						'AttributeTarget' = $_.Value
						'PSTypeName' = 'PSSharp.Pseudo.PSTypeDataDefinition'
					}
				}
			}
		}
		else {
			[System.Reflection.Assembly[]]$Assemblies = [System.AppDomain]::CurrentDomain.GetAssemblies()
			# Check only against current settings. Persistent settings are designed as a starting point
			# for any process, but are ignored at runtime.
			[PSTypeDataAutoImportSettings]$Settings = [PSTypeDataAutoImportSettings]::Current
			[System.Lazy[PSTypeDataAutoImportSettings]]$PersistentSettings = 
				[System.Lazy[PSTypeDataAutoImportSettings]]::new(
					[System.Func[PSTypeDataAutoImportSettings]]{ 
						[PSTypeDataAutoImportSettings]::ReloadPersistentScope()
						return [PSTypeDataAutoImportSettings]::Persistent
					}
				)
			foreach ($asm in $Assemblies) {
				if (!(Test-TypeDataImportPreference -Assembly $asm)) {
					Write-Debug "Skipping assembly '$( $asm.GetName().Name )'."
					continue
				}
				$Definitions = [PSSharp.PSTypeDataAttribute]::GetTypeDataDefinitions($asm)
				Write-Debug ("Assembly '$( $asm.GetName().Name )' contains $( $Definitions.Count ) TypeData definitions.")
				if ($Definitions.Count -eq 0) {
					if (!$PersistentSettings.Value.AlwaysCheckAssemblies -and
						$PersistentSettings.Value.SkipList -notcontains $asm.FullName)
					{
						$PersistentSettings.Value.SkipList += $asm.FullName
					}
					if (!$Settings.AlwaysCheckAssemblies) {
						$Settings.SkipList += $asm.FullName
					}
				}
				$Definitions.GetEnumerator() | ForEach-Object {
					[PSCustomObject]@{
						'AttributeDefinition' = $_.Key
						'AttributeTarget' = $_.Value
						'PSTypeName' = 'PSSharp.Pseudo.PSTypeDataDefinition'
					}
				}
			}
			if ($PersistentSettings.IsValueCreated) {
				Write-Debug 'Saving TypeData exclusions.'
				$PersistentSettings.Value.Save()
			}
		}
	}
}

<#
.SYNOPSIS
	Imports TypeData definitions from a given assembly or PowerShell class type.
.DESCRIPTION
	Imports PowerShell Extended Type System TypeData definitions. If no parameters are defined, this will import
	from all assemblies loaded into the Application Domain where the assembly is allowed to be imported given
	the current TypeData import settings.
.EXAMPLE
	PS C:\> Import-TypeDataDefinitions -Assembly ([MyNamespace.MyClass].Assembly)
	
	Imports all type data defined in the assembly of the [MyNamespace.MyClass] type.
.INPUTS
	[System.Reflection.Assembly]
	[System.Type]
.OUTPUTS
	None
.NOTES
#>
function Import-TypeDataDefinitions {
	[CmdletBinding(DefaultParameterSetName = "ImportAssemblyDefinitions")]
	param(
		# The assembly from which to import TypeData definitions.
		[Parameter(Position = 0, ValueFromPipeline, ParameterSetName = "ImportAssemblyDefinitions")]
		[System.Reflection.Assembly]
		$Assembly,

		# The type from which to import TypeData definitions.
		[Parameter(Position = 0, ValueFromPipeline, ParameterSetName = "ImportTypeDefinitions", Mandatory)]
		[System.Type]
		$Type,

		# The attribute that defines TypeData to be imported.
		[Parameter(DontShow, ValueFromPipelineByPropertyName, ParameterSetName = "FromGetFunction", Mandatory)]
		[PSSharp.PSTypeDataAttribute]
		$AttributeDefinition,

		# The assembly, type, method, or property to which the attribute is applied.
		[Parameter(DontShow, ValueFromPipelineByPropertyName, ParameterSetName = "FromGetFunction", Mandatory)]
		[System.Reflection.ICustomAttributeProvider]
		$AttributeTarget,

		# Overwrites existing TypeData for a type if a member with the same name has been previously defined.
		[Parameter()]
		[System.Management.Automation.SwitchParameter]
		$Force
	)
	begin {
		$TypeDataDefinitions = @()
		$PSBoundParameters.Remove('Force') | Out-Null
	}
	process {
		trap {
			$PSCmdlet.ThrowTerminatingError($_)
			break;
		}
		# $ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
		if ($PSCmdlet.ParameterSetName -eq 'FromGetFunction') {
			$TypeDataDefinitions += [PSCustomObject]@{
				AttributeDefinition	= $AttributeDefinition
				AttributeTarget		= $AttributeTarget
				PSTypeName = 'PSSharp.Pseudo.PSTypeDataDefinition'
			}
		}
		else {
			# Bound parameters can be sent because Assembly is not required by Get function.
			$PSBoundParameters['ErrorAction'] = [System.Management.Automation.ActionPreference]::Continue
			$TypeDataDefinitions += Get-TypeDataDefinitions @PSBoundParameters
		}
	}
	end {
		# trap {
		# 	$PSCmdlet.ThrowTerminatingError($_)
		# 	break;
		# }
		# $ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
		foreach ($TypeDataDefinition in $TypeDataDefinitions) {
			Write-Debug ("Importing TypeData definition " +
						"$( $TypeDataDefinition.AttributeDefinition.GetType().FullName ) " +
						"applied to $( $TypeDataDefinition.AttributeTarget ).")
			[System.Collections.Hashtable]$Params = @{}

			if ($TypeDataDefinition.AttributeDefinition -is [PSSharp.PSTypeConverterAttribute]) {
				if ($TypeDataDefinition.AttributeDefinition.CanConvertTypeNames) {
					$ThisType = $TypeDataDefinition.AttributeTarget.FullName
					foreach ($Type in $TypeDataDefinition.AttributeDefinition.CanConvertTypeNames) {
						$Params = @{
							TypeConverter 	= $ThisType
							TypeName 		= $Type
							ErrorAction 	= [System.Management.Automation.ActionPreference]::SilentlyContinue
							ErrorVariable 	= 'ers'
							Force			= $Force
						}
						Update-TypeData @Params
						foreach ($er in $ers) {
							$PSCmdlet.WriteError($er)
						}
					}
					continue
				}
			}
			elseif ($TypeDataDefinition.AttributeDefinition -is [PSSharp.PSTypeAdapterAttribute]) {
				if ($TypeDataDefinition.AttributeDefinition.CanAdaptTypeNames) {
					$ThisType = $TypeDataDefinition.AttributeTarget.FullName
					foreach ($Type in $TypeDataDefinition.AttributeDefinition.CanAdaptTypeNames) {
						$Params = @{
							TypeAdapter 	= $ThisType
							TypeName 		= $Type
							ErrorAction 	= [System.Management.Automation.ActionPreference]::SilentlyContinue
							ErrorVariable 	= 'ers'
							Force			= $Force
						}
						Update-TypeData @Params
						foreach ($er in $ers) {
							$PSCmdlet.WriteError($er)
						}
					}
					continue
				}
			}
			else {
				# Errors will be handled below. SetParameters() provides abnormal invocation info
				try {
					$TypeDataDefinition.AttributeDefinition.SetParameters($Params, $TypedataDefinition.AttributeTarget)
				} catch {}
			}
			
			#region Error Reporting
			# Validate parameters before passsing to Update-TypeData. I'm likely to be able to provide a more descriptive
			# error based on the attribute's context. Nevertheless, Update-TypeData may throw an error - I must be
			# prepared to wrap that error.

			# To be as descriptive as possible, I'll report all errors identified for a given type at once
			# so that they can *all* be fixed before the developer tries again.
			$Params.Keys | Where-Object { $null -eq $Params[$_] } | ForEach-Object { $Params.Remove($_) } | Out-Null
			[System.Boolean]$IsErrorState = $false
			if ([System.String]::IsNullOrWhiteSpace($Params['TypeName'])) {
				# Update-TypeData requires the TypeName parameter.
				$ex = [PSSharp.TypeDataDefinitionException]::new(
					'No type name was provided for the TypeData definition.', 
					$Params['TypeName'], 
					$Params['MemberType'], 
					$Params['MemberName']
				)
				$er = [System.Management.Automation.ErrorRecord]::new(
					$ex, 
					'TypeNameNotProvided', 
					[System.Management.Automation.ErrorCategory]::MetadataError,
					$TypeDataDefinition
				)
				$er.ErrorDetails = [System.Management.Automation.ErrorDetails]::new(
					"The TypeData definition for the $($params['MemberType']) member '$($params['MemberName'])' " +
					"of type '$($params['TypeName'])' cannot be processed. $ex"
				)
				$PSCmdlet.WriteError($er)
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
					$ex = [PSSharp.TypeDataDefinitionException]::new(
						'A TypeData attribute was present but the TypeData definition could not be determined.',
						'Undefined',
						$Params['TypeName'],
						$Params['MemberName']
					)
					$er = [System.Management.Automation.ErrorRecord]::new(
						$ex,
						'TypeDataNotDefined',
						[System.Management.Automation.ErrorCategory]::MetadataError,
						$TypeDataDefinition
					)
					$er.ErrorDetails = [System.Management.Automation.ErrorDetails]::new(
						"Could not determine the TypeData definition defined in the $( $TypeDataDefinition.AttributeDefinition.GetType().FullName ) attribute on $( $TypeDataDefinition.AttributeTarget ). $ex"
					)
					$PSCmdlet.WriteError($er)
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
						$ex = [PSSharp.TypeDataDefinitionException]::new(
							'A TypeData attribute was present but the TypeData definition could not be determined.',
							'Undefined',
							$Params['TypeName'],
							$Params['MemberName']
						)
						$er = [System.Management.Automation.ErrorRecord]::new(
							$ex,
							'TypeDataNotDefined',
							[System.Management.Automation.ErrorCategory]::MetadataError,
							$TypeDataDefinition
						)
						$er.ErrorDetails = [System.Management.Automation.ErrorDetails]::new(
							"Could not determine the TypeData definition defined in the " +
							"$( $TypeDataDefinition.AttributeDefinition.GetType().FullName ) attribute on $( $TypeDataDefinition.AttributeTarget )."
							# Omit $ex.Message; additional data provided by message will be irrelevant
							# with definition of attribute and location
						)
						$PSCmdlet.WriteError($er)
						$IsErrorState = $true
					}
				}
				if ($RequireMemberName -and [System.String]::IsNullOrWhiteSpace($Params['MemberName'])) {
					$ex = [PSSharp.TypeDataDefinitionException]::new(
						'No member name was provided for the TypeData definition.',
						$Params['TypeName'],
						$Params['MemberType'],
						$Params['MemberName']
					)
					$er = [System.Management.Automation.ErrorRecord]::new(
						$ex,
						'MemberNameNotProvided',
						[System.Management.Automation.ErrorCategory]::MetadataError,
						$TypeDataDefinition
					)
					$er.ErrorDetails = [System.Management.Automation.ErrorDetails]::new(
						"The TypeData definition for the $( $params['MemberType'] ) member '$( $params['MemberName'] )' " +
						"of type '$( $params['TypeName'] )' cannot be processed. $ex"
					)
					$PSCmdlet.WriteError($er)
					$IsErrorState = $true
				}
				if ($RequireValue -and $null -eq $Params['Value']) {
					if ($RequireCodeReference) {
						$ex = [PSSharp.TypeDataDefinitionException]::new(
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
							$TypeDataDefinition
						)
						$er.ErrorDetails = [System.Management.Automation.ErrorDetails]::new(
							"The TypeData definition for the $( $params['MemberType'] ) member " +
							"'$( $params['MemberName'] )' of type '$( $params['TypeName'] )' " +
							"cannot be processed. $ex"
						)
						$PSCmdlet.WriteError($er)
						$IsErrorState = $true
					}
					elseif ($RequireGetCodeReference) {
						$ex = [PSSharp.TypeDataDefinitionException]::new(
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
							$TypeDataDefinition
						)
						$er.ErrorDetails = [System.Management.Automation.ErrorDetails]::new(
							"The TypeData definition for the $( $params['MemberType'] ) member " +
							"'$( $params['MemberName'] )' of type '$( $params['TypeName'] )' " +
							"cannot be processed. $ex"
						)
						$PSCmdlet.WriteError($er)
						$IsErrorState = $true
					}
					else {
						$ex = [PSSharp.TypeDataDefinitionException]::new(
							'No value was provided for the TypeData definition.',
							$Params['TypeName'],
							$Params['MemberType'],
							$Params['MemberName']
						)
						$er = [System.Management.Automation.ErrorRecord]::new(
							$ex,
							'ValueNotProvided',
							[System.Management.Automation.ErrorCategory]::MetadataError,
							$TypeDataDefinition
						)
						$er.ErrorDetails = [System.Management.Automation.ErrorDetails]::new(
							"The TypeData definition for the $( $params['MemberType'] ) member '$( $params['MemberName'] )' " +
							"of type '$( $params['TypeName'] )' cannot be processed. $ex"
						)
						$PSCmdlet.WriteError($er)
						$IsErrorState = $true
					}
				}
				if ($RequireCodeReference -and $null -ne $Params['Value'] -and (
						!$Params['Value'].IsStatic -or 
						$Params['Value'].GetParameters().Count -lt 1 -or
						$Params['Value'].GetParameters()[0].ParameterType -ne [System.Management.Automation.PSObject]
					)) {
					$ex = [PSSharp.TypeDataDefinitionException]::new(
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
						$TypeDataDefinition
					)
					$er.ErrorDetails = [System.Management.Automation.ErrorDetails]::new(
						"The TypeData definition for the $( $params['MemberType'] ) member " +
						"'$( $params['MemberName'] )' of type '$( $params['TypeName'] )' " +
						"cannot be processed. $ex"
					)
						$PSCmdlet.WriteError($er)
					$IsErrorState = $true
				}
				if ($RequireGetCodeReference -and $null -ne $Params['Value'] -and (
						!$Params['Value'].IsStatic -or 
						$Params['Value'].GetParameters().Count -ne 1 -or
						$Params['Value'].GetParameters()[0].ParameterType -ne [System.Management.Automation.PSObject]
					)) {
					$ex = [PSSharp.TypeDataDefinitionException]::new(
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
						$TypeDataDefinition
					)
					$er.ErrorDetails = [System.Management.Automation.ErrorDetails]::new(
						"The TypeData definition for the $( $params['MemberType'] ) member " +
						"'$( $params['MemberName'] )' of type '$( $params['TypeName'] )' " +
						"cannot be processed. $ex"
					)
					$PSCmdlet.WriteError($er)
					$IsErrorState = $true
				}
				if ($RequireSetCodeReference -and $null -ne $Params['SecondValue'] -and (
						!$Params['SecondValue'].IsStatic -or 
						$Params['SecondValue'].GetParameters().Count -ne 2 -or
						$Params['SecondValue'].GetParameters()[0].ParameterType -ne 
						[System.Management.Automation.PSObject] -or
						$Params['SecondValue'].GetParameters()[1].ParameterType -ne
						$Params['Value'].ReturnType
					)) {
					$ex = [PSSharp.TypeDataDefinitionException]::new(
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
						$TypeDataDefinition
					)
					$er.ErrorDetails = [System.Management.Automation.ErrorDetails]::new(
						"The TypeData definition for the $( $params['MemberType'] ) member " +
						"'$( $params['MemberName'] )' of type '$( $params['TypeName'] )' " +
						"cannot be processed. $ex"
					)
					$PSCmdlet.WriteError($er)
					$IsErrorState = $true
				}
			}
			if ($IsErrorState) { continue }
			#endregion

			if ($Force) { $Params['Force'] = $Force }
			Write-Debug "Invoking Update-TypeData with the following parameters: $( [System.String]::Join(", ", [System.String[]]$Params.Keys) )"
			Update-TypeData @Params -ErrorAction SilentlyContinue -ErrorVariable ers
			foreach ($er in $ers) {
				$er.CategoryInfo.Activity = 'Update-TypeData'
				$PSCmdlet.WriteError($er)
			}
		}
	}
}

#endregion

#region Assembly Load Event Listener
Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Management.Automation;
namespace PSSharp.Runtime {
	public static class TypeDataDefinitionInitializer {
		public static bool TypeDataInitiallyImported {
			get { return _added; }
		}
		private static bool _added;
		public static void Subscribe(ScriptBlock script) {
			if (_added) {
				return;
			}
			else {
				_added = true;
			}
			AppDomain.CurrentDomain.AssemblyLoad += (a,b) => {
				List<PSVariable> variables = new List<PSVariable>();
				variables.Add(new PSVariable("Sender", a));
				variables.Add(new PSVariable("AssemblyLoadEventArgs", b));
				variables.Add(new PSVariable("_", b));
				script.InvokeWithContext(null, variables, a, b);
			};
		}
	}
}
'@
if (![PSSharp.Runtime.TypeDataDefinitionInitializer]::TypeDataInitiallyImported) {
	Import-TypeDataDefinitions -ErrorAction Ignore
	[PSSharp.Runtime.TypeDataDefinitionInitializer]::Subscribe({
		if (Test-TypeDataImportPreference -Assembly $_.LoadedAssembly) {
			Import-TypeDataDefinitions -Assembly $_.LoadedAssembly
		}
	})
}
#endregion