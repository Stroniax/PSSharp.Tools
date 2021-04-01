using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Text;

namespace PSSharp
{
    /// <summary>
    /// Expands values provided to a parameter or variable meant to be provided paths. This attribute will expand 
    /// the path using <see cref="PathIntrinsics.GetResolvedProviderPathFromPSPath(string, out ProviderInfo)"/> or 
    /// <see cref="PathIntrinsics.GetUnresolvedProviderPathFromPSPath(string, out ProviderInfo, out PSDriveInfo)"/>
    /// (depending on whether <see cref="IsLiteralPath"/> is <see langword="false"/> or not). If a value is provided
    /// for <see cref="ImplementingType"/>, this attribute will also validate that the 
    /// <see cref="ProviderInfo.ImplementingType"/> of the argument(s) provided matches <see cref="ImplementingType"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class PathExpansionAttribute : WildcardExpansionAttribute
    {
        /// <summary>
        /// Indicates that any number of results will be accepted.
        /// </summary>
        public const int AnyResultCount = -1;

        /// <summary>
        /// The value to compare with the <see cref="ProviderInfo.ImplementingType"/> of the path(s) provided.
        /// </summary>
        public Type? ImplementingType { get; set; }
        /// <summary>
        /// Generally for a "Path" parameter this will be false and for a "LiteralPath" or "PSPath" parameter this will be true. When false, wildcards are expanded using <see cref="PathIntrinsics.GetResolvedProviderPathFromPSPath(string, out ProviderInfo)"/>. When true, wildcards are not expanded and the provider is obtained using <see cref="PathIntrinsics.GetUnresolvedProviderPathFromPSPath(string, out ProviderInfo, out PSDriveInfo)"/>.
        /// </summary>
        public bool IsLiteralPath { get; }
        /// <summary>
        /// The number of matching paths that should be identified. If this value is not <see cref="AnyResultCount"/>, the number of resolved paths must match this value or an exception will be thrown.
        /// </summary>
        public int ResultsCount
        {
            get
            {
                return _resultsCount ?? AnyResultCount;
            }
            set
            {
                if (value == AnyResultCount)
                {
                    _resultsCount = null;
                }
                else
                {
                    _resultsCount = value;
                }
            }
        }
        private int? _resultsCount;
        /// <param name="isLiteralPath">Generally for a "Path" parameter this will be false and for a "LiteralPath" or "PSPath" parameter this will be true. When false, wildcards are expanded using <see cref="PathIntrinsics.GetResolvedProviderPathFromPSPath(string, out ProviderInfo)"/>. When true, wildcards are not expanded and the provider is obtained using <see cref="PathIntrinsics.GetUnresolvedProviderPathFromPSPath(string, out ProviderInfo, out PSDriveInfo)"/>.</param>
        public PathExpansionAttribute(bool isLiteralPath)
        {
            IsLiteralPath = isLiteralPath;
        }
        /// <summary>
        /// Expands the path(s) provided.
        /// </summary>
        protected sealed override IEnumerable<object> Expand(object inputDataObj, EngineIntrinsics engineIntrinsics)
        {
            if (inputDataObj is null)
            {
                var argEx = new ArgumentException("The value was not resolved to identify a path.");
                var er = new ErrorRecord(argEx, "NullPath", ErrorCategory.InvalidData, inputDataObj)
                {
                    ErrorDetails = new ErrorDetails($"The value provided cannot be null.")
                };
                throw new RuntimeException($"The value provided cannot be null.", argEx, er);
            }
            var inputData = inputDataObj as string;
            Collection<string> paths;
            ProviderInfo provider;
            if (IsLiteralPath)
            {
                paths = new Collection<string>() { engineIntrinsics.SessionState.Path.GetUnresolvedProviderPathFromPSPath(inputData, out provider, out _) };
            }
            else
            {
                paths = engineIntrinsics.SessionState.Path.GetResolvedProviderPathFromPSPath(inputData, out provider);
            }
            if (!(ImplementingType is null) && provider.ImplementingType != ImplementingType)
            {
                var argEx = new ArgumentException("The value does not belong to the provider required.");
                var er = new ErrorRecord(argEx, "InvalidProvider", ErrorCategory.InvalidData, inputData)
                {
                    ErrorDetails = new ErrorDetails($"The path '{inputData}' does not belong to a provider of type [{ImplementingType.FullName}].")
                };
                throw new RuntimeException($"The path '{inputData}' does not belong to a provider of type [{ImplementingType.FullName}].", argEx, er);
            }
            if (paths.Count == 0)
            {
                // a path with wildcards will not throw an exception even if no path is resolved

                var argEx = new ArgumentException("The value was not resolved to identify a path.");
                var er = new ErrorRecord(argEx, "PathNotFound", ErrorCategory.InvalidData, inputData)
                {
                    ErrorDetails = new ErrorDetails($"The path '{inputData}' was not able to be resolved to identify an existing path.")
                };
                throw new RuntimeException($"The path '{inputData}' was not able to be resolved to identify an existing path.", argEx, er);
            }
            return paths.ToArray();
        }
        /// <summary>
        /// Ensures that the number of output values matches <see cref="_resultsCount"/> unless <see cref="_resultsCount"/> is <see langword="null"/>.
        /// </summary>
        /// <param name="output"></param>
        /// <param name="engineIntrinsics"></param>
        protected sealed override void BeforeOutput(ref object?[] output, EngineIntrinsics engineIntrinsics)
        {
            base.BeforeOutput(ref output, engineIntrinsics);
            if (_resultsCount.HasValue && output.Length != _resultsCount.Value)
            {
                var ex = new ArgumentException("The value provided matches an invalid number of paths.");
                var er = new ErrorRecord(ex, "InvalidPathCount", ErrorCategory.InvalidData, output)
                {
                    ErrorDetails = new ErrorDetails($"The provided value must match {_resultsCount.Value} path(s), but {output.Length} value(s) were identified.")
                };
                throw new RuntimeException(er.ErrorDetails.Message, ex, er);
            }
        }
    }
}
