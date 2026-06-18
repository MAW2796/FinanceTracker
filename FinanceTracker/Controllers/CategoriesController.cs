using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinanceTracker.Data;
using FinanceTracker.Models;

namespace FinanceTracker.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly AppDbContext _context;

        public CategoriesController(AppDbContext context)
        {
            _context = context;
        }

        // 🔐 HELPER (AMBIL USER ID)
        private int? GetUserId()
        {
            var userId = HttpContext.Session.GetString("UserId");
            return userId != null ? int.Parse(userId) : null;
        }

        public async Task<IActionResult> Index(string search, string type, int page = 1)
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            int pageSize = 10;

            var query = _context.Categories
                .Where(c => c.UserId == uid)
                .AsQueryable();

            // 🔍 SEARCH
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => c.Name.Contains(search));
            }

            // 🎯 FILTER TYPE
            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(c => c.Type == type);
            }

            var totalItems = await query.CountAsync();

            var categories = await query
                .OrderBy(c => c.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            ViewBag.Search = search;
            ViewBag.Type = type;

            return View(categories);
        }

        // 🔥 DETAILS
        public async Task<IActionResult> Details(int? id)
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == uid);

            if (category == null)
                return NotFound();

            return View(category);
        }

        // 🔥 CREATE (GET)
        public IActionResult Create()
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            return View();
        }

        // 🔥 CREATE (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            category.UserId = uid.Value;

            if (ModelState.IsValid)
            {
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(category);
        }

        // 🔥 EDIT (GET)
        public async Task<IActionResult> Edit(int? id)
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == uid);

            if (category == null)
                return NotFound();

            return View(category);
        }

        // 🔥 EDIT (POST) — AMAN
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category model)
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == uid);

            if (category == null)
                return NotFound();

            if (ModelState.IsValid)
            {
                category.Name = model.Name;
                category.Type = model.Type;

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // 🔥 DELETE (GET)
        public async Task<IActionResult> Delete(int? id)
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == uid);

            if (category == null)
                return NotFound();

            return View(category);
        }

        // 🔥 DELETE (POST)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == uid);

            if (category != null)
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
