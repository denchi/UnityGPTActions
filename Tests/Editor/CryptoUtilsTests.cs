using NUnit.Framework;

public class CryptoUtilsTests
{
    [Test]
    public void EncryptAndDecrypt_RoundTrip_ReturnsOriginalText()
    {
        const string secret = "super secret";
        const string passphrase = "pass";

        var encrypted = CryptoUtils.EncryptString(secret, passphrase);
        var decrypted = CryptoUtils.DecryptString(encrypted, passphrase);

        Assert.AreEqual(secret, decrypted);
    }
}
