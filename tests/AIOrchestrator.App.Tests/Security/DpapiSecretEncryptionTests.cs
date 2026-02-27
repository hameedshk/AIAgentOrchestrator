using Xunit;
using AIOrchestrator.App.Security;

namespace AIOrchestrator.App.Tests.Security
{
    public class DpapiSecretEncryptionTests
    {
        [Fact]
        public void EncryptSecret_ReturnsEncryptedValue()
        {
            var encryption = new DpapiSecretEncryption();
            var plaintext = "super_secret_password_123";

            var encrypted = encryption.Encrypt(plaintext);

            Assert.NotEqual(plaintext, encrypted);
            Assert.NotEmpty(encrypted);
        }

        [Fact]
        public void DecryptSecret_ReturnsOriginalValue()
        {
            var encryption = new DpapiSecretEncryption();
            var plaintext = "super_secret_password_123";

            var encrypted = encryption.Encrypt(plaintext);
            var decrypted = encryption.Decrypt(encrypted);

            Assert.Equal(plaintext, decrypted);
        }

        [Fact]
        public void Decrypt_InvalidData_ThrowsInvalidOperationException()
        {
            var encryption = new DpapiSecretEncryption();

            Assert.Throws<InvalidOperationException>(() =>
            {
                encryption.Decrypt("invalid_base64_data_!!!!");
            });
        }

        [Fact]
        public void EncryptMultipleTimes_ProducesDifferentOutput()
        {
            var encryption = new DpapiSecretEncryption();
            var plaintext = "same_secret";

            var encrypted1 = encryption.Encrypt(plaintext);
            var encrypted2 = encryption.Encrypt(plaintext);

            // DPAPI should produce different ciphertext each time due to entropy
            Assert.NotEqual(encrypted1, encrypted2);

            // Both should decrypt to same plaintext
            Assert.Equal(plaintext, encryption.Decrypt(encrypted1));
            Assert.Equal(plaintext, encryption.Decrypt(encrypted2));
        }
    }
}
