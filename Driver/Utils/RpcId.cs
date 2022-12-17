namespace Driver.Utils;

public static class RpcId
{
    public static string GetRandom(int length)
    {
        Span<byte> buf = stackalloc byte[length];
        ThreadRng.Shared.NextBytes(buf);
        return Convert.ToBase64String(buf);
    }
}