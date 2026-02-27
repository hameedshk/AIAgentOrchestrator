using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace AIOrchestrator.App.Controllers
{
    /// <summary>
    /// Serves the web dashboard UI.
    /// All dashboard views require authentication (Bearer token).
    /// </summary>
    [Authorize]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Tasks()
        {
            return View();
        }

        public IActionResult AuditLog()
        {
            return View();
        }

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
