using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Text;

namespace PSSharp
{
#warning Cannot validate PSMemberTypes.All
    public class MemberNameValidationAttribute : ValidateArgumentsAttribute
    {
        public MemberNameValidationAttribute(string typeName, PSMemberTypes memberTypes) : this(new PSTypeName(typeName), memberTypes, false) { }
        public MemberNameValidationAttribute(Type type, PSMemberTypes memberTypes) : this(new PSTypeName(type), memberTypes, false) { }
        public MemberNameValidationAttribute(string typeName, PSMemberTypes memberTypes, bool includePrivateMembers) : this(new PSTypeName(typeName), memberTypes, includePrivateMembers) { }
        public MemberNameValidationAttribute(Type type, PSMemberTypes memberTypes, bool includePrivateMembers) : this(new PSTypeName(type), memberTypes, includePrivateMembers) { }
        private MemberNameValidationAttribute(PSTypeName type, PSMemberTypes memberTypes, bool includePrivateMembers)
        {
            _typeName = type;
            PSMemberTypes = memberTypes;
            IncludePrivateMembers = includePrivateMembers;
        }
        private PSTypeName _typeName;
        public string TypeName { get => _typeName.Name; }
        public Type Type { get => _typeName.Type; }
        public PSMemberTypes PSMemberTypes { get; }
        public bool IncludePrivateMembers { get; }
        protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
        {
            var options = GetPSMembers().Concat(GetTypeMembers() ?? Array.Empty<string>());
            Console.WriteLine("Found {0} members: {1}", options.Count(), string.Join(", ", options));
            if (arguments is Array arr)
            {
                foreach (var arg in arr)
                {
                    ValidateString(options, LanguagePrimitives.ConvertTo<string>(arg));
                }
            }
            else
            {
                ValidateString(options, LanguagePrimitives.ConvertTo<string>(arguments));
            }
        }
        private void ValidateString(IEnumerable<string> options, string argument)
        {
            if (!options.Contains(argument, StringComparer.OrdinalIgnoreCase))
            {
                throw new PSArgumentException($"The value '{argument}' is not the name of a valid member of " +
                    $"{_typeName.Type.FullName}. Provide a member name from the following set and try again: " +
                    $"{string.Join(", ", options)}.", "arguments");
            }
        }

        private IEnumerable<string>? GetTypeMembers()
        {
            var flags = BindingFlags.Instance | BindingFlags.Public;
            if (IncludePrivateMembers) flags |= BindingFlags.NonPublic;
            return _typeName.Type?.GetMembers(flags)
                .Where(i => !PSMemberTypes.HasFlag(PSMemberTypes.Property) || i.MemberType.HasFlag(MemberTypes.Property) || i.MemberType.HasFlag(MemberTypes.Field))
                .Where(i => !PSMemberTypes.HasFlag(PSMemberTypes.Method) || i.MemberType.HasFlag(MemberTypes.Method))
                .Where(i => !PSMemberTypes.HasFlag(PSMemberTypes.Event) || i.MemberType.HasFlag(MemberTypes.Event))
                .Select(i => i.Name);
        }
        private IEnumerable<string> GetPSMembers()
        {
            var psobj = new PSObject();
            psobj.TypeNames.Insert(0, _typeName.Name);
            return from i in psobj.Members
                   where i.IsInstance
                   where PSMemberTypes.HasFlag(i.MemberType)
                   select i.Name;
        }
    }
}
