using System;
using System.Security.Cryptography;
using System.Text;

public static class CryptoUtils
{
    public static string EncryptString(string plainText, string passphrase)
    {
        using var aes = Aes.Create();
        aes.GenerateIV();
        byte[] salt = new byte[16];
        RandomNumberGenerator.Fill(salt);
        var key = new Rfc2898DeriveBytes(passphrase, salt, 10000);
        aes.Key = key.GetBytes(32);

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        byte[] inputBytes = Encoding.UTF8.GetBytes(plainText);
        byte[] encryptedBytes = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);

        // Combine: salt + IV + encrypted
        byte[] full = new byte[salt.Length + aes.IV.Length + encryptedBytes.Length];
        Buffer.BlockCopy(salt, 0, full, 0, salt.Length);
        Buffer.BlockCopy(aes.IV, 0, full, salt.Length, aes.IV.Length);
        Buffer.BlockCopy(encryptedBytes, 0, full, salt.Length + aes.IV.Length, encryptedBytes.Length);
        return Convert.ToBase64String(full);
    }

    public static string DecryptString(string encryptedText, string passphrase)
    {
        byte[] full = Convert.FromBase64String(encryptedText);
        byte[] salt = new byte[16];
        byte[] iv = new byte[16];
        byte[] cipher = new byte[full.Length - salt.Length - iv.Length];

        Buffer.BlockCopy(full, 0, salt, 0, salt.Length);
        Buffer.BlockCopy(full, salt.Length, iv, 0, iv.Length);
        Buffer.BlockCopy(full, salt.Length + iv.Length, cipher, 0, cipher.Length);

        var key = new Rfc2898DeriveBytes(passphrase, salt, 10000);
        using var aes = Aes.Create();
        aes.Key = key.GetBytes(32);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        byte[] decryptedBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
        return Encoding.UTF8.GetString(decryptedBytes);
    }
}
