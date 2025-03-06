using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagerApp.Data;
using TaskManagerApp.Models;

namespace TaskManagerApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context; // Add ApplicationDbContext
        private readonly TaskManager _taskManager;
        public AccountController(TaskManager taskManager, ApplicationDbContext context)
        {
            _context = context;
            _taskManager = taskManager;
        }
        public IActionResult Administrator()
        {
            return View();
        }
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Login(string userName)
        {
            var user = _taskManager.SetCurrentUser(userName);
            if (user == null)
            {
                ViewBag.Message = "User not found. Please register.";
                return View();
            }
            ViewBag.Message = $"Welcome {user.userName}!";
            return RedirectToAction("Index", "Task");
        }
        public IActionResult Register()
        {
            return View();
        }
   
        
         [HttpPost]
        public IActionResult Register(string userName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userName))
                    throw new ArgumentException("User name cannot be empty.");

                // Check for existing user
                var existingUser = _context.Users.FirstOrDefault(u => u.userName == userName);
                if (existingUser != null)
                {
                    ViewBag.Message = "Username already exists. Please choose a different username.";
                    return View();
                }

                var user = new User(userName);
                _context.Users.Add(user); // Add user to the DbSet
                _context.SaveChanges();    // Save changes to the database

                ViewBag.Message = $"User {userName} has been registered successfully!";
                return RedirectToAction("Login");
            }
            catch (DbUpdateException dbEx)
            {
                // Log detailed exception
                Console.WriteLine(dbEx.InnerException?.Message);
                ViewBag.Message = "An error occurred while saving to the database. Please try again.";
            }
            catch (ArgumentException ex)
            {
                ViewBag.Message = ex.Message; // Provide user feedback for input issues
            }
            catch (Exception ex)
            {
                // Catch any other exceptions
                Console.WriteLine(ex.Message);
                ViewBag.Message = "An unexpected error occurred. Please try again.";
            }

            return View();
        } 
    }
}
