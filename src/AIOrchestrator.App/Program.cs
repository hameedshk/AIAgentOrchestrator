using AIOrchestrator.App.Startup;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);

var schedulerStateDir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "AIOrchestrator",
    "scheduler_state");

builder.Services.ConfigureAIOrchestratorServices(schedulerStateDir);

var app = builder.Build();

app.UseRouting();
app.MapControllers();

app.Run();
