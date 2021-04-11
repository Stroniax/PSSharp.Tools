using PSSharp;
using System.Threading.Tasks;

[assembly: ExternalPSScriptProperty(typeof(Task), nameof(Task<object>.Result), ""
    + "Set-StrictMode -Off; "
    + "if ($this.IsCompleted -or $this.IsFaulted -or $this.IsCancelled) {"
    + "return $this.PSBase.Result"
    + "}"
    + "else {"
    + "return $null"
    + "}"
    + "")]
[assembly: ExternalPSDefaultPropertySet(typeof(Task),
                                        nameof(Task.Id),
                                        nameof(Task.Status),
                                        nameof(Task.IsCompleted),
                                        nameof(Task.IsCanceled),
                                        nameof(Task.IsFaulted),
                                        nameof(Task<object>.Result)
                                        )]