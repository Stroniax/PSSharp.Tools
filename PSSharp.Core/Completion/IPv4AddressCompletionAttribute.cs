using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Net;
using System.Text;

namespace PSSharp
{
    /// <summary>
    /// Offers argument completion for IPv4 address values. Note that because of the range of possiblities
    /// and certain PowerShell internals operations, only <see cref="MaximumReturnCount"/> options will be
    /// provided. This value is settable, but the default range provides 512 addresses. It is recommended
    /// that to provide a more likely address, the user types the beginning of the address instead of holding
    /// the tab or Ctrl + Space keys.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class IPv4AddressCompletionAttribute : ArgumentCompleterAttribute, IArgumentCompleter
    {
        /// <summary>
        /// The maximum number of options returned by argument completion. Default is 512.
        /// </summary>
        public static decimal MaximumReturnCount { get; set; } = byte.MaxValue * 2;
        /// <summary>
        /// Default constructor.
        /// <para>Offers argument completion for IPv4 address values.</para>
        /// </summary>
        public IPv4AddressCompletionAttribute()
            :base(typeof(IPv4AddressCompletionAttribute))
        {
        }
        /// <summary>
        /// Offers argument completion for IPv4 address values.
        /// </summary>
        /// <param name="commandName">Unused</param>
        /// <param name="parameterName">Unused</param>
        /// <param name="wordToComplete">IP addresses will be matched against any value in this string.
        /// The closer to the intended IP address, the more accurate the argument completion results will be.</param>
        /// <param name="commandAst">Unused</param>
        /// <param name="fakeBoundParameters">Unused</param>
        /// <returns></returns>
        public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst, IDictionary fakeBoundParameters)
        {
            wordToComplete += "*";
            string[]? octetStrings = wordToComplete?.Split('.');
            if (octetStrings?.Length > 4)
            {
                yield break;
            }
            IEnumerable<byte>[] ranges = new IEnumerable<byte>[4];
            for(int i = 0; i < 4; i++)
            {
                string? octetString = octetStrings?.Length > i ? octetStrings[i] : null;
                ranges[i] = CompleteOctet(octetString);
            }


            decimal returned = 0;
            foreach (byte firstOctet in ranges[0])
            {
                foreach (byte secondOctet in ranges[1])
                {
                    foreach (byte thirdOctet in ranges[2])
                    {
                        foreach (byte fourthOctet in ranges[3])
                        {
                            var ip = new IPAddress(new[] { firstOctet, secondOctet, thirdOctet, fourthOctet }).ToString();
                            yield return new CompletionResult(ip, ip, CompletionResultType.ParameterValue, ip);
                            if (returned++ > MaximumReturnCount) yield break;
                        }
                    }
                }
            }
        }

        private IEnumerable<byte> CompleteOctet(string? initial)
        {
            if (string.IsNullOrEmpty(initial))
            {
                return Enumerable.Range(byte.MinValue, byte.MaxValue).Select(i => (byte)i);
            }
            else if (byte.TryParse(initial, out var byt))
            {
                return new[] { byt };
            }
            else
            {
                var wc = WildcardPattern.Get(initial, WildcardOptions.IgnoreCase);
                return Enumerable.Range(byte.MinValue, byte.MaxValue).Where(i => wc.IsMatch(i.ToString())).Select(i => (byte)i);
            }
        }
    }
}
