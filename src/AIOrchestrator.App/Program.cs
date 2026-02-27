using AIOrchestrator.App.Startup;

var builder = WebApplicationBuilder.CreateBuilder(args);

var schedulerStateDir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "AIOrchestrator",
    "scheduler_state");

builder.Services.ConfigureAIOrchestratorServices(schedulerStateDir);

var app = builder.Build();

app.UseRouting();
app.MapControllers();

app.Run();
