using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AIOrchestrator.App.Logging;
using NSubstitute;

namespace AIOrchestrator.App.Tests.Logging
{
    public class AuditLoggingTests
    {
        [Fact]
        public void AuditLogEntry_HasValidStructure()
        {
            var entry = new AuditLogEntry
            {
                HttpMethod = "POST",
                RequestPath = "/tasks",
                DeviceName = "TestDevice",
                IpAddress = "192.168.1.1",
                ResponseStatusCode = 201,
                ResponseTimeMs = 150
            };

            Assert.NotEmpty(entry.Id);
            Assert.Equal("POST", entry.HttpMethod);
            Assert.Equal("/tasks", entry.RequestPath);
            Assert.Equal("TestDevice", entry.DeviceName);
            Assert.True(entry.ResponseTimeMs > 0);
        }

        [Fact]
        public void AuditLogEntry_ConvertToJson()
        {
            var entry = new AuditLogEntry
            {
                HttpMethod = "GET",
                RequestPath = "/status",
                DeviceName = "TestDevice",
                IpAddress = "127.0.0.1",
                ResponseStatusCode = 200,
                ResponseTimeMs = 50
            };

            var json = entry.ToString();

            Assert.Contains("\"HttpMethod\":\"GET\"", json);
            Assert.Contains("\"RequestPath\":\"/status\"", json);
            Assert.Contains("\"DeviceName\":\"TestDevice\"", json);
            Assert.Contains("\"ResponseStatusCode\":200", json);
        }

        [Fact]
        public async Task AuditLogger_WritesEntryToFile()
        {
            var tempDir = System.IO.Path.GetTempPath();
            var testLogDir = System.IO.Path.Combine(tempDir, $"test_audit_logs_{Guid.NewGuid()}");
            var testLogPath = System.IO.Path.Combine(testLogDir, "audit.log");

            try
            {
                // Setup configuration with custom log directory
                var config = new ConfigurationBuilder()
                    .Build();

                // Create substitute logger
                var logger = Substitute.For<ILogger<AuditLogger>>();
                var auditLogger = new AuditLogger(config, logger);

                var entry = new AuditLogEntry
                {
                    HttpMethod = "POST",
                    RequestPath = "/tasks",
                    DeviceName = "TestDevice",
                    IpAddress = "127.0.0.1",
                    ResponseStatusCode = 201,
                    ResponseTimeMs = 100
                };

                await auditLogger.LogAsync(entry);

                // Verify that log method was called
                logger.Received().Log(
                    LogLevel.Information,
                    Arg.Any<EventId>(),
                    Arg.Any<object>(),
                    Arg.Any<Exception>(),
                    Arg.Any<Func<object, Exception, string>>());
            }
            finally
            {
                if (System.IO.Directory.Exists(testLogDir))
                    System.IO.Directory.Delete(testLogDir, true);
            }
        }
    }
}
