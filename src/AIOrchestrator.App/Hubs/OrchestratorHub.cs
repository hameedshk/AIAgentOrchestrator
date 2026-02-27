using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace AIOrchestrator.App.Hubs
{
    /// <summary>
    /// SignalR hub for real-time communication between server and connected clients.
    /// Notifies clients of task state changes, step completions, and resource alerts.
    /// </summary>
    public class OrchestratorHub : Hub
    {
        private readonly ILogger<OrchestratorHub> _logger;

        public OrchestratorHub(ILogger<OrchestratorHub> logger)
        {
            _logger = logger;
        }

        public override Task OnConnectedAsync()
        {
            _logger.LogInformation("Client {ClientId} connected to OrchestratorHub", Context.ConnectionId);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            _logger.LogInformation("Client {ClientId} disconnected from OrchestratorHub", Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Called by server to notify all connected clients of task state change.
        /// </summary>
        public async Task NotifyTaskStateChanged(string taskId, string newState)
        {
            await Clients.All.SendAsync("TaskStateChanged", taskId, newState);
        }

        /// <summary>
        /// Called by server to notify all connected clients that a step has completed.
        /// </summary>
        public async Task NotifyStepCompleted(string taskId, int stepIndex, bool success)
        {
            await Clients.All.SendAsync("StepCompleted", taskId, stepIndex, success);
        }

        /// <summary>
        /// Called by server to notify all connected clients of resource threshold violations.
        /// </summary>
        public async Task NotifyResourceAlert(string message, string severity)
        {
            await Clients.All.SendAsync("ResourceAlert", message, severity);
        }

        /// <summary>
        /// Called by server to notify clients that a plan is ready for approval.
        /// </summary>
        public async Task NotifyPlanReady(string taskId, string planJson)
        {
            await Clients.All.SendAsync("PlanReady", taskId, planJson);
        }

        /// <summary>
        /// Called by server to notify clients that re-planning has been triggered.
        /// </summary>
        public async Task NotifyReplanTriggered(string taskId)
        {
            await Clients.All.SendAsync("ReplanTriggered", taskId);
        }

        /// <summary>
        /// Called by server to notify clients of task completion.
        /// </summary>
        public async Task NotifyTaskCompleted(string taskId, bool success)
        {
            await Clients.All.SendAsync("TaskCompleted", taskId, success);
        }
    }
}
