using Microsoft.AspNetCore.Mvc;
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
            if (string.IsNullOrWhiteSpace(userName))
            {
                ViewBag.Message = "User name cannot be empty.";
                return View();
            }
            var user = new User { userName = userName };
            _context.Users.Add(user); // Add user to the DbSet
            _context.SaveChanges(); // Save changes to the database

            ViewBag.Message = $"User {userName} has been registered successfully!";
            return RedirectToAction("Login");
        }
    }
}
