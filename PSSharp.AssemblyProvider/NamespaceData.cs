using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Text;

namespace PSSharp.Providers
{
    public interface IProviderLocation
    {
        public string Name { get; }
        public string FullName { get; }
        public bool HasChildren { get; }
        public IProviderLocation? Parent { get; }
    }
    public class NamespaceData : ReflectedData
    {
        private Assembly _assembly;
        public NamespaceData(string namespaceName, Assembly assembly)
            : base(GetName(namespaceName),
                   GetParent(namespaceName, null),
                   GetParent(namespaceName, assembly),
                   namespaceName,
                   ReflectedDataType.Namespace,
                   true)
        {
            _assembly = assembly;
        }
        private static string GetName(string fullNamespace)
        {
            var parts = fullNamespace.Split('.');
            return parts[parts.Length - 1];
        }
        private static string GetParent(string fullNamespace, Assembly? assembly)
        {
            var parts = fullNamespace.Split('.');
            var parent = string.Join(".", parts.Except(new string[] { parts[parts.Length - 1] }));
            return parent ?? assembly?.FullName ?? string.Empty;
        }
        public override IProviderLocation? Parent
        {
            get
            {
                if (string.IsNullOrEmpty(Namespace))
                {
                    return new AssemblyData(_assembly);
                }
                else
                {
                    return new NamespaceData(Namespace, _assembly);
                }
            }
        }
    }
    public class TypeData : ReflectedData
    {
        public static TypeData Get(Type type)
        {
            switch (GetDataType(type))
            {
                case ReflectedDataType.Class:
                case ReflectedDataType.Delegate:
                case ReflectedDataType.Enum:
                case ReflectedDataType.Struct:
                case ReflectedDataType.Interface:
                    return new TypeData(type);
                default:
                    throw new NotImplementedException("Cannot get type data: no ReflectedTypeData is associated with the provided type.");
            }
        }
        protected TypeData(Type type)
            :base(type.Name, type.Namespace, GetParentPath(type), type.FullName, GetDataType(type), GetTypeHasChildren(type))
        {
            Type = type;
        }
        protected Type Type { get; }
        public Type GetReferencedType() => Type;
        public bool IsPublic => Type.IsPublic;
        private static ReflectedDataType GetDataType(Type type)
        {
            if (type.IsEnum) return ReflectedDataType.Enum;
            if (type.IsValueType) return ReflectedDataType.Struct;
            if (type.IsInterface) return ReflectedDataType.Interface;
            if (typeof(Delegate).IsAssignableFrom(type)) return ReflectedDataType.Delegate;
            if (type.IsClass) return ReflectedDataType.Class;
            throw new NotImplementedException("Cannot get type data: no ReflectedTypeData is associated with the provided type.");
        }
        private static bool GetTypeHasChildren(Type type)
            => type.GetNestedTypes().Length > 0;
        private static string GetParentPath(Type type)
            => type.FullName
            .Substring(0, type.FullName.Length - (type.Name.Length - 1))
            .TrimEnd('.');
        public override IProviderLocation? Parent
        {
            get
            {
                if (Type.IsNested)
                {
                    throw new NotImplementedException();
                }
                if (string.IsNullOrEmpty(Namespace))
                {
                    return new AssemblyData(Type.Assembly);
                }
                else
                {
                    return new NamespaceData(Type.Namespace, Type.Assembly);
                }
            }
        }
    }
    public class AssemblyData : ReflectedData
    {
        public Assembly GetReferencedAssembly() => _assembly;
        private Assembly _assembly;
        public AssemblyData(Assembly assembly)
            : base(assembly?.GetName().Name ?? throw new ArgumentNullException(nameof(assembly)),
                   string.Empty,
                   assembly.Location,
                   string.Empty, //assembly.FullName,
                   ReflectedDataType.Assembly,
                   true)
        {
            _assembly = assembly;
        }
        public override IProviderLocation? Parent => null;
    }
    public abstract class ReflectedData : IProviderLocation
    {
        public ReflectedData(
            string name,
            string namespaceName,
            string parentPath,
            string fullName,
            ReflectedDataType reflectedDataType,
            bool hasChildren)
        {
            Name = name;
            Namespace = namespaceName;
            ParentPath = parentPath;
            FullName = fullName;
            ReflectedDataType = reflectedDataType;
            HasChildren = hasChildren;
        }

        public string Name { get; }
        public string Namespace { get; }
        public string ParentPath { get; }
        public string FullName { get; }
        public ReflectedDataType ReflectedDataType { get; }
        public bool HasChildren { get; }
        public abstract IProviderLocation? Parent { get; }
    }
    public enum ReflectedDataType
    {
        Assembly,
        Namespace,
        Interface,
        Class,
        Struct,
        Enum,
        Delegate
    }
}
