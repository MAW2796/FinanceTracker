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
    public class TransactionsController : BaseController
    {
        private readonly AppDbContext _context;

        public TransactionsController(AppDbContext context)
        {
            _context = context;
        }

        private SelectList GetGroupedCategorySelectList(int userId, int? selectedId = null)
        {
            var categories = _context.Categories
                .Where(c => c.UserId == userId)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    GroupName = c.Type == "Income" ? "Pemasukan" : "Pengeluaran"
                })
                .OrderBy(c => c.GroupName)
                .ThenBy(c => c.Name)
                .ToList();

            return new SelectList(categories, "Id", "Name", selectedId, "GroupName");
        }

        public async Task<IActionResult> Index(string search, string month, int page = 1)
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            int pageSize = 10;

            var query = _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.UserId == uid)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(t => t.Description.Contains(search));
            }

            DateTime selectedMonth;

            if (!string.IsNullOrEmpty(month) && DateTime.TryParse(month, out var parsedMonth))
            {
                selectedMonth = parsedMonth;
            }
            else
            {
                selectedMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                month = selectedMonth.ToString("yyyy-MM");
            }

            query = query.Where(t =>
                t.Date.Month == selectedMonth.Month &&
                t.Date.Year == selectedMonth.Year);

            var totalItems = await query.CountAsync();

            var transactions = await query
                .OrderByDescending(t => t.Date)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var allData = await query.ToListAsync();

            ViewBag.TotalIncome = allData
                .Where(t => t.Category.Type == "Income")
                .Sum(t => t.Amount);

            ViewBag.TotalExpense = allData
                .Where(t => t.Category.Type == "Expense")
                .Sum(t => t.Amount);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.Search = search;
            ViewBag.Month = month;

            return View(transactions);
        }

        public async Task<IActionResult> Details(int? id)
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var transaction = await _context.Transactions
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == uid);

            if (transaction == null)
                return NotFound();

            return View(transaction);
        }

        public IActionResult Create()
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            ViewData["CategoryId"] = GetGroupedCategorySelectList(uid.Value);

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Transaction transaction)
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            transaction.UserId = uid.Value;

            if (ModelState.IsValid)
            {
                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["CategoryId"] = GetGroupedCategorySelectList(uid.Value, transaction.CategoryId);

            return View(transaction);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == uid);

            if (transaction == null)
                return NotFound();

            ViewData["CategoryId"] = new SelectList(
                _context.Categories.Where(c => c.UserId == uid),
                "Id",
                "Name",
                transaction.CategoryId
            );

            return View(transaction);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Transaction transaction)
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            if (id != transaction.Id)
                return NotFound();

            var existingTransaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == uid);

            if (existingTransaction == null)
                return NotFound();

            if (ModelState.IsValid)
            {
                existingTransaction.Date = transaction.Date;
                existingTransaction.Amount = transaction.Amount;
                existingTransaction.CategoryId = transaction.CategoryId;
                existingTransaction.Description = transaction.Description;

                // Kalau transaction ini berasal dari planning, sync juga
                if (existingTransaction.MonthlyPlanningId != null)
                {
                    var planning = await _context.MonthlyPlannings
                        .FirstOrDefaultAsync(p => p.Id == existingTransaction.MonthlyPlanningId && p.UserId == uid);

                    if (planning != null)
                    {
                        planning.Title = transaction.Description;
                        planning.Amount = transaction.Amount;
                        planning.CategoryId = transaction.CategoryId;
                        planning.PaidDate = transaction.Date;
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["CategoryId"] = new SelectList(
                _context.Categories.Where(c => c.UserId == uid),
                "Id",
                "Name",
                transaction.CategoryId
            );

            return View(transaction);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var transaction = await _context.Transactions
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == uid);

            if (transaction == null)
                return NotFound();

            return View(transaction);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == uid);

            if (transaction != null)
            {
                MonthlyPlanning? planning = null;

                // Prioritas 1: cari dari FK langsung
                if (transaction.MonthlyPlanningId != null)
                {
                    planning = await _context.MonthlyPlannings
                        .FirstOrDefaultAsync(p => p.Id == transaction.MonthlyPlanningId && p.UserId == uid);
                }

                // Fallback untuk data lama (sebelum ada MonthlyPlanningId)
                if (planning == null)
                {
                    planning = await _context.MonthlyPlannings
                        .FirstOrDefaultAsync(p =>
                            p.UserId == uid &&
                            p.CategoryId == transaction.CategoryId &&
                            p.Amount == transaction.Amount &&
                            p.DueDate.Month == transaction.Date.Month &&
                            p.DueDate.Year == transaction.Date.Year &&
                            p.IsPaid == true);
                }

                // 🔥 Balikin planning jadi unpaid
                if (planning != null)
                {
                    planning.IsPaid = false;
                    planning.PaidDate = null;
                }

                _context.Transactions.Remove(transaction);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Dashboard(string month)
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            DateTime selectedMonth;

            if (!string.IsNullOrEmpty(month) && DateTime.TryParse(month, out var parsedMonth))
            {
                selectedMonth = parsedMonth;
            }
            else
            {
                selectedMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                month = selectedMonth.ToString("yyyy-MM");
            }

            var monthlyTransactions = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.UserId == uid &&
                            t.Date.Month == selectedMonth.Month &&
                            t.Date.Year == selectedMonth.Year)
                .ToListAsync();

            ViewBag.TotalIncome = monthlyTransactions
                .Where(t => t.Category.Type == "Income")
                .Sum(t => t.Amount);

            ViewBag.TotalExpense = monthlyTransactions
                .Where(t => t.Category.Type == "Expense")
                .Sum(t => t.Amount);

            ViewBag.Balance = ViewBag.TotalIncome - ViewBag.TotalExpense;

            var expenseByCategory = monthlyTransactions
                .Where(t => t.Category.Type == "Expense")
                .GroupBy(t => t.Category.Name)
                .Select(g => new {
                    Category = g.Key,
                    Total = g.Sum(t => t.Amount)
                })
                .ToList();

            ViewBag.ExpenseCategoryLabels = expenseByCategory.Select(x => x.Category).ToList();
            ViewBag.ExpenseCategoryValues = expenseByCategory.Select(x => x.Total).ToList();

            var barLabels = new List<string>();
            var barIncomeData = new List<decimal>();
            var barExpenseData = new List<decimal>();

            for (int i = 2; i >= 0; i--)
            {
                var targetMonth = selectedMonth.AddMonths(-i);

                var monthData = await _context.Transactions
                    .Include(t => t.Category)
                    .Where(t => t.UserId == uid &&
                                t.Date.Month == targetMonth.Month &&
                                t.Date.Year == targetMonth.Year)
                    .ToListAsync();

                barLabels.Add(targetMonth.ToString("MMM"));
                barIncomeData.Add(monthData.Where(t => t.Category.Type == "Income").Sum(t => t.Amount));
                barExpenseData.Add(monthData.Where(t => t.Category.Type == "Expense").Sum(t => t.Amount));
            }

            ViewBag.BarLabels = barLabels;
            ViewBag.BarIncomeData = barIncomeData;
            ViewBag.BarExpenseData = barExpenseData;

            ViewBag.Month = month;

            return View(monthlyTransactions);
        }
    }
}