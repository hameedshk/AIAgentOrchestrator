using AIOrchestrator.App.Hubs;
using AIOrchestrator.App.Logging;
using AIOrchestrator.App.Security;
using AIOrchestrator.App.Startup;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);

var schedulerStateDir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "AIOrchestrator",
    "scheduler_state");

builder.Services.ConfigureAIOrchestratorServices(schedulerStateDir);

var app = builder.Build();

// Add Bearer token auth middleware early in pipeline
app.UseMiddleware<BearerTokenAuthMiddleware>();

// Add audit logging middleware after Bearer token auth
app.UseMiddleware<AuditLoggingMiddleware>();

app.UseRouting();
app.MapControllers();
app.MapHub<OrchestratorHub>("/orchestrator-hub");

app.Run();
