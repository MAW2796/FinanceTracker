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

        // 🔥 INDEX
        public async Task<IActionResult> Index(string month)
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            await GenerateInstallmentPlannings(uid.Value);

            var now = DateTime.Now;

            DateTime selectedMonth;
            if (!string.IsNullOrEmpty(month) && DateTime.TryParse(month, out var parsedMonth))
            {
                selectedMonth = new DateTime(parsedMonth.Year, parsedMonth.Month, 1);
            }
            else
            {
                selectedMonth = new DateTime(now.Year, now.Month, 1);
                month = selectedMonth.ToString("yyyy-MM");
            }

            bool isCurrentMonth = selectedMonth.Year == now.Year && selectedMonth.Month == now.Month;

            var data = await _context.MonthlyPlannings
                .Include(x => x.Category)
                .Where(x => x.UserId == uid &&
                    (
                        // planning di bulan yang dipilih
                        (x.DueDate.Month == selectedMonth.Month && x.DueDate.Year == selectedMonth.Year)

                        // kalau yang dipilih bulan sekarang, ikut tampilkan overdue dari bulan-bulan sebelumnya
                        || (isCurrentMonth && !x.IsPaid && x.DueDate < new DateTime(now.Year, now.Month, 1))
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

            ViewBag.Month = month;
            ViewBag.SelectedMonthTotal = data.Sum(x => x.Amount);

            // Proyeksi cicilan yang BELUM di-generate (di luar window 15 hari),
            // khusus untuk bulan yang dipilih -> cuma buat itung-itungan, read-only
            var activeInstallments = await _context.Installments
                .Include(i => i.Category)
                .Where(i => i.UserId == uid && i.IsActive)
                .ToListAsync();

            var projectedInstallments = new List<dynamic>();

            foreach (var inst in activeInstallments)
            {
                var generatedCount = await _context.MonthlyPlannings
                    .CountAsync(p => p.InstallmentId == inst.Id);

                for (int n = generatedCount; n < inst.TenorMonths; n++)
                {
                    var dueDate = inst.StartDate.AddMonths(n);

                    if (dueDate.Year == selectedMonth.Year && dueDate.Month == selectedMonth.Month)
                    {
                        projectedInstallments.Add(new
                        {
                            Title = $"{inst.Name} ({n + 1}/{inst.TenorMonths})",
                            Amount = inst.MonthlyAmount,
                            CategoryName = inst.Category?.Name,
                            DueDate = dueDate
                        });
                    }
                }
            }

            ViewBag.ProjectedInstallments = projectedInstallments;
            ViewBag.ProjectedTotal = projectedInstallments.Sum(x => (decimal)x.Amount);

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

        private async Task GenerateInstallmentPlannings(int userId)
        {
            var today = DateTime.Now.Date;

            var activeInstallments = await _context.Installments
                .Where(i => i.UserId == userId && i.IsActive)
                .ToListAsync();

            foreach (var inst in activeInstallments)
            {
                var generatedCount = await _context.MonthlyPlannings
                    .CountAsync(p => p.InstallmentId == inst.Id);

                // catch-up: generate semua periode yang sudah mendekati/lewat jatuh tempo
                while (generatedCount < inst.TenorMonths)
                {
                    var nextDueDate = inst.StartDate.AddMonths(generatedCount);

                    if (nextDueDate > today.AddDays(15))
                        break;

                    var planning = new MonthlyPlanning
                    {
                        Title = $"{inst.Name} ({generatedCount + 1}/{inst.TenorMonths})",
                        Amount = inst.MonthlyAmount,
                        DueDate = nextDueDate,
                        CategoryId = inst.CategoryId,
                        UserId = inst.UserId,
                        InstallmentId = inst.Id
                    };

                    _context.MonthlyPlannings.Add(planning);
                    generatedCount++;
                }

                if (generatedCount >= inst.TenorMonths)
                {
                    inst.IsActive = false;
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}