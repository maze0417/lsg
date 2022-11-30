using System;
using LSG.Core;
using LSG.Infrastructure;
using LSG.Infrastructure.Filters;
using Microsoft.AspNetCore.Mvc;

namespace LSG.Hosts.LsgFrontend.Controllers
{
    [RouteAcceptWhenSiteIs(Const.Sites.LsgFrontend)]
    [Route("api/config")]
    public class ConfigController : ControllerBase
    {
        private readonly ILsgConfig _lsgConfig;
        private readonly IResponseCreator _responseCreator;
        

        public ConfigController(ILsgConfig lsgConfig, IResponseCreator responseCreator)
        {
            _lsgConfig = lsgConfig;
            _responseCreator = responseCreator;
        }

        [Route(""), HttpGet]
        public IActionResult GetAsync()
        {
            return _responseCreator.CreateOkResponse(new
            {
                _lsgConfig.CdnPath,
                _lsgConfig.LsgFrontendUIStyle,
            });
        }

        [Route("all"), HttpGet]
        public IActionResult GetAllAsync(string key = null)
        {
            if (key != Const.StatusKey)
                throw new UnauthorizedAccessException();

            return _responseCreator.CreateOkResponse(new
            {
                Config = _lsgConfig.GetAllSetting()
            });
        }
    }
}