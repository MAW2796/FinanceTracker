using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FinanceTracker.Data;
using FinanceTracker.Models;
using System.Linq;

namespace FinanceTracker.Controllers
{
    public class DebtReceivablesController : BaseController
    {
        private readonly AppDbContext _context;

        public DebtReceivablesController(AppDbContext context)
        {
            _context = context;
        }

        // 🔥 INDEX
        public async Task<IActionResult> Index()
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            var today = DateTime.Now.Date;

            var data = await _context.DebtReceivables
                .Include(d => d.Category)
                .Include(d => d.Payments)
                .Where(d => d.UserId == uid)
                .ToListAsync();

            var withRemaining = data.Select(d => new DebtItemViewModel
            {
                Item = d,
                Paid = d.Payments.Sum(p => p.Amount),
                Remaining = d.TotalAmount - d.Payments.Sum(p => p.Amount)
            }).ToList();

            ViewBag.Utang = withRemaining
                .Where(x => x.Item.Type == "Utang")
                .OrderBy(x => x.Item.DueDate)
                .ToList();

            ViewBag.Piutang = withRemaining
                .Where(x => x.Item.Type == "Piutang")
                .OrderBy(x => x.Item.DueDate)
                .ToList();

            ViewBag.Today = today;

            return View();
        }

        // 🔥 CREATE GET
        public IActionResult Create()
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            ViewBag.ExpenseCategories = new SelectList(
                _context.Categories.Where(c => c.UserId == uid && c.Type == "Expense"),
                "Id", "Name"
            );

            ViewBag.IncomeCategories = new SelectList(
                _context.Categories.Where(c => c.UserId == uid && c.Type == "Income"),
                "Id", "Name"
            );

            return View();
        }

        // 🔥 CREATE POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DebtReceivable model)
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            if (model.Type != "Utang" && model.Type != "Piutang")
                ModelState.AddModelError("Type", "Tipe harus Utang atau Piutang.");

            model.UserId = uid.Value;

            if (ModelState.IsValid)
            {
                _context.DebtReceivables.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.ExpenseCategories = new SelectList(
                _context.Categories.Where(c => c.UserId == uid && c.Type == "Expense"),
                "Id", "Name", model.CategoryId
            );

            ViewBag.IncomeCategories = new SelectList(
                _context.Categories.Where(c => c.UserId == uid && c.Type == "Income"),
                "Id", "Name", model.CategoryId
            );

            return View(model);
        }

        // 🔥 ADD PAYMENT (bayar sebagian / lunas)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPayment(int id, string amount)
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            // Bersihkan input dari karakter non-digit (titik, koma, spasi, dll),
            // supaya tidak tergantung culture (id-ID vs en-US) sama sekali.
            var cleanAmount = new string((amount ?? "").Where(char.IsDigit).ToArray());

            if (!decimal.TryParse(cleanAmount, out decimal parsedAmount) || parsedAmount <= 0)
            {
                TempData["Error"] = "Nominal pembayaran tidak valid.";
                return RedirectToAction(nameof(Index));
            }

            var debt = await _context.DebtReceivables
                .Include(d => d.Payments)
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == uid);

            if (debt == null)
                return NotFound();

            var remaining = debt.TotalAmount - debt.Payments.Sum(p => p.Amount);

            if (parsedAmount > remaining)
            {
                TempData["Error"] = "Nominal pembayaran melebihi sisa tagihan.";
                return RedirectToAction(nameof(Index));
            }

            // Utang dibayar = pengeluaran, Piutang diterima = pemasukan
            var transaction = new Transaction
            {
                Date = DateTime.Now,
                Amount = parsedAmount,
                CategoryId = debt.CategoryId,
                Description = $"Pembayaran {debt.Type} - {debt.PersonName}",
                UserId = uid.Value
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync(); // supaya transaction.Id terisi

            var payment = new DebtPayment
            {
                DebtReceivableId = debt.Id,
                Amount = parsedAmount,
                TransactionId = transaction.Id
            };

            _context.DebtPayments.Add(payment);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // 🔥 DELETE GET
        public async Task<IActionResult> Delete(int? id)
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var item = await _context.DebtReceivables
                .Include(d => d.Category)
                .Include(d => d.Payments)
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid);

            if (item == null)
                return NotFound();

            return View(item);
        }

        // 🔥 DELETE POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            var item = await _context.DebtReceivables
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid);

            if (item == null)
                return NotFound();

            // DebtPayments otomatis ikut terhapus (Cascade di database),
            // tapi Transaction historisnya TETAP dibiarkan ada (uang yang sudah
            // dibayar/diterima adalah fakta yang sudah terjadi, tidak boleh hilang)
            _context.DebtReceivables.Remove(item);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}