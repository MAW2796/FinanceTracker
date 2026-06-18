using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FinanceTracker.Data;
using FinanceTracker.Models;

namespace FinanceTracker.Controllers
{
    public class MonthlyPlanningsController : BaseController
    {
        private readonly AppDbContext _context;

        public MonthlyPlanningsController(AppDbContext context)
        {
            _context = context;
        }

        private int? GetUserId()
        {
            var userId = HttpContext.Session.GetString("UserId");
            return userId != null ? int.Parse(userId) : null;
        }

        // 🔥 INDEX
        public async Task<IActionResult> Index()
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            var now = DateTime.Now;

            var data = await _context.MonthlyPlannings
                .Include(x => x.Category)
                .Where(x => x.UserId == uid &&
                    (
                        // planning bulan ini
                        (x.DueDate.Month == now.Month && x.DueDate.Year == now.Year)

                        // atau overdue dari bulan sebelumnya yang belum dibayar
                        || (!x.IsPaid && x.DueDate < new DateTime(now.Year, now.Month, 1))
                    ))
                .ToListAsync();

            ViewBag.Upcoming = data
                .Where(x => !x.IsPaid && x.DueDate >= now)
                .OrderBy(x => x.DueDate)
                .ToList();

            ViewBag.Overdue = data
                .Where(x => !x.IsPaid && x.DueDate < now)
                .OrderBy(x => x.DueDate)
                .ToList();

            ViewBag.Paid = data
                .Where(x => x.IsPaid)
                .OrderByDescending(x => x.PaidDate)
                .ToList();

            return View();
        }

        // 🔥 CREATE GET
        public IActionResult Create()
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            ViewData["CategoryId"] = new SelectList(
                _context.Categories.Where(c => c.UserId == uid && c.Type == "Expense"),
                "Id",
                "Name"
            );

            return View();
        }

        // 🔥 CREATE POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MonthlyPlanning model)
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            model.UserId = uid.Value;

            if (ModelState.IsValid)
            {
                _context.MonthlyPlannings.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["CategoryId"] = new SelectList(
                _context.Categories.Where(c => c.UserId == uid && c.Type == "Expense"),
                "Id",
                "Name",
                model.CategoryId
            );

            return View(model);
        }

        // 🔥 MARK AS PAID + AUTO MASUK TRANSACTION
        public async Task<IActionResult> MarkAsPaid(int id)
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            var item = await _context.MonthlyPlannings
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid);

            if (item == null)
                return NotFound();

            if (!item.IsPaid)
            {
                item.IsPaid = true;
                item.PaidDate = DateTime.Now;

                var transaction = new Transaction
                {
                    Date = DateTime.Now,
                    Amount = item.Amount,
                    CategoryId = item.CategoryId,
                    Description = item.Title,
                    UserId = item.UserId,
                    MonthlyPlanningId = item.Id // 🔥 RELASI WAJIB
                };

                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }



        // 🔥 DELETE PLANNING
        public async Task<IActionResult> Delete(int? id)
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var planning = await _context.MonthlyPlannings
                .Include(p => p.Category)
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid);

            if (planning == null)
                return NotFound();

            return View(planning);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            var item = await _context.MonthlyPlannings
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid);

            if (item == null)
                return NotFound();

            // Kalau ada transaction terkait planning ini, ikut hapus
            var relatedTransactions = await _context.Transactions
                .Where(t => t.MonthlyPlanningId == item.Id && t.UserId == uid)
                .ToListAsync();

            if (relatedTransactions.Any())
            {
                _context.Transactions.RemoveRange(relatedTransactions);
            }

            _context.MonthlyPlannings.Remove(item);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // 🔥 EDIT GET
        public async Task<IActionResult> Edit(int id)
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            var item = await _context.MonthlyPlannings
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid);

            if (item == null)
                return NotFound();

            ViewData["CategoryId"] = new SelectList(
                _context.Categories.Where(c => c.UserId == uid && c.Type == "Expense"),
                "Id",
                "Name",
                item.CategoryId
            );

            return View(item);
        }

        // 🔥 EDIT POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MonthlyPlanning model)
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            var item = await _context.MonthlyPlannings
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid);

            if (item == null)
                return NotFound();

            if (ModelState.IsValid)
            {
                item.Title = model.Title;
                item.Amount = model.Amount;
                item.DueDate = model.DueDate;
                item.CategoryId = model.CategoryId;
                item.IsRecurring = model.IsRecurring;
                item.RecurringDay = model.RecurringDay;

                // Kalau planning ini sudah dibayar, update transaksi terkait
                var relatedTransactions = await _context.Transactions
                    .Where(t => t.MonthlyPlanningId == item.Id && t.UserId == uid)
                    .ToListAsync();

                foreach (var trx in relatedTransactions)
                {
                    trx.Amount = model.Amount;
                    trx.CategoryId = model.CategoryId;
                    trx.Description = model.Title;
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["CategoryId"] = new SelectList(
                _context.Categories.Where(c => c.UserId == uid && c.Type == "Expense"),
                "Id",
                "Name",
                model.CategoryId
            );

            return View(model);
        }
    }
}