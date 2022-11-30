using System.Linq;
using LSG.Core;
using Microsoft.AspNetCore.Http;

namespace LSG.Infrastructure
{
    public interface IClientIpAnalyzer
    {
        string GetIp(HttpContext context);
    }

    public sealed class ClientIpAnalyzer : IClientIpAnalyzer
    {
        private const string HttpForwardFor = "X-FORWARDED-FOR";

        string IClientIpAnalyzer.GetIp(HttpContext context)
        {
            if (context?.Request == null)
            {
                return Const.NotAvailable;
            }

            var remoteIp = context.Connection.RemoteIpAddress;
            var userHostAddress = remoteIp.IsIPv4MappedToIPv6 ? remoteIp.MapToIPv4().ToString() : remoteIp.ToString();

            var forwardIps = context.Request.Headers[HttpForwardFor];

            if (forwardIps.Count == 0)
            {
                return userHostAddress;
            }

            return forwardIps.Select(e => e.Trim()).FirstOrDefault()
                   ?? userHostAddress;
        }
    }
}