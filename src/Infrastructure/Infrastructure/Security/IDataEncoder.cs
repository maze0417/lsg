namespace LSG.Infrastructure.Security
{
    public interface IDataEncoder
    {
        string Encode(byte[] bytes);
        byte[] Decode(string encoded);
    }
}