using System.Security.Cryptography;
using System.Text;

namespace SqlVersioningService.Services;

public class HashingService : IHashingService
{
    public string ComputeHash(string input)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash);
    }
}
