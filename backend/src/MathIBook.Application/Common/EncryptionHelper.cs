using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MathIBook.Application.Common;

public static class EncryptionHelper
{
    // A fixed 32-character key for AES-256. 
    // In production, this key should be loaded securely from environment variables or configuration.
    private static readonly string KeyString = "MathIBookSecretKeyForTokenEnc123"; 

    public static string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;

        byte[] keyBytes = Encoding.UTF8.GetBytes(KeyString);
        using var aes = Aes.Create();
        aes.Key = keyBytes;
        aes.GenerateIV();
        byte[] iv = aes.IV;

        using var encryptor = aes.CreateEncryptor(aes.Key, iv);
        using var ms = new MemoryStream();
        // Write IV to the beginning of the stream
        ms.Write(iv, 0, iv.Length);

        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    public static string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return cipherText;

        byte[] fullCipher = Convert.FromBase64String(cipherText);
        byte[] keyBytes = Encoding.UTF8.GetBytes(KeyString);

        using var aes = Aes.Create();
        aes.Key = keyBytes;

        byte[] iv = new byte[aes.BlockSize / 8];
        byte[] cipher = new byte[fullCipher.Length - iv.Length];

        Array.Copy(fullCipher, iv, iv.Length);
        Array.Copy(fullCipher, iv.Length, cipher, 0, cipher.Length);

        using var decryptor = aes.CreateDecryptor(aes.Key, iv);
        using var ms = new MemoryStream(cipher);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);

        return sr.ReadToEnd();
    }
}
