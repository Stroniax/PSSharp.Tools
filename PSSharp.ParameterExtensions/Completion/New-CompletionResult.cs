using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;

namespace PSSharp.Commands
{
    [Cmdlet(VerbsCommon.New, "CompletionResult")]
    public class NewCompletionResultCommand : PSCmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        public string CompletionText { get; set; } = null!;

        [Parameter(Position = 1)]
        public string? ListItemText { get; set; }

        [Parameter(Position = 2)]
        public CompletionResultType ResultType { get; set; } = CompletionResultType.ParameterValue;

        [Parameter(Position = 3)]
        public string? ToolTip { get; set; }

        [Parameter(DontShow = true)]
        public SwitchParameter DoNotEscape { get; set; }

        protected override void ProcessRecord()
        {
            if (!MyInvocation.BoundParameters.ContainsKey(nameof(ListItemText)))
            {
                ListItemText = CompletionText;
            }
            if (!MyInvocation.BoundParameters.ContainsKey(nameof(ToolTip)))
            {
                ToolTip = CompletionText;
            }
            string completionText;
            if (DoNotEscape) {
                completionText = CompletionText;
            }
            else {
                var temp = CodeGeneration.EscapeSingleQuotedStringContent(CompletionText);
                if (temp.Contains(" ") || temp.Contains("'"))
                {
                    completionText = "'" + temp + "'";
                }
                else
                {
                    completionText = temp;
                }
            }
            WriteObject(new CompletionResult(completionText, ListItemText, ResultType, ToolTip));
        }
    }
}
