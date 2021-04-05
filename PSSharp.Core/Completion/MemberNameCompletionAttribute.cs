using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Reflection;
using System.Text;

namespace PSSharp
{
    public class MemberNameCompletionAttribute : CompletionBaseAttribute
    {
        public MemberNameCompletionAttribute(string typeName, PSMemberTypes memberTypes) : this(new PSTypeName(typeName), memberTypes, false) { }
        public MemberNameCompletionAttribute(Type type, PSMemberTypes memberTypes) : this(new PSTypeName(type), memberTypes, false) { }
        public MemberNameCompletionAttribute(string typeName, PSMemberTypes memberTypes, bool includePrivateMembers) : this(new PSTypeName(typeName), memberTypes, includePrivateMembers) { }
        public MemberNameCompletionAttribute(Type type, PSMemberTypes memberTypes, bool includePrivateMembers) : this(new PSTypeName(type), memberTypes, includePrivateMembers) { }
        private MemberNameCompletionAttribute(PSTypeName type, PSMemberTypes memberTypes, bool includePrivateMembers) : base(
            () => new Completer(type, memberTypes, includePrivateMembers)
            )
        {
            
        }
        
        private class Completer : IArgumentCompleter
        {
            public Completer(PSTypeName type, PSMemberTypes memberTypes, bool includePrivateMembers)
            {
                _typeName = type;
                _memberTypes = memberTypes;
                _includePrivateMembers = includePrivateMembers;
            }
            private readonly PSTypeName _typeName;
            private readonly bool _includePrivateMembers;
            private readonly PSMemberTypes _memberTypes;
            public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst, IDictionary fakeBoundParameters)
            {
                var wc = new WildcardPattern(wordToComplete + "*", WildcardOptions.IgnoreCase);
                return GetPSMembers().Concat(GetTypeMembers() ?? Array.Empty<string>())
                    .Where(i => wc.IsMatch(i))
                    .Select(i => CreateCompletionResult(i));
            }
            private IEnumerable<string>? GetTypeMembers()
            {
                Console.WriteLine($"\nGetting members from type {_typeName.Type}");
                var flags = BindingFlags.Instance | BindingFlags.Public;
                if (_includePrivateMembers) flags |= BindingFlags.NonPublic;
                return _typeName.Type?.GetMembers(flags)
                    .Where(i => !_memberTypes.HasFlag(PSMemberTypes.Property) || i.MemberType.HasFlag(MemberTypes.Property) || i.MemberType.HasFlag(MemberTypes.Field))
                    .Where(i => !_memberTypes.HasFlag(PSMemberTypes.Method) || i.MemberType.HasFlag(MemberTypes.Method))
                    .Where(i => !_memberTypes.HasFlag(PSMemberTypes.Event) || i.MemberType.HasFlag(MemberTypes.Event))
                    .Select(i => i.Name);
            }
            private IEnumerable<string> GetPSMembers()
            {
                Console.WriteLine($"\nGetting members from type name {_typeName.Name}");
                var psobj = new PSObject();
                psobj.TypeNames.Insert(0, _typeName.Name);
                return psobj.Members
                    .Where(i => i.IsInstance)
                    .Where(i => !_memberTypes.HasFlag(PSMemberTypes.AliasProperty) || i.MemberType.HasFlag(PSMemberTypes.AliasProperty))
                    .Where(i => !_memberTypes.HasFlag(PSMemberTypes.CodeMethod) || i.MemberType.HasFlag(PSMemberTypes.CodeMethod))
                    .Where(i => !_memberTypes.HasFlag(PSMemberTypes.CodeProperty) || i.MemberType.HasFlag(PSMemberTypes.CodeProperty))
                    .Where(i => !_memberTypes.HasFlag(PSMemberTypes.Dynamic) || i.MemberType.HasFlag(PSMemberTypes.Dynamic))
                    .Where(i => !_memberTypes.HasFlag(PSMemberTypes.Event) || i.MemberType.HasFlag(PSMemberTypes.Event))
                    .Where(i => !_memberTypes.HasFlag(PSMemberTypes.MemberSet) || i.MemberType.HasFlag(PSMemberTypes.MemberSet))
                    .Where(i => !_memberTypes.HasFlag(PSMemberTypes.Method) || i.MemberType.HasFlag(PSMemberTypes.Method))
                    .Where(i => !_memberTypes.HasFlag(PSMemberTypes.NoteProperty) || i.MemberType.HasFlag(PSMemberTypes.NoteProperty))
                    .Where(i => !_memberTypes.HasFlag(PSMemberTypes.ParameterizedProperty) || i.MemberType.HasFlag(PSMemberTypes.ParameterizedProperty))
                    .Where(i => !_memberTypes.HasFlag(PSMemberTypes.Property) || i.MemberType.HasFlag(PSMemberTypes.Property))
                    .Where(i => !_memberTypes.HasFlag(PSMemberTypes.PropertySet) || i.MemberType.HasFlag(PSMemberTypes.PropertySet))
                    .Where(i => !_memberTypes.HasFlag(PSMemberTypes.ScriptMethod) || i.MemberType.HasFlag(PSMemberTypes.ScriptMethod))
                    .Where(i => !_memberTypes.HasFlag(PSMemberTypes.ScriptProperty) || i.MemberType.HasFlag(PSMemberTypes.ScriptProperty))
                    .Select(i => i.Name);
            }
        }
    }
}
