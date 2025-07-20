using Konscious.Security.Cryptography;
using System;
using System.Security.Cryptography;
using System.Text;

namespace DiagnosticExplorer.Api.Security;

public class PasswordHasher
{
    // Use at least 16 bytes (128 bits) for salt
    private const int SaltSize = 16; 
    private const int HashSize = 32; // 256 bits

    public (byte[] hash, byte[] salt) HashPassword(string password)
    {
        // Generate a random salt for this password
        byte[] salt = new byte[SaltSize];
        using var rng = RandomNumberGenerator.Create();
        
        rng.GetBytes(salt);

        using var hasher = new Argon2id(Encoding.UTF8.GetBytes(password));
        
        hasher.Salt = salt;
        hasher.DegreeOfParallelism = 8;  // Number of threads to use
        hasher.MemorySize = 65536;       // 64 MB
        hasher.Iterations = 4;           // Number of iterations

        byte[] hash = hasher.GetBytes(HashSize);
        return (hash, salt);
    }
    
    public string HashSecret(string secret)
    {
        using var hasher = new Argon2id(Encoding.UTF8.GetBytes(secret));
        
        hasher.DegreeOfParallelism = Math.Max(4, Environment.ProcessorCount);  // Number of threads to use
        hasher.MemorySize = 65536;       // 64 MB
        hasher.Iterations = 4;           // Number of iterations

        byte[] hash = hasher.GetBytes(HashSize);
        return Convert.ToBase64String(hash);
    }

    public bool VerifyPassword(string password, byte[] salt, byte[] storedHash)
    {
        using var hasher = new Argon2id(Encoding.UTF8.GetBytes(password));
        
        hasher.Salt = salt;
        hasher.DegreeOfParallelism = 8;
        hasher.MemorySize = 65536;
        hasher.Iterations = 4;

        byte[] newHash = hasher.GetBytes(storedHash.Length);
        return CryptographicOperations.FixedTimeEquals(newHash, storedHash);
    }
    
    public bool VerifySecret(string secret, string storedHash)
    {
        return VerifySecret(secret, Convert.FromBase64String(storedHash));
    }
    
    public bool VerifySecret(string secret, byte[] storedHash)
    {
        using var hasher = new Argon2id(Encoding.UTF8.GetBytes(secret));
        
        hasher.DegreeOfParallelism = 8;
        hasher.MemorySize = 65536;
        hasher.Iterations = 4;

        byte[] newHash = hasher.GetBytes(storedHash.Length);
        return CryptographicOperations.FixedTimeEquals(newHash, storedHash);
    }
}
