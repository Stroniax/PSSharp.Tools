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
	[System.Boolean]$CompleteDefinition = {0}

	$Variables = Get-Variable -Scope 1 -Name "$wordToComplete*"
	foreach ($variable in $variables) {
		if ($CompleteDefinition) {
			[System.String]$completionText = "`$$( $variable.Name )"
			New-CompletionResult -CompletionText $completionText -ListItemText $variable.Name -ToolTip $variable.CompleteDefinition -DoNotEscape
		}
		else {
			[System.String]$completionText = [System.Management.Automation.Language.CodeGeneration]::EscapeSingleQuotedStringContent($variable.Name)
			New-CompletionResult -CompletionText "'$completionText'" -DoNotEscape
		}
	}
}