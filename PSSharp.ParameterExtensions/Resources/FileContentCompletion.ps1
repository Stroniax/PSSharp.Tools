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
	$FilePath = "{0}"

	if (!(Test-Path $FilePath -PathType Leaf)) {
		return
	}

	try {
		[System.IO.FileStream]$FileStream = New-FileStream -FilePath $FilePath -FileMode Open -FileAccess Read
		[System.IO.StreamReader]$FileReader = [System.IO.StreamReader]::new($FileStream)
		while (!$FileReader.EndOfStream) {
			$line = $FileReader.ReadLine()
			if ($line -like "$wordToComplete*") {
				New-CompletionResult -CompletionText $line
			}
		}
	}
	finally {
		if ($null -ne $FileStream) { $FileStream.Dispose() }
		if ($null -ne $FileReader) { $FileReader.Dispose() }
	}
	
}