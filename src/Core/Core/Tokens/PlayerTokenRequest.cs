using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace LSG.Core.Tokens
{
    public abstract class PlayerTokenRequest : ITokenRequest<PlayerTokenData>
    {
        [BindNever, JsonIgnore] public string RawToken { get; set; }

        [BindNever, JsonIgnore] public PlayerTokenData TokenData { get; set; }
    }
}