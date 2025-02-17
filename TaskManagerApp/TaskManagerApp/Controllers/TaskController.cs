using Microsoft.AspNetCore.Mvc;
using TaskManagerApp.Models;

namespace TaskManagerApp.Controllers
{
    public class TaskController : Controller
    {
        private readonly TaskManager _taskManager;

        public TaskController(TaskManager taskManager)
        {
            _taskManager = taskManager;
        }

        public IActionResult Index()
        {
            return View(); // Load the main page
        }
        [HttpPost]
        public IActionResult ClockIn(string userName, string taskName)
        {
           var user = _taskManager.SetCurrentUser(userName);

            if (user == null)
            {
                return Json(new { message = $"User '{userName}' not found." }); // Handle case where user is not found
            }
        
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(taskName))
            {
                return Json(new { message = "Please provide valid input." }); // Return an error message as JSON
            }

            _taskManager.ClockIn(taskName, userName); 
            return Json(new { message = $"{userName} clocked in for task '{taskName}' successfully." }); // Return success message as JSON
        }
        [HttpPost]
        public IActionResult ClockOut(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                return Json(new { message = "Please provide a valid user name." }); // Return error as JSON
            }

            _taskManager.ClockOut(userName); // Process the clock out action
            return Json(new { message = $"{userName} clocked out successfully." }); // Return success message as JSON
        }

        [HttpGet]
        public IActionResult GenerateReport(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                return Json(new { message = "Please provide a valid user name." }); // Return error as JSON
            }

            var reportData = _taskManager.GenerateReport(userName); // Get report data

            // Check if the report data is null or empty
            if (reportData == null || reportData.Count == 0)
            {
                return Json(new { message = "No report data available for the specified user." });
            }

            // Return success with the report data
            return Json(new { message = "Report generated successfully.", data = reportData });
        }
        /*   [HttpPost]
         public IActionResult SetCurrentUser(string userName)
          {
              var user = _taskManager.SetCurrentUser(userName);
              if (user == null)
              {
                  return Json(new { message = $"User '{userName}' not found." });
              }
              return Json(new { message = $"User '{user.Name}' is now active." });
          }
        */
    }
}
