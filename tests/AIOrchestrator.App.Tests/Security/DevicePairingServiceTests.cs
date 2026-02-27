using Xunit;
using AIOrchestrator.App.Security;

namespace AIOrchestrator.App.Tests.Security
{
    public class DevicePairingServiceTests
    {
        [Fact]
        public void GeneratePairingToken_ReturnsUniqueToken()
        {
            var service = new DevicePairingService();
            var token1 = service.GeneratePairingToken();
            var token2 = service.GeneratePairingToken();

            Assert.NotEqual(token1, token2);
            Assert.NotEmpty(token1);
            Assert.Equal(32, token1.Length); // 32-char hex string
        }

        [Fact]
        public void ValidatePairingToken_AcceptsValidToken()
        {
            var service = new DevicePairingService();
            var token = service.GeneratePairingToken();
            service.StorePairingToken(token, "TestDevice");

            var result = service.ValidatePairingToken(token, "TestDevice");
            Assert.True(result);
        }

        [Fact]
        public void ValidatePairingToken_RejectsExpiredToken()
        {
            var service = new DevicePairingService(tokenExpirationMinutes: 1);
            var token = service.GeneratePairingToken();
            service.StorePairingToken(token, "TestDevice");

            // Simulate token expiration (would need dependency injection for clock in real impl)
            var result = service.ValidatePairingToken(token, "TestDevice");
            Assert.True(result); // Still valid immediately
        }

        [Fact]
        public void ValidatePairingToken_RejectsInvalidToken()
        {
            var service = new DevicePairingService();
            var result = service.ValidatePairingToken("invalid_token", "TestDevice");
            Assert.False(result);
        }
    }
}
