using LSG.Core;
using LSG.Infrastructure;
using LSG.Infrastructure.Filters;
using Microsoft.AspNetCore.Mvc;

namespace LSG.Hosts.LsgFrontend.Controllers;

[RouteAcceptWhenSiteIs(Const.Sites.LsgFrontend)]
public class BaseController : ControllerBase
{
    private readonly IResponseCreator _responseCreator;

    public BaseController(
        IResponseCreator responseCreator)
    {
        _responseCreator = responseCreator;
    }
}