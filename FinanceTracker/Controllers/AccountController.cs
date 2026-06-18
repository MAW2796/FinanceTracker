using FinanceTracker.Data;
using FinanceTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace FinanceTracker.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
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
                user.Role = "User";
                _context.Users.Add(user);
                _context.SaveChanges();

                return RedirectToAction("Login");
            }

            return View(user);
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            var user = _context.Users.FirstOrDefault(x => x.Email == email);

            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                HttpContext.Session.SetString("UserId", user.Id.ToString());
                HttpContext.Session.SetString("UserName", user.Name);
                HttpContext.Session.SetString("UserRole", user.Role);

                if (user.Role == "Admin")
                    return RedirectToAction("Index", "Home"); 
                else
                    return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Email atau password salah";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        public IActionResult ChangePassword()
        {
            if (HttpContext.Session.GetString("UserId") == null)
                return RedirectToAction("Login");

            return View();
        }

        [HttpPost]
        public IActionResult ChangePassword(string currentPassword, string newPassword)
        {
            var userId = HttpContext.Session.GetString("UserId");

            var user = _context.Users.Find(int.Parse(userId));

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.Password))
            {
                ViewBag.Error = "Password lama salah";
                return View();
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);

            _context.SaveChanges();

            ViewBag.Success = "Password berhasil diubah";

            return View();
        }
    }
}