using AIOrchestrator.App.Hubs;
using AIOrchestrator.App.Logging;
using AIOrchestrator.App.Security;
using AIOrchestrator.App.Startup;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("orchestrator.config.json", optional: true, reloadOnChange: true)
    .AddJsonFile(
        Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "..", "orchestrator.config.json")),
        optional: true,
        reloadOnChange: true);

var schedulerStateDir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "AIOrchestrator",
    "scheduler_state");

builder.Services.ConfigureAIOrchestratorServices(schedulerStateDir, builder.Configuration);

var app = builder.Build();

app.UseStaticFiles();

// Add Bearer token auth middleware early in pipeline
app.UseMiddleware<BearerTokenAuthMiddleware>();

// Add audit logging middleware after Bearer token auth
app.UseMiddleware<AuditLoggingMiddleware>();

app.UseRouting();

app.MapControllers();
app.MapHub<OrchestratorHub>("/orchestrator-hub");
app.MapHealthChecks("/health");
app.MapHealthChecks("/api/health");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
