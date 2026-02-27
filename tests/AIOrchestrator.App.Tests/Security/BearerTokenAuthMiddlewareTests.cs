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
        public async Task Middleware_AcceptsValidToken()
        {
            var store = new InMemoryTokenStore();
            var validToken = "valid_test_token_12345678901234";
            store.StoreToken(validToken, "TestDevice");

            var context = new DefaultHttpContext();
            context.Request.Headers["Authorization"] = $"Bearer {validToken}";
            var nextCalled = false;

            Func<HttpContext, Task> next = async (ctx) => { nextCalled = true; };
            var middleware = new BearerTokenAuthMiddleware(next);

            await middleware.InvokeAsync(context, store);

            Assert.True(nextCalled, "Next middleware should be called for valid token");
        }
    }

    internal class InMemoryTokenStore
    {
        private Dictionary<string, string> _tokens = new();

        public void StoreToken(string token, string deviceName)
        {
            _tokens[token] = deviceName;
        }

        public bool ValidateToken(string token, out string deviceName)
        {
            return _tokens.TryGetValue(token, out deviceName);
        }
    }
}
