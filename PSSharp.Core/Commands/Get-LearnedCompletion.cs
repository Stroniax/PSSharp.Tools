using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;

namespace PSSharp.Commands
{
    [Cmdlet(VerbsCommon.Get, "LearnedCompletion")]
    [OutputType(typeof(LearnedCompletionData))]
    public class GetLearnedCompletionCommand : Cmdlet
    {
        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [CommandParameterCompletion]
        [SupportsWildcards]
        public string? CommandName { get; set; }

        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [CommandParameterCompletion]
        [SupportsWildcards]
        public string? ParameterName { get; set; }

        protected override void ProcessRecord()
        {
            var commandName = CommandName is null ? null : WildcardPattern.Get(CommandName, WildcardOptions.IgnoreCase);
            var parameterName = ParameterName is null ? null : WildcardPattern.Get(ParameterName, WildcardOptions.IgnoreCase);
            
            var completions = LearnedCompletionData.GetLearnedCompletions()
                .Where(c => commandName?.IsMatch(c.CommandName) ?? true)
                .Where(c => parameterName?.IsMatch(c.ParameterName) ?? true)
                .ToList();
            WriteObject(completions, true);
            if ((CommandName != null && !WildcardPattern.ContainsWildcardCharacters(CommandName) 
                || ParameterName != null && !WildcardPattern.ContainsWildcardCharacters(ParameterName)
                ) && completions.Count == 0)
            {
                WriteError(new ErrorRecord(
                    new ItemNotFoundException("No learned completions were found."),
                    "CompletionNotFound",
                    ErrorCategory.ObjectNotFound,
                    $"{CommandName ?? "*"}:{ParameterName ?? "*"}")
                {
                    ErrorDetails = new ErrorDetails($"No learned completions were found for command " +
                    $"'{CommandName ?? "*"}', parameter '{ParameterName ?? "*"}'.")
                });
            }
        }
    }
}
