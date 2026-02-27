using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Xunit;
using AIOrchestrator.App.Security;

namespace AIOrchestrator.App.Tests.Security
{
    public class BearerTokenAuthMiddlewareTests
    {
        [Fact]
        public async Task Middleware_RejectsRequestWithoutBearerToken()
        {
            var context = new DefaultHttpContext();
            context.Request.Headers["Authorization"] = "";
            context.Response.Body = new System.IO.MemoryStream();

            var middleware = new BearerTokenAuthMiddleware(next: async (ctx) => { });
            var store = new InMemoryTokenStore();

            await middleware.InvokeAsync(context, store);

            Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        }

        [Fact]
        public async Task Middleware_RejectsRequestWithInvalidToken()
        {
            var context = new DefaultHttpContext();
            context.Request.Headers["Authorization"] = "Bearer invalid_token";
            context.Response.Body = new System.IO.MemoryStream();

            var middleware = new BearerTokenAuthMiddleware(next: async (ctx) => { });
            var store = new InMemoryTokenStore();

            await middleware.InvokeAsync(context, store);

            Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        }

        [Fact]
        public async Task Middleware_RejectsRequestWithEmptyTokenAfterBearer()
        {
            var context = new DefaultHttpContext();
            context.Request.Headers["Authorization"] = "Bearer ";
            context.Response.Body = new System.IO.MemoryStream();

            var middleware = new BearerTokenAuthMiddleware(next: async (ctx) => { });
            var store = new InMemoryTokenStore();

            await middleware.InvokeAsync(context, store);

            Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        }

        [Fact]
        public async Task Middleware_RejectsRequestWithOnlyBearerPrefix()
        {
            var context = new DefaultHttpContext();
            context.Request.Headers["Authorization"] = "Bearer";
            context.Response.Body = new System.IO.MemoryStream();

            var middleware = new BearerTokenAuthMiddleware(next: async (ctx) => { });
            var store = new InMemoryTokenStore();

            await middleware.InvokeAsync(context, store);

            Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        }

        [Fact]
        public async Task Middleware_AcceptsValidToken()
        {
            var store = new InMemoryTokenStore();
            var validToken = "valid_test_token_12345678901234";
            store.StoreToken(validToken, "TestDevice");

            var context = new DefaultHttpContext();
            context.Request.Headers["Authorization"] = $"Bearer {validToken}";
            var nextCalled = false;

            RequestDelegate next = async (ctx) => { nextCalled = true; };
            var middleware = new BearerTokenAuthMiddleware(next);

            await middleware.InvokeAsync(context, store);

            Assert.True(nextCalled, "Next middleware should be called for valid token");
        }

        [Fact]
        public async Task Middleware_StoresAuthenticationContextCorrectly()
        {
            var store = new InMemoryTokenStore();
            var validToken = "valid_test_token_secure";
            const string deviceName = "TestDevice";
            store.StoreToken(validToken, deviceName);

            var context = new DefaultHttpContext();
            context.Request.Headers["Authorization"] = $"Bearer {validToken}";

            RequestDelegate next = async (ctx) =>
            {
                // Verify context items contain deviceName and authenticatedAt
                Assert.True(ctx.Items.ContainsKey("DeviceName"));
                Assert.Equal(deviceName, ctx.Items["DeviceName"]);
                Assert.True(ctx.Items.ContainsKey("AuthenticatedAt"));
                Assert.IsType<DateTimeOffset>(ctx.Items["AuthenticatedAt"]);

                // Verify token is NOT stored in context items
                Assert.False(ctx.Items.ContainsKey("Token"));
            };

            var middleware = new BearerTokenAuthMiddleware(next);
            await middleware.InvokeAsync(context, store);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        }

        [Fact]
        public async Task Middleware_ThrowsArgumentNullException_WhenContextIsNull()
        {
            var middleware = new BearerTokenAuthMiddleware(next: ctx => Task.CompletedTask);
            var store = new InMemoryTokenStore();

            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await middleware.InvokeAsync(null!, store));
        }

