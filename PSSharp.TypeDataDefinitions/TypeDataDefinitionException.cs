using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace PSSharp
{
    [Serializable]
    public class TypeDataDefinitionException : Exception
    {
        public string? TypeName { get; }
        public string? MemberName { get; }
        public string? MemberType { get; }

        public TypeDataDefinitionException(string message, string memberType, string typeName)
            :this(message ?? "The TypeData definition is for the type is invalid." + 
                 $"\nMemberType: {memberType}\nTypeName: {typeName}")
        {
            TypeName = typeName;
            MemberType = memberType;
        }
        public TypeDataDefinitionException(string message, string memberType, string typeName, string memberName)
            :this(message ?? "The TypeData definition is for the type is invalid." + 
                 $"\nMemberType: {memberType}\nTypeName: {typeName}\nMemberName: {memberName}")
        {
            TypeName = typeName;
            MemberType = memberType;
            MemberName = memberName;
        }

        public TypeDataDefinitionException()
            : base("The TypeData definition is for the type is invalid.")
        {
        }

        public TypeDataDefinitionException(string message) : base(message)
        {
        }

        public TypeDataDefinitionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected TypeDataDefinitionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
