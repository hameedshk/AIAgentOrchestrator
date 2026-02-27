using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using AIOrchestrator.App.Hubs;

namespace AIOrchestrator.App.Services
{
    /// <summary>
    /// Manages real-time notifications to connected dashboard clients.
    /// This service is injected into orchestration engine components to push updates.
    /// </summary>
    public class HubConnectionManager
    {
        private readonly IHubContext<OrchestratorHub> _hubContext;
        private readonly ILogger<HubConnectionManager> _logger;

        public HubConnectionManager(IHubContext<OrchestratorHub> hubContext, ILogger<HubConnectionManager> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task NotifyTaskStateChanged(string taskId, string newState)
        {
            try
            {
                var hub = _hubContext.Clients.All;
                await hub.SendAsync("TaskStateChanged", taskId, newState);
                _logger.LogInformation("Notified clients of task {TaskId} state change: {NewState}", taskId, newState);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to notify clients of task state change");
            }
        }

        public async Task NotifyStepCompleted(string taskId, int stepIndex, bool success)
        {
            try
            {
                await _hubContext.Clients.All.SendAsync("StepCompleted", taskId, stepIndex, success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to notify clients of step completion");
            }
        }

        public async Task NotifyResourceAlert(string message, string severity = "warning")
        {
            try
            {
                await _hubContext.Clients.All.SendAsync("ResourceAlert", message, severity);
                _logger.LogWarning("Resource alert sent to clients: {Message}", message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to notify clients of resource alert");
            }
        }

        public async Task NotifyPlanReady(string taskId, string planJson)
        {
            try
            {
                await _hubContext.Clients.All.SendAsync("PlanReady", taskId, planJson);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to notify clients of plan ready");
            }
        }

        public async Task NotifyTaskCompleted(string taskId, bool success)
        {
            try
            {
                await _hubContext.Clients.All.SendAsync("TaskCompleted", taskId, success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to notify clients of task completion");
            }
        }
    }
}
