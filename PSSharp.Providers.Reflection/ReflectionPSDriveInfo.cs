using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;

namespace PSSharp.Providers
{
    public class ReflectionPSDriveInfo : PSDriveInfo
    {
        internal ReflectionPSDriveInfo(PSDriveInfo driveInfo, Assembly assembly)
            : base(driveInfo)
        {
            Assembly = assembly;
        }
#if DEBUG
        public SortedList<string, ReflectedData> Children
#else
        internal SortedList<string, ReflectionItem> Children
#endif
        { 
            get
            {
                if (_children is null)
                {
                    _children = new SortedList<string, ReflectedData>();
                    var types = Assembly.GetTypes();
                    foreach (var type in types)
                    {
                        var typeInfo = TypeData.Get(type);
                        _children.Add(typeInfo.FullName, typeInfo);
                    }
                    foreach (var ns in types.GroupBy(i => i.Namespace).Select(i => i.Key))
                    {
                        var nsInfo = new NamespaceData(ns, Assembly);
                        _children.Add(nsInfo.FullName, nsInfo);
                    }
                }
                return _children;
            }
        }
        private SortedList<string, ReflectedData>? _children;
        public Assembly Assembly { get; }
    }
}
