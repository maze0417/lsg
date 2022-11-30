using System.Linq;
using LSG.SharedKernel.Extensions;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LSG.Infrastructure.Filters
{
    public sealed class OnlyShowDocFilter : IDocumentFilter
    {
        private readonly string _site;

        public OnlyShowDocFilter(string site)
        {
            _site = site ?? string.Empty;
        }

        void IDocumentFilter.Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            foreach (var apiDescription in context.ApiDescriptions)
            {
                if (apiDescription.TryGetMethodInfo(out var methodInfo))
                {
                    var attr = methodInfo.DeclaringType?.GetCustomAttributes(true)
                        .OfType<RouteAcceptWhenSiteIsAttribute>()
                        .FirstOrDefault(r => r.Sites.Contains(_site));

                    var hasRouteAcceptAttr = attr != null;
                    if (hasRouteAcceptAttr)
                        continue;
                }

                var keys = swaggerDoc.Paths
                    .Where(p => p.Key.Contains(apiDescription.RelativePath))
                    .Select(a => a.Key);

                keys.ForEach(key => swaggerDoc.Paths.Remove(key));
            }
        }
    }
}