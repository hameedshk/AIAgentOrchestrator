using System;
using Xunit;
using AIOrchestrator.App.Security;
using NSubstitute;

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
            var baseTime = DateTimeOffset.UtcNow;
            var mockClock = Substitute.For<ISystemClock>();
            mockClock.UtcNow.Returns(baseTime);

            var service = new DevicePairingService(clock: mockClock);
            var token = service.GeneratePairingToken();
            service.StorePairingToken(token, "TestDevice");

            var result = service.ValidatePairingToken(token, "TestDevice");
            Assert.True(result);
        }

        [Fact]
        public void ValidatePairingToken_RejectsExpiredToken()
        {
            var baseTime = DateTimeOffset.UtcNow;
            var mockClock = Substitute.For<ISystemClock>();
            mockClock.UtcNow.Returns(baseTime);

            var service = new DevicePairingService(tokenExpirationMinutes: 15, clock: mockClock);
            var token = service.GeneratePairingToken();
            service.StorePairingToken(token, "TestDevice");

            // Token should be valid immediately
            var resultBeforeExpiry = service.ValidatePairingToken(token, "TestDevice");
            Assert.True(resultBeforeExpiry);

            // Generate and store another token before advancing time
            var token2 = service.GeneratePairingToken();
            service.StorePairingToken(token2, "TestDevice2");

            // Advance time past expiration (15 minutes + 1)
            mockClock.UtcNow.Returns(baseTime.AddMinutes(16));

            // Now validate the second token which should be expired
            var resultAfterExpiry = service.ValidatePairingToken(token2, "TestDevice2");
            // This should fail because the mock time has advanced past the 15-minute expiration
            Assert.False(resultAfterExpiry);
        }

        [Fact]
        public void ValidatePairingToken_RejectsInvalidToken()
        {
            var baseTime = DateTimeOffset.UtcNow;
            var mockClock = Substitute.For<ISystemClock>();
            mockClock.UtcNow.Returns(baseTime);

            var service = new DevicePairingService(clock: mockClock);
            var result = service.ValidatePairingToken("invalid_token", "TestDevice");
            Assert.False(result);
        }

        [Fact]
        public void ValidatePairingToken_RejectionEnforcsSingleUse()
        {
            var baseTime = DateTimeOffset.UtcNow;
            var mockClock = Substitute.For<ISystemClock>();
            mockClock.UtcNow.Returns(baseTime);

            var service = new DevicePairingService(clock: mockClock);
            var token = service.GeneratePairingToken();
            service.StorePairingToken(token, "TestDevice");

            // First validation should succeed
            var result1 = service.ValidatePairingToken(token, "TestDevice");
            Assert.True(result1);

            // Second validation should fail (already used)
            var result2 = service.ValidatePairingToken(token, "TestDevice");
            Assert.False(result2);
        }

        [Fact]
        public void ValidatePairingToken_RejectsDeviceNameMismatch()
        {
            var baseTime = DateTimeOffset.UtcNow;
            var mockClock = Substitute.For<ISystemClock>();
            mockClock.UtcNow.Returns(baseTime);

            var service = new DevicePairingService(clock: mockClock);
            var token = service.GeneratePairingToken();
            service.StorePairingToken(token, "ExpectedDevice");

            // Validation with wrong device name should fail
            var result = service.ValidatePairingToken(token, "DifferentDevice");
            Assert.False(result);
        }

        [Fact]
        public void StorePairingToken_ThrowsOnNullToken()
        {
            var service = new DevicePairingService();
#pragma warning disable CS8625
            Assert.Throws<ArgumentNullException>(() => service.StorePairingToken(null, "TestDevice"));
#pragma warning restore CS8625
        }

        [Fact]
        public void StorePairingToken_ThrowsOnEmptyToken()
        {
            var service = new DevicePairingService();
            Assert.Throws<ArgumentNullException>(() => service.StorePairingToken(string.Empty, "TestDevice"));
        }

        [Fact]
        public void StorePairingToken_ThrowsOnNullDeviceName()
        {
            var service = new DevicePairingService();
#pragma warning disable CS8625
            Assert.Throws<ArgumentNullException>(() => service.StorePairingToken("token", null));
#pragma warning restore CS8625
        }

        [Fact]
        public void StorePairingToken_ThrowsOnEmptyDeviceName()
        {
            var service = new DevicePairingService();
            Assert.Throws<ArgumentNullException>(() => service.StorePairingToken("token", string.Empty));
        }

        [Fact]
        public void ValidatePairingToken_ThrowsOnNullToken()
        {
            var service = new DevicePairingService();
#pragma warning disable CS8625
            Assert.Throws<ArgumentNullException>(() => service.ValidatePairingToken(null, "TestDevice"));
#pragma warning restore CS8625
        }

        [Fact]
        public void ValidatePairingToken_ThrowsOnEmptyToken()
        {
            var service = new DevicePairingService();
            Assert.Throws<ArgumentNullException>(() => service.ValidatePairingToken(string.Empty, "TestDevice"));
        }

        [Fact]
        public void ValidatePairingToken_ThrowsOnNullDeviceName()
        {
            var service = new DevicePairingService();
            var token = service.GeneratePairingToken();
            service.StorePairingToken(token, "TestDevice");
#pragma warning disable CS8625
            Assert.Throws<ArgumentNullException>(() => service.ValidatePairingToken(token, null));
#pragma warning restore CS8625
        }

        [Fact]
        public void CleanupExpiredTokens_ReturnsCountOfRemovedTokens()
        {
            var baseTime = DateTimeOffset.UtcNow;
            var mockClock = Substitute.For<ISystemClock>();
            mockClock.UtcNow.Returns(baseTime);

            var service = new DevicePairingService(tokenExpirationMinutes: 15, clock: mockClock);

            // Store multiple tokens
            var token1 = service.GeneratePairingToken();
            var token2 = service.GeneratePairingToken();
            service.StorePairingToken(token1, "Device1");
            service.StorePairingToken(token2, "Device2");

            // Advance time past expiration
            mockClock.UtcNow.Returns(baseTime.AddMinutes(16));

            // Cleanup should remove both tokens
            var removedCount = service.CleanupExpiredTokens();
            Assert.Equal(2, removedCount);
        }
    }
}
