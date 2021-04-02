using PSSharp;

#region IPv4 Conversion
[assembly: ExternalPSCodeProperty(typeof(System.Net.IPAddress), "AddressBinaryString", typeof(IPv4TypeConverter), nameof(IPv4TypeConverter.GetBinaryAddressString))]
[assembly: ExternalPSDefaultPropertySet (typeof(IPv4SubnetRange),
    nameof(IPv4SubnetRange.CidrNotation),
    nameof(IPv4SubnetRange.SubnetMask),
    nameof(IPv4SubnetRange.NetworkAddress),
    nameof(IPv4SubnetRange.BroadcastAddress),
    nameof(IPv4SubnetRange.StartRange),
    nameof(IPv4SubnetRange.EndRange))]
[assembly: ExternalPSScriptProperty(typeof(IPv4SubnetRange), "Range", "" +
    "$bottom = Convert-IPv4ToNumber $this.StartRange;" +
    "$top = Convert-IPv4ToNumber $this.EndRange;" +
    "for ($i = $bottom; $i -lt $top; $i++) {" +
    "Convert-NumberToIPv4 $i" +
    "}")]
[assembly: ExternalPSAliasProperty(typeof(IPv4SubnetRange), nameof(IPv4SubnetRange.NetworkAddress), nameof(System.Net.IPAddress))]
#endregion IPv4 Conversion