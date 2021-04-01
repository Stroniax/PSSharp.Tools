[CmdletBinding()]
param(
	[Parameter()]
	[System.String]
	$commandName,
	
	[Parameter()]
	[System.String]
	$parameterName,
	
	[Parameter()]
	[System.String]
	$wordToComplete,
	
	[Parameter()]
	[System.Management.Automation.Language.CommandAst]
	$commandAst,
	
	[Parameter()]
	[System.Collections.IDictionary]
	$fakeBoundParameters
)
process {
	trap{break}
	[System.String[]]$Set = ,{0} | Where-Object {$_}

	foreach ($item in $set) {
		if ($set -like $item) {
			New-CompletionResult -CompletionText $item
		}
	}
}