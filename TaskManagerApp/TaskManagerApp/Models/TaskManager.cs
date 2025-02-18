using System.Text;
using TaskManagerApp.Data;

namespace TaskManagerApp.Models
{
    public class TaskManager
    {
        private readonly ApplicationDbContext _context;
        private readonly Dictionary<string, User> activeUsers = new Dictionary<string, User>();

        public TaskManager(ApplicationDbContext context)
        {
            _context = context;
                
            }
        public void AddUser(string userName)
        {
            var user = new User(userName);
            _context.Users.Add(user);
            _context.SaveChanges();
        }

        public User SetCurrentUser(string userName)
        {
            Console.WriteLine($"Debug: Searching for user '{userName}'...");
            userName = userName?.Trim();

            // Use ToLower() for case-insensitive comparison
            var user = _context.Users.FirstOrDefault(u => u.userName.ToLower() == userName.ToLower());

            if (user != null)
            {
                activeUsers[userName] = user; // Add to active users dictionary
                Console.WriteLine($"Debug: User '{userName}' found and set as active.");
                Console.WriteLine($"Active Users Count: {activeUsers.Count}"); // Log count of active users
                foreach (var activeUser in activeUsers)
                {
                    Console.WriteLine($"Active User: {activeUser.Key}"); // Log each active user
                }
            }
            else
            {
                Console.WriteLine($"Debug: User '{userName}' NOT found!");
            }

            return user; // Return user or null if not found
        }
        public void ClockIn(string taskName, string userName)
        {
            if (activeUsers.TryGetValue(userName, out var user))
            {
                var taskTime = new TaskTime
                {
                    TaskName = taskName,
                    StartTime = DateTime.UtcNow, // Use UTC time
                    EndTime = DateTime.MinValue, // Default as unclocked out
                    UserId = user.Id // Link it to the user
                };

                user.AddTaskTime(taskName, taskTime); // Add task to user's collection
                _context.TaskTimes.Add(taskTime); // Persist the task time
                _context.SaveChanges(); // Save changes to the database

                user.CurrentTask = taskName; // Set current task immediately
                Console.WriteLine($"{user.userName} clocked in for task '{taskName}'.");
            }
            else
            {
                Console.WriteLine($"User '{userName}' is not active. Cannot clock in.");
            }
        }


        public void ClockOut(string userName)
        {
            if (activeUsers.TryGetValue(userName, out var user)) // Check active user
            {
                if (!string.IsNullOrWhiteSpace(user.CurrentTask)) // Check for current task
                {
                    var taskTime = user.TaskTimes.LastOrDefault(t => t.TaskName == user.CurrentTask && t.EndTime == DateTime.MinValue);
                    if (taskTime != null)
                    {
                        taskTime.EndTime = DateTime.UtcNow; // Set EndTime
                        _context.TaskTimes.Update(taskTime); // Mark it for update
                        _context.SaveChanges(); // Commit to the database
                        user.CurrentTask = null; // Clear the current task
                        Console.WriteLine($"{user.userName} clocked out successfully from task '{user.CurrentTask}'.");
                    }
                    else
                    {
                        Console.WriteLine($"No active task time found for '{user.userName}'.");
                    }
                }
                else
                {
                    Console.WriteLine($"User '{userName}' has no current task to clock out from.");
                }
            }
            else
            {
                Console.WriteLine($"User '{userName}' is not in active users.");
            }
        }


        public Dictionary<string, TimeSpan> GenerateReport(string userName)
        {
            if (activeUsers.TryGetValue(userName, out var user))
            {
                var report = new Dictionary<string, TimeSpan>();

                foreach (var taskTime in user.TaskTimes)
                {
                    if (taskTime.EndTime != DateTime.MinValue) // Validate that the end time is set
                    {
                        var totalTimeSpent = taskTime.EndTime - taskTime.StartTime; // Calculate total time for this task
                        if (report.ContainsKey(taskTime.TaskName))
                        {
                            report[taskTime.TaskName] += totalTimeSpent; // Accumulate time spent on existing task
                        }
                        else
                        {
                            report[taskTime.TaskName] = totalTimeSpent; // Add task time to report
                        }
                    }
                }

                // Log report contents for debugging
                Console.WriteLine($"Report for {userName}:");
                foreach (var task in report)
                {
                    Console.WriteLine($"Task: {task.Key}, Time Spent: {task.Value}");
                }

                return report; // Return the report data
            }

            return null; // User not found
        }
        public void GenerateReportForAllUsers(string filePath)
        {
            StringBuilder csvContent = new StringBuilder();
            csvContent.AppendLine("User Name,Task Name,Total Time Spent");

            foreach (var user in activeUsers.Values) // Iterate over active users
            {
                foreach (var taskTime in user.TaskTimes)
                {
                    if (taskTime.EndTime != DateTime.MinValue) // Only include completed tasks
                    {
                        var totalTimeSpent = taskTime.EndTime - taskTime.StartTime; // Calculate time spent
                        csvContent.AppendLine($"{user.userName},{taskTime.TaskName},{totalTimeSpent}");
                    }
                }
            }

            try
            {
                File.WriteAllText(filePath, csvContent.ToString()); // Write CSV content to file
                Console.WriteLine($"Report generated and saved to {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving the file: {ex.Message}");
            }
        }
    }
}
    