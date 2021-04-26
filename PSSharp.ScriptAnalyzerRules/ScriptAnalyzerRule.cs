using System;
using System.Management.Automation.Language;
using Microsoft.Windows.PowerShell.ScriptAnalyzer.Generic;
using System.Collections.Generic;
using PSSharp.ScriptAnalyzerRules.Properties;
using System.ComponentModel.Composition;

namespace PSSharp.ScriptAnalyzerRules
{
    /// <summary>
    /// A rule used by the PSScriptAnalyzer to validate PowerShell scripting best practice patterns and
    /// assist in avoiding error states.
    /// </summary>
    [Export(typeof(IScriptRule))]
    public abstract class ScriptAnalyzerRule : IScriptRule
    {
        protected abstract bool Predicate(Ast ast);
        public virtual IEnumerable<DiagnosticRecord> AnalyzeScript(Ast ast, string fileName)
        {
            foreach (var violation in ast.FindAll(Predicate, SearchNestedScriptBlocks))
            {
                yield return CreateDiagnosticRecord(ast, fileName);
            }
        }
        /// <summary>
        /// Creates a diagnostic record based on the provided <paramref name="ast"/> and members
        /// of this type such as <see cref="Message"/>.
        /// </summary>
        /// <param name="ast">The ast that a message will be generated for.</param>
        /// <param name="fileName">The name of the file the message pertains to.</param>
        /// <returns></returns>
        public DiagnosticRecord CreateDiagnosticRecord(Ast ast, string? fileName = null)
        {
            return new DiagnosticRecord(
                message: Message,
                extent: ast.Extent,
                ruleName: Name,
                severity: DiagnosticSeverity,
                scriptPath: fileName,
                ruleId: Name);
        }
        public virtual string Name => GetType().Name;
        public virtual string CommonName => Name;
        public virtual string Message => Resources.ResourceManager.GetString(Name);
        public virtual bool SearchNestedScriptBlocks => false;
        public virtual RuleSeverity RuleSeverity => RuleSeverity.Information;
        public virtual DiagnosticSeverity DiagnosticSeverity => DiagnosticSeverity.Information;
        public string GetName() => Name;
        public string GetCommonName() => CommonName;
        public string GetDescription() => Message;
        public string GetSourceName() => "PSSharp";
        public SourceType GetSourceType() => SourceType.Module;
        public RuleSeverity GetSeverity() => RuleSeverity;
    }
    /// <inheritdoc cref="ScriptAnalyzerRule"/>
    /// <typeparam name="TAst">The <see cref="Ast"/> type to test.</typeparam>
    [Export(typeof(IScriptRule))]
    public abstract class ScriptAnalyzerRule<TAst> : ScriptAnalyzerRule
        where TAst: Ast
    {
        /// <inheritdoc cref="ScriptAnalyzerRule.Predicate(Ast)"/>
        protected sealed override bool Predicate(Ast ast) => ast is TAst tAst && Predicate(tAst);
        /// <inheritdoc cref="ScriptAnalyzerRule.Predicate(Ast)"/>
        protected abstract bool Predicate(TAst ast);
    }
}
