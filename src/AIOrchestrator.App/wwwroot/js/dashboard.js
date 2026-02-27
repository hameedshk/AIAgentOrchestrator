/**
 * Dashboard client-side functionality.
 * Handles API calls, real-time updates via SignalR, and UI interactions.
 */

// Initialize SignalR connection for real-time updates
let signalRConnection = null;

async function initializeSignalR() {
    try {
        signalRConnection = new signalR.HubConnectionBuilder()
            .withUrl('/orchestrator-hub', {
                accessTokenFactory: () => localStorage.getItem('token')
            })
            .withAutomaticReconnect()
            .build();

        signalRConnection.on('TaskStateChanged', (taskId, newState) => {
            console.log(`Task ${taskId} state changed to ${newState}`);
            // Update UI
            updateTaskInUI(taskId, newState);
        });

        signalRConnection.on('StepCompleted', (taskId, stepIndex, success) => {
            console.log(`Step ${stepIndex} completed for task ${taskId}. Success: ${success}`);
            showAlert(success ? 'success' : 'warning', `Step ${stepIndex} completed for task ${taskId}`);
        });

        signalRConnection.on('ResourceAlert', (message, severity) => {
            console.log(`Resource alert (${severity}): ${message}`);
            showAlert(severity || 'warning', message);
        });

        signalRConnection.on('PlanReady', (taskId, planJson) => {
            console.log(`Plan ready for task ${taskId}`);
            showAlert('info', `Plan is ready for task ${taskId}. Click to review.`);
        });

        signalRConnection.on('ReplanTriggered', (taskId) => {
            console.log(`Re-planning triggered for task ${taskId}`);
            showAlert('warning', `Re-planning has been triggered for task ${taskId}`);
        });

        signalRConnection.on('TaskCompleted', (taskId, success) => {
            console.log(`Task ${taskId} completed. Success: ${success}`);
            showAlert(success ? 'success' : 'error', `Task ${taskId} has been completed`);
            updateTaskInUI(taskId, success ? 'Completed' : 'Failed');
        });

        await signalRConnection.start();
        console.log('SignalR connected');
    } catch (error) {
        console.error('SignalR connection failed:', error);
    }
}

function showAlert(type, message) {
    const alertElement = document.createElement('div');
    alertElement.className = `alert alert-${type}`;
    alertElement.textContent = message;

    const contentArea = document.querySelector('.content-area');
    if (contentArea) {
        contentArea.insertBefore(alertElement, contentArea.firstChild);
        setTimeout(() => alertElement.remove(), 5000);
    }
}

function updateTaskInUI(taskId, newState) {
    const rows = document.querySelectorAll('#tasks-table tr');
    rows.forEach(row => {
        const idCell = row.querySelector('td:first-child');
        if (idCell && idCell.textContent.includes(taskId)) {
            const statusCell = row.querySelector('td:nth-child(4)');
            if (statusCell) {
                statusCell.innerHTML = `<span class="status-badge status-${newState.toLowerCase()}">${newState}</span>`;
            }
        }
    });
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', () => {
    initializeSignalR();
});
