using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Collections.Concurrent;
using System.Text;
using TaskManagerApp.Data;

namespace TaskManagerApp.Models
{
    public class TaskManager
    {
        private readonly ApplicationDbContext _context;
        private readonly ConcurrentDictionary<string, User> activeUsers = new ConcurrentDictionary<string, User>();

        public TaskManager(ApplicationDbContext context)
        {
            _context = context;
        }

        public void AddUser(string userName)
        {
            var user = new User(userName);
            _context.Users.Add(user);
            try
            {
                _context.SaveChanges();
                Log.Information($"User '{userName}' added successfully.");
            }
            catch (DbUpdateException ex)
            {
                Log.Error($"Error adding user '{userName}': {ex.Message}");
            }
            catch (Exception ex)
            {
                Log.Error($"An unexpected error occurred while adding user '{userName}': {ex.Message}");
            }
        }

        public User SetCurrentUser(string userName)
        {
            Log.Debug($"Searching for user '{userName}'...");
            userName = userName?.Trim();

            var user = _context.Users.FirstOrDefault(u => u.userName.ToLower() == userName.ToLower());

            if (user != null)
            {
                Log.Debug($"Found user {user.userName}. Checking active users...");
                if (!activeUsers.ContainsKey(userName))
                {
                    if (activeUsers.TryAdd(userName, user))
                    {
                        Log.Debug($"User '{userName}' added to active users.");
                        LogActiveUsers();
                    }
                    else
                    {
                        Log.Warning($"Failed to add user '{userName}' to active users.");
                    }
                }
                else
                {
                    Log.Debug($"User '{userName}' is already active.");
                }
            }
            else
            {
                Log.Warning($"User '{userName}' NOT found in database!");
            }

            return user; // Return user or null if not found
        }

        public void ClockIn(string taskName, string userName)
        {
            Log.Debug($"Attempting to clock in user '{userName}' with task '{taskName}'.");

            if (activeUsers.TryGetValue(userName, out var user))
            {
                Log.Debug("User found in active users.");
                if (!string.IsNullOrWhiteSpace(user.CurrentTask))
                {
                    Log.Warning($"User '{userName}' is already clocked in for task '{user.CurrentTask}'.");
                    return;
                }

                var taskTime = new TaskTime
                {
                    TaskName = taskName,
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.MinValue,
                    UserId = user.Id
                };

                user.AddTaskTime(taskName, taskTime);
                _context.TaskTimes.Add(taskTime);

                try
                {
                    _context.SaveChanges(); // Attempt to save changes to the database
                    Log.Information($"{user.userName} clocked in for task '{taskName}'.");
                }
                catch (DbUpdateException ex)
                {
                    Log.Error($"Error saving clock-in information for user '{userName}': {ex.Message}");
                }
                catch (Exception ex)
                {
                    Log.Error($"An unexpected error occurred during clock-in for user '{userName}': {ex.Message}");
                }
            }
            else
            {
                Log.Warning($"User '{userName}' is not active. Cannot clock in.");
            }
        }

        public void ClockOut(string userName)
        {
            Log.Debug($"Debug: Attempting to clock out user '{userName}'.");

            if (activeUsers.TryGetValue(userName, out var user)) // Check active user
            {
                Log.Debug("Debug: User found in active users for clocking out.");

                var taskTime = _context.TaskTimes
                    .Where(t => t.UserId == user.Id && t.TaskName == user.CurrentTask && t.EndTime == DateTime.MinValue)
                    .OrderByDescending(t => t.StartTime)
                    .FirstOrDefault();

                if (taskTime != null)
                {
                    taskTime.EndTime = DateTime.UtcNow; // Set EndTime 
                    _context.TaskTimes.Update(taskTime);

                    try
                    {
                        _context.SaveChanges(); // Commit to the database
                        Log.Information($"{user.userName} clocked out successfully from task '{user.CurrentTask}'.");
                        user.CurrentTask = null; // Clear the current task
                    }
                    catch (DbUpdateException ex)
                    {
                        // Handle database update exceptions
                        Log.Error($"Error saving clock-out information for user '{userName}': {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        // Handle any other exceptions that may occur
                        Log.Error($"An unexpected error occurred during clock-out for user '{userName}': {ex.Message}");
                    }
                }
                else
                {
                    Log.Debug($"No active task time found for user '{user.userName}'.");
                }
            }
            else
            {
                Log.Warning($"User '{userName}' is not in active users.");
            }

            // Log current active users after clocking out
            Log.Debug("Active Users After ClockOut:");
            LogActiveUsers(); // Log active users after the clock-out attempt
        }

        // Helper method to log active users
        private void LogActiveUsers()
        {
            Log.Debug($"Active Users Count: {activeUsers.Count}");
            foreach (var activeUser in activeUsers)
            {
                Log.Debug($"Active User: {activeUser.Key}"); // Log each active user
            }
        }

        // New method to check if a user is active
        public bool IsUserActive(string userName)
        {
            return activeUsers.ContainsKey(userName); // Returns true if user is active
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
                Log.Information($"Report generated for {userName}:");
                foreach (var task in report)
                {
                    Log.Information($"Task: {task.Key}, Time Spent: {task.Value}");
                }

                return report; // Return the report data
            }

            Log.Warning($"User '{userName}' not found while generating report.");
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
                Log.Information($"Report generated and saved to {filePath}");
            }
            catch (Exception ex)
            {
                Log.Error($"Error saving the file: {ex.Message}");
            }
        }
    }
}