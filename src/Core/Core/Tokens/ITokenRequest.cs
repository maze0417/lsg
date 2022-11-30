namespace LSG.Core.Tokens
{
    public interface ITokenRequest<T> where T : BaseTokenData
    {
        string RawToken { get; set; }
        T TokenData { get; set; }
    }
}