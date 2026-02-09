using System.Net;

namespace MegabonkTogether.Helpers
{
    public static class DnsHelper
    {
        public static string ResolveDomainToIp(string domainName)
        {
            var addresses = Dns.GetHostAddresses(domainName);
            return addresses.Length > 0 ? addresses[0].ToString() : null;
        }
    }
}
