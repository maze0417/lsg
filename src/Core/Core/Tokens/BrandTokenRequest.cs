using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace LSG.Core.Tokens
{
    public abstract class BrandTokenRequest : ITokenRequest<BrandTokenData>
    {
        [BindNever, JsonIgnore] public string RawToken { get; set; }
        [BindNever, JsonIgnore] public BrandTokenData TokenData { get; set; }
    }
}