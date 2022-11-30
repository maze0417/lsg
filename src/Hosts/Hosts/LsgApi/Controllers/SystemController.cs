using System.Text;
using LSG.Core;
using LSG.Infrastructure.Filters;
using LSG.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;

namespace LSG.Hosts.LsgApi.Controllers
{
    [RouteAcceptWhenSiteIs(Const.Sites.LsgApi)]
    [ApiController]
    [Route("api")]
    public class SystemController : ControllerBase
    {
        private readonly ICryptoProvider _cryptoProvider;

        public SystemController(ICryptoProvider cryptoProvider)
        {
            _cryptoProvider = cryptoProvider;
        }

        [Route("encrypt/{input}"), HttpGet]
        public string Encrypt(string input)
        {
            return _cryptoProvider.EncryptBytes(Encoding.UTF8.GetBytes(input));
        }

        [Route("decrypt/{input}"), HttpGet]
        public string Decrypt(string input)
        {
            return Encoding.UTF8.GetString(_cryptoProvider.DecryptBytes(input));
        }
    }
}