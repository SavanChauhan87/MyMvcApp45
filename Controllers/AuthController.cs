using Microsoft.AspNetCore.Mvc;
using MyMvcApp.Models;
using System.Collections.Generic;
using MyMvcApp.Data;

namespace MyMvcApp.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AuthController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            var user = _db.Users.FirstOrDefault(u => u.Username == username && u.Password == password && u.IsActive);
            
            if (user != null)
            {
                // Update last login
                user.LastLogin = DateTime.Now;
                _db.SaveChanges();
                
                // Set session or authentication cookie
                HttpContext.Session.SetString("UserId", user.Id.ToString());
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("UserRole", user.Role);
                HttpContext.Session.SetString("UserFullName", user.FullName);

                if (user.Role == "Admin")
                {
                    return RedirectToAction("Index", "Admin");
                }
                else
                {
                    return RedirectToAction("Index", "Shop");
                }
            }
            
            ViewBag.ErrorMessage = "Invalid username or password";
            return View();
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(User user)
        {
            if (ModelState.IsValid)
            {
                // Check if username already exists
                if (_db.Users.Any(u => u.Username == user.Username))
                {
                    ViewBag.ErrorMessage = "Username already exists. Please choose a different username.";
                    return View(user);
                }

                // Check if email already exists
                if (_db.Users.Any(u => u.Email == user.Email))
                {
                    ViewBag.ErrorMessage = "Email already exists. Please use a different email.";
                    return View(user);
                }

                // Create new user
                user.Role = "Customer";
                user.IsActive = true;
                user.CreatedAt = DateTime.Now;
                user.LastLogin = DateTime.Now;

                _db.Users.Add(user);
                _db.SaveChanges();

                ViewBag.SuccessMessage = "Registration successful! Please login with your credentials.";
                return RedirectToAction("Login");
            }
            
            return View(user);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
            
        }

        // Helper method to get current user
        public User? GetCurrentUser(int userId)
        {
            return _db.Users.FirstOrDefault(u => u.Id == userId);
        }

        // Helper method to get all users
        public List<User> GetAllUsers()
        {
            return _db.Users.ToList();
        }
    }
}
