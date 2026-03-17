using Microsoft.AspNetCore.Mvc;

namespace AIOrchestrator.App.Controllers
{
    /// <summary>
    /// Serves the web dashboard UI.
    /// API calls from dashboard require Bearer token authentication.
    /// Page routes are open so users can load the UI and set token in localStorage.
    /// </summary>
    public class DashboardController : Controller
    {
        [HttpGet("/dashboard")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("/dashboard/tasks")]
        public IActionResult Tasks()
        {
            return View();
        }

        [HttpGet("/dashboard/auditlog")]
        public IActionResult AuditLog()
        {
            return View();
        }

        [HttpGet("/dashboard/settings")]
        public IActionResult Settings()
        {
            return View();
        }

        [HttpGet("/dashboard/tasks/{id}")]
        public IActionResult TaskDetail(string id)
        {
            ViewData["TaskId"] = id;
            ViewData["Title"] = $"Task {id}";
            return View("TaskDetail");
        }
    }
}
