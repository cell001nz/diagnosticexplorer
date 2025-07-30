using System;
using System.Security.Cryptography;

namespace DiagnosticExplorer.Util;

public static class HashHelper
{
    public static byte[] ComputeHash(byte[] data)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            return sha256.ComputeHash(data);
        }
    }

    public static string ComputeHashString(byte[] data)
    {
        byte[] hashBytes = ComputeHash(data);
        // Convert hash bytes to a hex string
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}