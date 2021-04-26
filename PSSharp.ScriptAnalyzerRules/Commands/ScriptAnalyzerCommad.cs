using PSSharp.ScriptAnalyzerRules.Properties;
using System;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PSSharp.ScriptAnalyzerRules
{
    /// <summary>
    /// <para type='synopsis'>Performs a test against an 
    /// <see cref="System.Management.Automation.Language.Ast"/> for the PSScriptAnalyzer.</para>
    /// </summary>
    [OutputType("Microsoft.Windows.PowerShell.ScriptAnalyzer.Generic.DiagnosticRecord[]")]
    public abstract class ScriptAnalyzerCommand : Cmdlet
    {
        /// <summary>
        /// <para type='description'>The Abstract Syntax Tree instance to be tested.</para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipeline = true, HelpMessage = "The Ast to be tested.")]
        public virtual Ast Ast { get; set; } = null!;
        /// <inheritdoc/>
        protected override void ProcessRecord()
        {
            var violations = Ast.FindAll(Predicate, SearchNestedScriptBlocks);
            foreach (var violation in violations)
            {
                WriteObject(CreateResultObject(violation));
            }
        }

        /// <summary>
        /// A predicate used to determine if an Ast is in an error state.
        /// </summary>
        /// <param name="ast">The ast to be tested.</param>
        /// <returns><see langword="true"/> if the ast represents a potential error state.</returns>
        protected abstract bool Predicate(Ast ast);
        /// <summary>
        /// Creates the result object written for violations of the test represented by
        /// this command.
        /// </summary>
        /// <param name="ast"></param>
        /// <returns></returns>
        protected virtual PSObject CreateResultObject(Ast ast)
        {
            var output = new PSObject();
            output.Properties.Add(new PSNoteProperty("Message", Message));
            output.Properties.Add(new PSNoteProperty("Extent", ast.Extent));
            output.Properties.Add(new PSNoteProperty("RuleName", RuleName));
            output.Properties.Add(new PSNoteProperty("Severity", Severity.ToString()));
            return output;
        }
        /// <summary>
        /// The name of the rule associated with infractions. By default, the resource string
        /// identified by the name of the type.
        /// </summary>
        protected virtual string Message { get => Resources.ResourceManager.GetString(GetType().Name); }
        /// <summary>
        /// The name of the rule associated with infractions. By default, the name of the type.
        /// </summary>
        protected virtual string RuleName { get => GetType().Name; }
        /// <inheritdoc cref="DiagnosticSeverity"/>
        protected virtual DiagnosticSeverity Severity { get; }
        /// <summary>
        /// Defines the severity of infractions.
        /// </summary>
        protected enum DiagnosticSeverity
        {
            Information,
            Warning,
            Error,
            ParseError
        }
        /// <summary>
        /// Determines if nested scriptblocks of <see cref="Ast"/> will also be tested for
        /// child asts where <see cref="Predicate(Ast)"/> is true.
        /// </summary>
        protected virtual bool SearchNestedScriptBlocks { get; } = false;
    }
    /// <summary><inheritdoc cref="ScriptAnalyzerCommand"/></summary>
    /// <typeparam name="TAst">The <see cref="System.Management.Automation.Language.Ast"/> type to be tested.</typeparam>
    public abstract class ScriptAnalyzerCommand<TAst> : ScriptAnalyzerCommand
        where TAst: Ast
    {
        /// <inheritdoc/>
        [Parameter(Mandatory = true, ValueFromPipeline = true, HelpMessage = "The Ast to be tested.")]
        new public TAst Ast
        {
            get => (TAst)base.Ast; 
            set => base.Ast = value; 
        }
        /// <inheritdoc/>
        protected sealed override bool Predicate(Ast ast)
        {
            if (ast is TAst)
            {
                return Predicate((TAst)ast);
            }
            else
            {
                return false;
            }
        }
        /// <inheritdoc cref="ScriptAnalyzerCommand.Predicate(System.Management.Automation.Language.Ast)"/>
        protected abstract bool Predicate(TAst ast);
    }
}
