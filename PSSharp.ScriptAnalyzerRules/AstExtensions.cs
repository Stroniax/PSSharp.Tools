using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;
using System.Text;

namespace PSSharp.ScriptAnalyzerRules.Extensions
{
    internal static class AstExtensions
    {
        public static IEnumerable<T> FindAll<T>(this Ast source, bool searchNestedScriptBlocks)
            where T: Ast
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            return source.FindAll(i => i is T, searchNestedScriptBlocks).Cast<T>();
        }
        public static IEnumerable<T> FindAll<T>(this Ast source, Func<T, bool> predicate, bool searchNestedScriptBlocks)
            where T: Ast
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            return source.FindAll(i => i is T && predicate((T)i), searchNestedScriptBlocks).Cast<T>();
        }
        public static IEnumerable<T> FindAllParents<T>(this Ast source)
            where T: Ast
        {
            return source.FindAllParents<T>(i => true);
        }
        public static IEnumerable<T> FindAllParents<T>(this Ast source, Func<T, bool> predicate)
            where T: Ast
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            Ast ast;
            do
            {
                ast = source.Parent;
                if (ast is T astT && predicate(astT))
                {
                    yield return astT;
                }
            }
            while (ast != null);
        }
        public static Ast GetTopParent(this Ast source)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            Ast parent = source;
            while (parent.Parent != null)
            {
                parent = parent.Parent;
            }
            return parent;
        }
    }
}
