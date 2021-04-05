using Microsoft.Management.Infrastructure;
using Microsoft.Management.Infrastructure.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Text;
using System.Threading;

namespace PSSharp.Providers
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class CimPSDriveInfo : PSDriveInfo
    {
        internal CimPSDriveInfo(CimSession session, string name, ProviderInfo providerInfo, string root, string description, PSCredential credential)
            :base(name, providerInfo, root, description, credential)
        {
            CimSession = session;
            GetNamespaces();
        }
        internal CimPSDriveInfo(PSDriveInfo driveInfo, CimSession cimSession)
            : base(driveInfo)
        {
            CimSession = cimSession;
            GetNamespaces();
        }

        internal CimSession CimSession { get; }
        public ConcurrentDictionary<string, List<string>> Namespaces { get; } = new ConcurrentDictionary<string, List<string>>();
        private void GetNamespaces()
        {
            var async = CimSession.EnumerateInstancesAsync("root", "__NAMESPACE");
            var observer = new GetNamespaceObserver(CimSession, Namespaces);
            observer.Disposable = async.Subscribe(observer);
        }
        private class GetClassObserver : IObserver<CimClass>
        {
            internal GetClassObserver(CimSession session, ConcurrentDictionary<string, List<string>> namespaces)
            {
                _namespaces = namespaces;
                _session = session;
            }
            private readonly ConcurrentDictionary<string, List<string>> _namespaces;
            private readonly CimSession _session;
            internal IDisposable? Disposable { get; set; }

            public void OnCompleted() => Disposable?.Dispose();
            public void OnError(Exception error) { }
            public void OnNext(CimClass value)
            {
                var list = _namespaces.GetOrAdd(value.CimSystemProperties.Namespace, i => new List<string>());
                list.Add(value.CimSystemProperties.ClassName);
                value.Dispose();
            }
        }
        private class GetNamespaceObserver : IObserver<CimInstance>
        {
            internal GetNamespaceObserver(CimSession session, ConcurrentDictionary<string, List<string>> namespaces)
            {
                _namespaces = namespaces;
                _session = session;
            }
            private readonly ConcurrentDictionary<string, List<string>> _namespaces;
            private readonly CimSession _session;
            internal IDisposable? Disposable { get; set; }
            public void OnCompleted() => Disposable?.Dispose();
            public void OnError(Exception error) { }
            public void OnNext(CimInstance value)
            {
                var ns = $"{value.CimSystemProperties.Namespace}/{value.CimInstanceProperties["Name"].Value}";
                _namespaces.AddOrUpdate(ns, i => new List<string>(), (a,b) => b);
                
                // collect classes
                var async = _session.EnumerateClassesAsync(ns);
                var observer = new GetClassObserver(_session, _namespaces);
                observer.Disposable = async.Subscribe(observer);

                // collect nested namespaces
                var async2 = _session.EnumerateInstancesAsync(ns, "__NAMESPACE");
                var observer2 = new GetNamespaceObserver(_session, _namespaces);
                observer2.Disposable = async2.Subscribe(observer2);
            }
        }
    }
    [CmdletProvider("Cim", ProviderCapabilities.Credentials | ProviderCapabilities.ExpandWildcards | ProviderCapabilities.ShouldProcess)]
    public sealed class CimProvider : ContainerCmdletProvider
    {

        // Container    -> [namespace]\[class]
        // Item         -> [instance[]]
        // ItemProperty -> [property of instance]

        // EXAMPLE:
        // Container    -> \\root\cimv2\Win32_OperatingSystem
        // Item         -> @( {Disk #0, Partition #0}, {Disk #0, Partition #1}, {Disk #1, Partition #0} )
        // ItemProperty -> Name             : Disk #0, Partition #0
        //              -> NumberOfBlocks   : 449040126
        //              -> Size             : 229908544512
        //              -> BootPartition    : True
        new internal CimPSDriveInfo PSDriveInfo => (CimPSDriveInfo)base.PSDriveInfo;
        private CancellationTokenSource? _cts;
        #region CmdletProvider
        protected override ProviderInfo Start(ProviderInfo providerInfo)
        {
            providerInfo.Description = "Access CIM namespaces, classes, and instances through the PowerShell Cim Provider.";
            providerInfo.Home = "//root/cimv2";
            return base.Start(providerInfo);
        }
        protected override void StopProcessing()
        {
            _cts?.Cancel();
            base.StopProcessing();
        }
        protected override object StartDynamicParameters()
            => base.StartDynamicParameters();
        public override string GetResourceString(string baseName, string resourceId)
            => base.GetResourceString(baseName, resourceId);
        protected override void Stop()
            => base.Stop();
        #endregion
        #region DriveCmdletProvider
        protected override Collection<PSDriveInfo> InitializeDefaultDrives()
        {
            var currentDrive = new CimPSDriveInfo(CimSession.Create(null), "LocalCim",
                                               ProviderInfo,
                                               Environment.MachineName,
                                               "Access to the local CIM.",
                                               Credential);
            return new Collection<PSDriveInfo>()
            {
                currentDrive
            };
        }
        protected override PSDriveInfo? NewDrive(PSDriveInfo drive)
        {
            if (drive is CimPSDriveInfo) return drive;

            var cimSession = (DynamicParameters as RuntimeDefinedParameterDictionary)?["CimSession"].Value as CimSession;
            bool createdSession = false;
            if (cimSession is null)
            {
                cimSession = CimSession.Create(drive.Root);
                createdSession = true;
            }
            if (cimSession.ComputerName != drive.Root)
            {
                if (createdSession) cimSession.Dispose();
                WriteError(new ErrorRecord(
                    new ArgumentException(),
                    "DriveRootCimSessionMismatch",
                    ErrorCategory.InvalidArgument,
                    createdSession ? null : cimSession)
                {
                    ErrorDetails = new ErrorDetails($"The computer name of the CIM session provided ('{cimSession.ComputerName}') does not match the drive root of the drive created ('{drive.Root}').")
                });
                return null;
            }
            var cimDrive = new CimPSDriveInfo(drive, cimSession);
            return base.NewDrive(cimDrive);
        }
        protected override object NewDriveDynamicParameters()
        {
            var parameters = new RuntimeDefinedParameterDictionary();
            parameters.Add("CimSession", new RuntimeDefinedParameter("CimSession", typeof(CimSession), new Collection<Attribute>()
            {
                new ParameterAttribute()
                {
                     ValueFromPipeline = true,
                },
                new ValidateNotNullOrEmptyAttribute(),
                new NoCompletionAttribute()
            }));
            return parameters;
        }
        protected override PSDriveInfo RemoveDrive(PSDriveInfo drive)
        {
            var cimDrive = drive as CimPSDriveInfo;
            cimDrive?.CimSession.Dispose();
            return base.RemoveDrive(drive);
        }
        #endregion
        #region ItemCmdletProvider
        private void SplitPath(string path, out string cimNamespace, out string? cimClass)
        {
            var pathParts = path.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            cimNamespace = "\\" + string.Join("\\", pathParts);

            if (!PSDriveInfo.Namespaces.ContainsKey(cimNamespace))
            {
                cimClass = pathParts[pathParts.Length - 1];
                cimNamespace = cimNamespace.Replace(cimClass, "");
            }
            else
            {
                cimClass = null;
            }
            WriteDebug($"Split path '{path}' to namespace '{cimNamespace}' and class '{cimClass ?? "(null)"}'.");
        }
        private void RefreshCancellationTokenSource()
        {
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
        }
        protected override bool IsValidPath(string path)
        {
            WriteDebug($"IsValidPath: '{path ?? "(null)"}'");
            return System.Text.RegularExpressions.Regex.IsMatch(path, @"^(\w|\\|_)*$");
        }
        protected override void ClearItem(string path) => base.ClearItem(path);
        protected override object ClearItemDynamicParameters(string path) => base.ClearItemDynamicParameters(path);
        protected override string[] ExpandPath(string path)
        {
            Console.WriteLine($"ExpandPath: '{path ?? "(null)"}'");
            var wc = new WildcardPattern(path, WildcardOptions.IgnoreCase);
            var output = new List<string>();
            foreach (var key in PSDriveInfo.Namespaces.Keys)
            {
                if (wc.IsMatch(key)) output.Add(key);
                foreach (var value in PSDriveInfo.Namespaces[key])
                {
                    if (wc.IsMatch(value)) output.Add($"{key}/{value}");
                }
            }
            Console.WriteLine("ExpandPath: {0} matches found", output.Count);
            return output.ToArray();
        }
        protected override void GetItem(string path) => base.GetItem(path);
        protected override object GetItemDynamicParameters(string path) => base.GetItemDynamicParameters(path);
        protected override void InvokeDefaultAction(string path) => base.InvokeDefaultAction(path);
        protected override object InvokeDefaultActionDynamicParameters(string path) => base.InvokeDefaultActionDynamicParameters(path);
        protected override bool ItemExists(string path)
        {
            RefreshCancellationTokenSource();
            var sn = PSDriveInfo.CimSession;
            if (!sn.TestConnection(out _, out var e))
            {
                WriteError(new ErrorRecord(e,
                                           "ConnectionError",
                                           ErrorCategory.ConnectionError,
                                           null)
                {
                    ErrorDetails = new ErrorDetails($"Could not connect to the CIM session. {e.Message}")
                });
                return false;
            }
            else
            {
                SplitPath(path, out string cimNamespace, out string cimClass);
                using var options = new CimOperationOptions()
                {
                    CancellationToken = _cts?.Token,
                    WriteProgress = (activity, currentOperation, statusDescription, percentageCompleted, secondsRemaining)
                    => WriteProgress(new ProgressRecord(0,
                                                        activity,
                                                        statusDescription)
                    {
                        PercentComplete = (int)Math.Min(percentageCompleted, 100),
                        SecondsRemaining = (int)secondsRemaining
                    }),
                    WriteErrorMode = CimCallbackMode.Ignore
                };
                try
                {
                    var r = sn.GetClass(cimNamespace, cimClass, options);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }
        protected override object ItemExistsDynamicParameters(string path) => base.ItemExistsDynamicParameters(path);
        protected override void SetItem(string path, object value) => base.SetItem(path, value);
        protected override object SetItemDynamicParameters(string path, object value) => base.SetItemDynamicParameters(path, value);
        #endregion
        #region ContainerCmdletProvider
        protected override bool ConvertPath(string path, string filter, ref string updatedPath, ref string updatedFilter)
        {
            return base.ConvertPath(path, filter, ref updatedPath, ref updatedFilter);
        }
        protected override void CopyItem(string path, string copyPath, bool recurse)
        {
            base.CopyItem(path, copyPath, recurse);
        }
        protected override object CopyItemDynamicParameters(string path, string destination, bool recurse)
        {
            return base.CopyItemDynamicParameters(path, destination, recurse);
        }
        protected override void GetChildItems(string path, bool recurse)
        {
            base.GetChildItems(path, recurse);
        }
        protected override void GetChildItems(string path, bool recurse, uint depth)
        {
            base.GetChildItems(path, recurse, depth);
        }
        protected override object GetChildItemsDynamicParameters(string path, bool recurse)
        {
            return base.GetChildItemsDynamicParameters(path, recurse);
        }
        protected override void GetChildNames(string path, ReturnContainers returnContainers)
        {
            base.GetChildNames(path, returnContainers);
        }
        protected override object GetChildNamesDynamicParameters(string path)
        {
            return base.GetChildNamesDynamicParameters(path);
        }
        protected override bool HasChildItems(string path)
        {
            return base.HasChildItems(path);
        }
        protected override void NewItem(string path, string itemTypeName, object newItemValue)
        {
            base.NewItem(path, itemTypeName, newItemValue);
        }
        protected override object NewItemDynamicParameters(string path, string itemTypeName, object newItemValue)
        {
            return base.NewItemDynamicParameters(path, itemTypeName, newItemValue);
        }
        protected override void RemoveItem(string path, bool recurse)
        {
            base.RemoveItem(path, recurse);
        }
        protected override object RemoveItemDynamicParameters(string path, bool recurse)
        {
            return base.RemoveItemDynamicParameters(path, recurse);
        }
        protected override void RenameItem(string path, string newName)
        {
            base.RenameItem(path, newName);
        }
        protected override object RenameItemDynamicParameters(string path, string newName)
        {
            return base.RenameItemDynamicParameters(path, newName);
        }
        #endregion
    }
}
