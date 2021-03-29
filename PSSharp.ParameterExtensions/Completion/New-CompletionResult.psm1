<#
.SYNOPSIS
Short description

.DESCRIPTION
Long description

.EXAMPLE
An example

.NOTES
General notes
#>
function New-CompletionResult {
	[CmdletBinding()]
	param(
		[Parameter(Mandatory)]
		[System.String]
		$CompletionText,

		[Parameter()]
		[System.String]
		$ListItemText,

		[Parameter()]
		[System.Management.Automation.CompletionResultType]
		$ResultType = [System.Management.Automation.CompletionResultType]::ParameterValue,

		[Parameter()]
		[System.String]
		$ToolTip
	)
	begin {

	}
	process {
		if (!$PSBoundParameters.ContainsKey('ListItemText')) {
			$ListItemText = $CompletionText
		}
		if (!$PSBoundParameters.ContainsKey('ToolTip')) {
			$ToolTip = $CompletionText
		}
		[System.Management.Automation.CompletionResult]::new($ListItemText, $CompletionText, $ResultType, $ToolTip)
	}
	end {

	}
}