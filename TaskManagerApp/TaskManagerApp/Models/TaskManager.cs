using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Collections.Concurrent;
using System.Text;
using TaskManagerApp.Data;
using TaskManagerApp.Models;

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
            if (_context.Users.Any(u => u.userName.ToLower() == userName.ToLower()))
            {
                Log.Warning($"User '{userName}' already exists. Cannot add.");
                return; // Exit if the user already exists
            }

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
                if (!string.IsNullOrWhiteSpace(user.CurrentTask))
                {
                    Log.Warning($"User '{userName}' is already clocked in for task '{user.CurrentTask}'.");
                    return;
                }

                var taskTime = new TaskTime
                {
                    TaskName = taskName,
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.MaxValue, // Explicitly set this when creating
                    UserId = user.Id
                };

                user.AddTaskTime(taskName, taskTime); // Add the task time to the user
                user.CurrentTask = taskName; // Set the current task to the provided taskName
                Log.Debug($"Set CurrentTask for '{user.userName}' to '{user.CurrentTask}'.");

                _context.TaskTimes.Add(taskTime);

                try
                {
                    _context.SaveChanges(); // Commit to the database
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
            Log.Debug($"Attempting to clock out user '{userName}'.");

            if (activeUsers.TryGetValue(userName, out var user)) // Check if user is active
            {
                Log.Debug("User found in active users for clocking out.");
                Log.Debug($"CurrentTask for '{user.userName}': {user.CurrentTask}");

                // Log all task times before checking for the current task time
                var taskTimes = _context.TaskTimes.Where(t => t.UserId == user.Id).ToList();
                foreach (var tt in taskTimes)
                {
                    Log.Debug($"TaskTime - Task: {tt.TaskName}, StartTime: {tt.StartTime}, EndTime: {tt.EndTime}");
                }

                // Retrieve current task time
                var taskTime = _context.TaskTimes
    .Where(t => t.UserId == user.Id && t.TaskName == user.CurrentTask) // Skip EndTime for debug purposes
    .OrderByDescending(t => t.StartTime)
    .FirstOrDefault();

                if (taskTime != null)
                {
                    Log.Debug($"Current task found for '{user.userName}': {taskTime.TaskName}, starting at {taskTime.StartTime}");

                    taskTime.EndTime = DateTime.UtcNow; // Set EndTime
                    user.CurrentTask = null; // Clear the current task
                    _context.TaskTimes.Update(taskTime); // Update task time in the context
                    _context.Users.Update(user); // Mark user for update

                    // Save changes within a transaction
                    using (var transaction = _context.Database.BeginTransaction())
                    {
                        try
                        {
                            _context.SaveChanges(); // Commit changes to the database
                            transaction.Commit();
                            Log.Information($"{user.userName} clocked out successfully from task '{taskTime.TaskName}'.");
                            activeUsers.TryRemove(userName, out _); // Remove user from active users list
                        }
                        catch (DbUpdateException ex)
                        {
                            transaction.Rollback(); // Rollback on error
                            Log.Error($"Error saving clock-out information for user '{userName}': {ex.Message}");
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback(); // Rollback on unexpected error
                            Log.Error($"An unexpected error occurred during clock-out for user '{userName}': {ex.Message}");
                        }
                    }
                }
                else
                {
                    Log.Warning($"No active task time found for user '{user.userName}'.");
                }
            }
            else
            {
                Log.Warning($"User '{userName}' is not in active users.");
            }
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

                // Ensure that TaskTimes is initialized and available
                foreach (var taskTime in user.TaskTimes ?? Enumerable.Empty<TaskTime>())
                {
                    // Check if EndTime is set (if using nullable)
                    if (taskTime.EndTime.HasValue) // If EndTime is nullable
                    {
                        var totalTimeSpent = taskTime.EndTime.Value - taskTime.StartTime; // Calculate total time for this task
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
            return null; // Return null if the user is not found
        }
    }
}