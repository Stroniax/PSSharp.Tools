function Coalesce {
    param(
        [Parameter(Position = 0, ValueFromPipeline)]
        [object[]]
        $ArgumentList
    )
    begin {
        [System.Boolean]$returned = $false
    }
    process {
        if ($returned) { return }
        foreach ($arg in $ArgumentList) {
            if ($null -ne $arg) {
                $returned = $true
                return $arg
            }
        }
    }
}

function Register-PSPrintJobServer {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory,ValueFromPipelineByPropertyName,Position=0)]
        [System.String[]]
        $ComputerName,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [SupportsWildcards()]
        [System.String[]]
        $PrinterName
    )
    process {
        Set-StrictMode -Version 2
        [PSPrintJobSourceAdapter]::Servers[$ComputerName] = Coalesce($PrinterName, "*")
    }
}

class PSReadOnlyAttribute : System.Management.Automation.ValidateArgumentsAttribute {
    hidden [PSReadOnlyAttribute()]$IsUnset = $true
    [void] Validate([object]$value, [System.Management.Automation.EngineIntrinsics]$engineIntrinsics) {
        if ($this.IsUnset) {
            $this.IsUnset = $false
        }
        else {
            throw [System.InvalidOperationException]::new('The parameter or variable is read-only.')
        }
    }
}
class PSPrintJobSourceAdapter : System.Management.Automation.JobSourceAdapter {
    hidden
    static 
    [PSReadOnlyAttribute()]
    [System.Collections.Concurrent.ConcurrentDictionary[System.String,System.String[]]]
    $Servers


}