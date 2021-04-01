using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;

namespace PSSharp.Commands
{
    [Cmdlet(VerbsCommon.Clear, "LearnedCompletion", SupportsShouldProcess = true)]
    public class ClearLearnedCompletionCommand : Cmdlet
    {
        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
        [CommandParameterCompletion]
        [SupportsWildcards]
        public string? CommandName { get; set; }
        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
        [CommandParameterCompletion]
        [SupportsWildcards]
        public string? ParameterName { get; set; }

        protected override void ProcessRecord()
        {
            var completions = LearnedCompletionData.GetLearnedCompletions();
            if (completions.Count == 0)
            {
                WriteDebug("No learned completions exist to be cleared.");
                return;
            }
            if (CommandName is null && ParameterName is null)
            {
                if (ShouldProcess($"Removing all {completions.Count} completions.",
                    $"Remove all {completions.Count} completions?",
                    "Clear-LearnedCompletion"))
                {
                    File.Delete(LearnedCompletionData.LearningStoragePath);
                }
                return;
            }
            IEnumerable<LearnedCompletionData> filteredCompletions = completions;
            if (CommandName != null)
            {
                var wc = WildcardPattern.Get(CommandName, WildcardOptions.IgnoreCase);
                filteredCompletions = completions.Where(i => wc.IsMatch(i.CommandName));
            }
            if (ParameterName != null)
            {
                var wc = WildcardPattern.Get(ParameterName, WildcardOptions.IgnoreCase);
                filteredCompletions = filteredCompletions.Where(i => wc.IsMatch(i.ParameterName));
            }
            var filteredCompletionsList = filteredCompletions.ToList();
            if (filteredCompletionsList.Count == 0)
            {
                WriteDebug($"No learned completions were identified with command '{CommandName ?? "*"}'," +
                    $" parameter '{ParameterName ?? "*"}' to be cleared.");
                return;
            }
            int commandCount = filteredCompletionsList
                .GroupBy(i => i.CommandName, StringComparer.OrdinalIgnoreCase)
                .Count();
            int parameterCount = filteredCompletionsList
                .GroupBy(i => i.ParameterName, StringComparer.OrdinalIgnoreCase)
                .Count();
            if (ShouldProcess($"Removing {filteredCompletionsList.Count} completions for {commandCount} " +
                $"command(s) and {parameterCount} parameter(s).",
                $"Remove {filteredCompletionsList.Count} completions for {commandCount} command(s) and {parameterCount} parameter(s)?",
                "Clear-LearnedCompletion"))
            {
                LearnedCompletionData.SetLearnedCompletions(completions.Except(filteredCompletionsList).ToList());
            }
        }
    }
}
