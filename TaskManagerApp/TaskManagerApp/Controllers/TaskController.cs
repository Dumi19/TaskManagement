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
            Console.WriteLine($"Debug: ClockIn called with Username: {userName}, TaskName: {taskName}");

            var user = GetActiveUser(userName);
            if (user == null) return Json(new { message = $"User '{userName}' not found." });

            if (user == null)
            {
                Console.WriteLine($"Debug: User '{userName}' not found.");
                return Json(new { message = $"User '{userName}' not found." }); // Handle case where user is not found
            }

            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(taskName))
            {
                Console.WriteLine("Debug: Invalid input detected.");
                return Json(new { message = "Please provide valid input." }); // Return an error message as JSON
            }

            _taskManager.ClockIn(taskName, userName);
            Console.WriteLine($"Debug: {userName} clocked in for task '{taskName}' successfully.");
            return Json(new { message = $"{userName} clocked in for task '{taskName}' successfully." }); // Return success message as JSON
        }

        [HttpPost]
        public IActionResult ClockOut(string userName)
        {
            Console.WriteLine($"Debug: ClockOut called with Username: {userName}");
            var user = GetActiveUser(userName);
            if (user == null) return Json(new { message = $"User '{userName}' not found." });

            if (string.IsNullOrWhiteSpace(userName))
            {
                Console.WriteLine("Debug: Valid user name is required for clocking out.");
                return Json(new { message = "Please provide a valid user name." }); // Return error as JSON
            }

            _taskManager.ClockOut(userName); // Process the clock out action
            Console.WriteLine($"Debug: Attempting to clock out {userName}");

            // Check if the user is still active after the clock out attempt
            var userStillActive = _taskManager.IsUserActive(userName); // Implement IsUserActive in TaskManager

            if (!userStillActive)
            {
                Console.WriteLine($"{userName} clocked out successfully.");
            }
            else
            {
                Console.WriteLine($"Warning: {userName} is still marked as active after clock out attempt.");
            }

            return Json(new { message = $"{userName} clocked out successfully." }); // Return success message as JSON
        }
        [HttpGet]
        public IActionResult GenerateReport(string userName)
        {
            // Using a logger instead of Console.WriteLine for production code
            Console.WriteLine($"GenerateReport called with Username: {userName}");

            if (string.IsNullOrWhiteSpace(userName))
            {
                Console.WriteLine("User name must be provided to generate a report.");
                return Json(new { message = "Please provide a valid user name." }); // Return error as JSON
            }

            var reportData = _taskManager.GenerateReport(userName); // Get report data
            Console.WriteLine($"Requesting report for user: {userName}");

            // Check if the report data is null or empty
            if (reportData == null || reportData.Count == 0)
            {
                Console.WriteLine($"No report data available for user: {userName}");
                return Json(new { message = "No report data available for the specified user." });
            }

            // Return success with the report data
            Console.WriteLine($"Report generated successfully for user: {userName}.");
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
        private User GetActiveUser(string userName)
        {
            var user = _taskManager.SetCurrentUser(userName);
            if (user == null)
            {
                Console.WriteLine($"User '{userName}' not found.");
            }
            return user; // Will be null if not found
        }
    }
}