        [Fact]
        public async Task Middleware_ThrowsArgumentNullException_WhenTokenStoreIsNull()
        {
            var middleware = new BearerTokenAuthMiddleware(next: ctx => Task.CompletedTask);
            var context = new DefaultHttpContext();
            context.Request.Headers["Authorization"] = "Bearer valid_token";

            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await middleware.InvokeAsync(context, null!));
        }

        [Fact]
        public void InMemoryTokenStore_ValidateToken_ReturnsFalse_WhenTokenIsNull()
        {
            var store = new InMemoryTokenStore();
            var result = store.ValidateToken(null!, out var deviceName);

            Assert.False(result);
            Assert.Null(deviceName);
        }

        [Fact]
        public void InMemoryTokenStore_ValidateToken_ReturnsFalse_WhenTokenIsEmpty()
        {
            var store = new InMemoryTokenStore();
            var result = store.ValidateToken("", out var deviceName);

            Assert.False(result);
            Assert.Null(deviceName);
        }

        [Fact]
        public void InMemoryTokenStore_ValidateToken_ReturnsFalse_WhenTokenIsWhitespace()
        {
            var store = new InMemoryTokenStore();
            var result = store.ValidateToken("   ", out var deviceName);

            Assert.False(result);
            Assert.Null(deviceName);
        }

        [Fact]
        public void InMemoryTokenStore_StoreToken_ThrowsArgumentException_WhenTokenIsNull()
        {
            var store = new InMemoryTokenStore();

            var exception = Assert.Throws<ArgumentException>(() =>
                store.StoreToken(null, "TestDevice"));

            Assert.Equal("token", exception.ParamName);
        }

        [Fact]
        public void InMemoryTokenStore_StoreToken_ThrowsArgumentException_WhenTokenIsEmpty()
        {
            var store = new InMemoryTokenStore();

            var exception = Assert.Throws<ArgumentException>(() =>
                store.StoreToken("", "TestDevice"));

            Assert.Equal("token", exception.ParamName);
        }

        [Fact]
        public void InMemoryTokenStore_StoreToken_ThrowsArgumentException_WhenDeviceNameIsNull()
        {
            var store = new InMemoryTokenStore();

            var exception = Assert.Throws<ArgumentException>(() =>
                store.StoreToken("valid_token", null));

            Assert.Equal("deviceName", exception.ParamName);
        }

        [Fact]
        public void InMemoryTokenStore_StoreToken_ThrowsArgumentException_WhenDeviceNameIsEmpty()
        {
            var store = new InMemoryTokenStore();

            var exception = Assert.Throws<ArgumentException>(() =>
                store.StoreToken("valid_token", ""));

            Assert.Equal("deviceName", exception.ParamName);
        }

        [Fact]
        public void InMemoryTokenStore_HandlesConcurrentAccess()
        {
            var store = new InMemoryTokenStore();
            var tasks = new List<Task>();

            // Concurrent stores
            for (int i = 0; i < 100; i++)
            {
                var i_copy = i;
                tasks.Add(Task.Run(() => store.StoreToken($"token_{i_copy}", $"device_{i_copy}")));
            }

            Task.WaitAll(tasks.ToArray());

            // Verify all stored
            for (int i = 0; i < 100; i++)
            {
                var success = store.ValidateToken($"token_{i}", out var deviceName);
                Assert.True(success);
                Assert.Equal($"device_{i}", deviceName);
            }
        }

        [Fact]
        public void InMemoryTokenStore_HandlesConcurrentValidation()
        {
            var store = new InMemoryTokenStore();

            // Pre-populate with tokens
            for (int i = 0; i < 50; i++)
            {
                store.StoreToken($"token_{i}", $"device_{i}");
            }

            var tasks = new List<Task>();
            var successCount = 0;
            var lockObj = new object();

            // Concurrent validations
            for (int i = 0; i < 100; i++)
            {
                var i_copy = i;
                tasks.Add(Task.Run(() =>
                {
                    var tokenIndex = i_copy % 50;
                    var success = store.ValidateToken($"token_{tokenIndex}", out var deviceName);
                    if (success)
                    {
                        lock (lockObj)
                        {
                            successCount++;
                        }
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());

            // All 100 validations should succeed
            Assert.Equal(100, successCount);
        }
    }

    internal class InMemoryTokenStore : ITokenStore
    {
        private Dictionary<string, string> _tokens = new();

        public void StoreToken(string token, string deviceName)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Token cannot be null or empty", nameof(token));
            if (string.IsNullOrWhiteSpace(deviceName))
                throw new ArgumentException("Device name cannot be null or empty", nameof(deviceName));

            _tokens[token] = deviceName;
        }

        public bool ValidateToken(string? token, out string? deviceName)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                deviceName = null;
                return false;
            }

            return _tokens.TryGetValue(token, out deviceName);
        }
    }
}
