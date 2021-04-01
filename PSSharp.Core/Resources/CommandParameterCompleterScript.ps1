using namespace System.Collections;
using namespace System.Management.Automation;
using namespace System.Management.Automation.Language;

param(
	[string]$commandName, 
	[string]$parameterName, 
	[string]$wordToComplete, 
	[CommandAst]$commandAst, 
	[IDictionary]$fakeBoundParameters
)
if ($parameterName -eq 'Parameter' -or $parameterName -eq 'ParameterName') {
	(Get-Command "$($fakeBoundParameters['Command'])$($fakeBoundParameters['CommandName'])*").Parameters.Keys `
	| ForEach-Object { 
		return [CompletionResult]::new(
			[CodeGeneration]::EscapeSingleQuotedStringContent($_),
			$_,
			'ParameterValue',
			$_
		)
	}
}
else {
	(Get-Command "$wordToComplete*").Name `
	| ForEach-Object {
		return [System.Management.Automation.CompletionResult]::new(
			[CodeGeneration]::EscapeSingleQuotedStringContent($_),
			$_,
			'ParameterValue',
			$_
		)
	}
}