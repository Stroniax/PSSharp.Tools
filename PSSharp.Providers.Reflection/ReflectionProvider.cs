using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Reflection;

namespace PSSharp.Providers
{
    [CmdletProvider("Reflection",
        ProviderCapabilities.ExpandWildcards | ProviderCapabilities.Include | ProviderCapabilities.Exclude | ProviderCapabilities.Filter)]
    public class ReflectionProvider : ContainerCmdletProvider //, IPropertyCmdletProvider
    {
        new ReflectionPSDriveInfo PSDriveInfo => (ReflectionPSDriveInfo)base.PSDriveInfo;
        public ReflectionProvider()
        {
            Console.WriteLine("ReflectionProvider: .ctor()");
        }
        protected override bool IsValidPath(string path)
        {
            Console.WriteLine("IsValidPath() -> testing '{0}'", path);
            var result = System.Text.RegularExpressions.Regex.IsMatch(path, @"^(\w|`|[[]|[]]|[.])*$");
            Console.WriteLine("IsValidPath() -> '{0}' ? {1}", path, result);
            return result;
        }
        protected override string[] ExpandPath(string path)
        {
            Console.WriteLine("ExpandPath() -> expanding '{0}'", path);
            var wc = WildcardPattern.Get(SplitPath(path) + "*", WildcardOptions.IgnoreCase);
            var publicTypes = PSDriveInfo.Children.Values.Select(i => i.FullName).Where(i => wc.IsMatch(i)).ToArray();
            if (publicTypes.Length != 0)
            {
                Console.WriteLine("ExpandPath() -> found {0} paths (public).", publicTypes.Length);
                return publicTypes;
            }
            else
            {
                var allTypes = PSDriveInfo.Children.Values.Select(i => i.FullName).Where(i => wc.IsMatch(i)).ToArray();
                Console.WriteLine("ExpandPath() -> found {0} paths (all).", allTypes.Length);
                return allTypes;
            }
        }
        protected override bool ItemExists(string path)
        {
            Console.WriteLine("ItemExists() -> testing path '{0}'", path);
            path = SplitPath(path);
            var wc = WildcardPattern.Get(path, WildcardOptions.IgnoreCase);
            return string.IsNullOrEmpty(path) || PSDriveInfo.Children.Values.Select(i => i.FullName).Any(i => wc.IsMatch(i));
        }
        protected override PSDriveInfo NewDrive(PSDriveInfo drive)
        {
            Assembly assembly;
            if (File.Exists(drive.Root))
            {
                assembly = Assembly.ReflectionOnlyLoadFrom(drive.Root);
            }
            else
            {
                assembly = Assembly.Load(drive.Root);
            }
            if (assembly is null)
            {
                ThrowTerminatingError(new ErrorRecord(
                    new ArgumentException("Could not load an assembly from the provided drive root."),
                    "NoAssemblyLoaded",
                    ErrorCategory.InvalidArgument,
                    drive.Root
                    )
                {
                    ErrorDetails = new ErrorDetails($"No assembly could be loaded from the file path or assembly name '{drive.Root}'.")
                });
                return null!;
            }

            return new ReflectionPSDriveInfo(drive, assembly);
        }
        protected override void GetChildItems(string path, bool recurse)
            => GetChildItems(path, recurse, uint.MaxValue);
        private string SplitPath(string path)
        {
            Console.WriteLine($"AssemblyProvider: SplitPath() -> '{path}'");
            return path;
        }
        protected override void GetChildItems(string path, bool recurse, uint depth)
        {
            Console.WriteLine("GetChildItems() -> getting children for path '{0}'", path);
            var wc = WildcardPattern.Get(path, WildcardOptions.IgnoreCase);
            var items = PSDriveInfo.Children.Values
                .Where(i => wc.IsMatch(i.FullName) && (Force || (i is TypeData td ? td.IsPublic : true)));
            Console.WriteLine("GetChildItems() -> found {0} child items", items.Count());
            foreach (var item in items)
            {
                WriteItemObject(item, item.FullName, item is AssemblyData || item is NamespaceData);
            }
        }
        protected override void GetChildNames(string path, ReturnContainers returnContainers)
        {
            Console.WriteLine("GetChildNames() -> getting children for path '{0}'", path);
            var items = PSDriveInfo.Children
                .Values
                .Where(i => i.FullName.StartsWith(path, StringComparison.OrdinalIgnoreCase)
                && !i.FullName.Equals(path, StringComparison.OrdinalIgnoreCase));
            Console.WriteLine("GetChildNames() -> found {0} child items", items.Count());
            foreach (var item in items)
            {
                WriteItemObject(item, item.FullName, item is AssemblyData || item is NamespaceData);
            }
        }
        protected override void GetItem(string path)
        {
            path = SplitPath(path);
            Console.WriteLine("GetItem() -> getting item(s) for path '{0}'", path);
            if (string.IsNullOrEmpty(path))
            {
                Console.WriteLine("GetItem() -> getting assembly item");
                var item = new AssemblyData(PSDriveInfo.Assembly);
                WriteItemObject(item, string.Empty, true);
                return;
            }
            var items = PSDriveInfo.Children.Values
                .Where(i => i.FullName.Equals(path, StringComparison.OrdinalIgnoreCase));
            Console.WriteLine("GetItem() -> found {0} items", items.Count());
            foreach (var item in items)
            {
                WriteItemObject(item, item.FullName, item is AssemblyData || item is NamespaceData);
            }

        }
        protected override bool HasChildItems(string path)
        {
            Console.WriteLine("HasChildItems() -> testing if '{0}' has child items.", path);
            return PSDriveInfo.Children.Values
                .Where(i => i.FullName.Equals(path, StringComparison.OrdinalIgnoreCase))
                .Any(i => i.HasChildren);
        }
    }
}
