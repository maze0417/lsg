using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace LSG.Core.Tokens
{
    public abstract class AdminTokenRequest : ITokenRequest<AdminTokenData>
    {
        [BindNever, JsonIgnore] public string RawToken { get; set; }
        [BindNever, JsonIgnore] public AdminTokenData TokenData { get; set; }
    }
}