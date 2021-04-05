using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;

namespace Stroniax.PowerShell
{
    /// <summary>
    /// A <see cref="IEqualityComparer{T}"/> to compare string values that may or may not contain wildcards.
    /// </summary>
    public sealed class WildcardComparer : IEqualityComparer<string>
    {
        /// <summary>
        /// Gets a <see cref="WildcardComparer"/> that compares using the given <paramref name="options"/>.
        /// </summary>
        /// <param name="options">The wildcard options to be used by the comparer.</param>
        public static WildcardComparer GetWildcardComparer(WildcardOptions options)
        {
            if (!_comparers.ContainsKey(options))
            {
                _comparers.Add(options, new WildcardComparer(options));
            }
            return _comparers[options];
        }
        /// <summary>
        /// A <see cref="WildcardComparer"/> that compares values using <see cref="WildcardOptions.IgnoreCase"/>.
        /// </summary>
        public static WildcardComparer IgnoreCase
        {
            get => GetWildcardComparer(WildcardOptions.IgnoreCase);
        }
        /// <summary>
        /// A <see cref="WildcardComparer"/> that compares values using <see cref="WildcardOptions.CultureInvariant"/>.
        /// </summary>
        public static WildcardComparer CultureInvariant
            => GetWildcardComparer(WildcardOptions.CultureInvariant);
        /// <summary>
        /// A <see cref="WildcardComparer"/> that compares values using <see cref="WildcardOptions.Compiled"/>.
        /// </summary>
        public static WildcardComparer Compiled
            => GetWildcardComparer(WildcardOptions.Compiled);
        /// <summary>
        /// A <see cref="WildcardComparer"/> that compares values using <see cref="WildcardOptions.None"/>.
        /// </summary>
        public static WildcardComparer None
            => GetWildcardComparer(WildcardOptions.None);

        private static readonly Dictionary<WildcardOptions, WildcardComparer> _comparers = new Dictionary<WildcardOptions, WildcardComparer>();

        private WildcardComparer(WildcardOptions options)
        {
            WildcardOptions = options;
        }
        /// <summary>
        /// Gets the <see cref="System.Management.Automation.WildcardOptions"/> this <see cref="WildcardComparer"/> uses.
        /// </summary>
        public WildcardOptions WildcardOptions { get; } = WildcardOptions.IgnoreCase;
        /// <summary>
        /// Determines if <paramref name="wildcardPattern"/> matches <paramref name="comparisonTarget"/>.
        /// </summary>
        /// <param name="wildcardPattern">The wildcard expression.</param>
        /// <param name="comparisonTarget">The value to match against.</param>
        /// <returns></returns>
        public bool Equals(string wildcardPattern, string comparisonTarget)
            => WildcardPattern.Get(comparisonTarget, WildcardOptions).IsMatch(wildcardPattern);
        /// <summary>
        /// Returns <see cref="string.GetHashCode"/> for <paramref name="obj"/>.
        /// </summary>
        public int GetHashCode(string obj)
            => obj.GetHashCode();
    }
}
