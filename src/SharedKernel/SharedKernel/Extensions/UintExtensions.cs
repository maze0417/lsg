namespace LSG.SharedKernel.Extensions
{
    public static class UintExtensions
    {
        public static string ToHexString(this uint input)
        {
            return input.ToString("x8");
        }
    }
}