using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinanceTracker.Data;
using FinanceTracker.Models;

namespace FinanceTracker.Controllers
{
    public class InvestmentsController : BaseController
    {
        private readonly AppDbContext _context;

        public InvestmentsController(AppDbContext context)
        {
            _context = context;
        }

        // 🔥 INDEX
        public async Task<IActionResult> Index()
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            var data = await _context.Investments
                .Where(i => i.UserId == uid)
                .OrderByDescending(i => i.TotalUnits * i.CurrentPrice)
                .ToListAsync();

            ViewBag.TotalCurrentValue = data.Sum(i => i.TotalUnits * i.CurrentPrice);
            ViewBag.TotalCost = data.Sum(i => i.TotalUnits * i.AvgBuyPrice);

            return View(data);
        }

        // 🔥 CREATE GET
        public IActionResult Create()
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            return View();
        }

        // 🔥 CREATE POST (sekaligus transaksi Beli pertama)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Investment model, string initialUnits, string initialPrice)
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            model.UserId = uid.Value;

            var cleanUnits = CleanDecimalString(initialUnits);
            var cleanPrice = CleanDecimalString(initialPrice);

            if (!decimal.TryParse(cleanUnits, out decimal units) || units <= 0)
                ModelState.AddModelError("", "Jumlah unit awal tidak valid.");

            if (!decimal.TryParse(cleanPrice, out decimal price) || price <= 0)
                ModelState.AddModelError("", "Harga beli awal tidak valid.");

            if (ModelState.IsValid)
            {
                model.TotalUnits = units;
                model.AvgBuyPrice = price;
                model.CurrentPrice = price; // default harga sekarang = harga beli awal

                _context.Investments.Add(model);
                await _context.SaveChangesAsync(); // supaya model.Id terisi

                _context.InvestmentTransactions.Add(new InvestmentTransaction
                {
                    InvestmentId = model.Id,
                    Type = "Buy",
                    Units = units,
                    PricePerUnit = price
                });
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // 🔥 BUY (top up)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Buy(int id, string units, string pricePerUnit)
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            var investment = await _context.Investments
                .FirstOrDefaultAsync(i => i.Id == id && i.UserId == uid);

            if (investment == null)
                return NotFound();

            var cleanUnits = CleanDecimalString(units);
            var cleanPrice = CleanDecimalString(pricePerUnit);

            if (!decimal.TryParse(cleanUnits, out decimal newUnits) || newUnits <= 0 ||
                !decimal.TryParse(cleanPrice, out decimal newPrice) || newPrice <= 0)
            {
                TempData["Error"] = "Jumlah unit atau harga tidak valid.";
                return RedirectToAction(nameof(Index));
            }

            // hitung ulang average buy price (weighted average)
            var totalCostLama = investment.TotalUnits * investment.AvgBuyPrice;
            var totalCostBaru = newUnits * newPrice;
            var totalUnitsBaru = investment.TotalUnits + newUnits;

            investment.AvgBuyPrice = (totalCostLama + totalCostBaru) / totalUnitsBaru;
            investment.TotalUnits = totalUnitsBaru;

            _context.InvestmentTransactions.Add(new InvestmentTransaction
            {
                InvestmentId = investment.Id,
                Type = "Buy",
                Units = newUnits,
                PricePerUnit = newPrice
            });

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // 🔥 SELL
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sell(int id, string units, string pricePerUnit)
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            var investment = await _context.Investments
                .FirstOrDefaultAsync(i => i.Id == id && i.UserId == uid);

            if (investment == null)
                return NotFound();

            var cleanUnits = CleanDecimalString(units);
            var cleanPrice = CleanDecimalString(pricePerUnit);

            if (!decimal.TryParse(cleanUnits, out decimal sellUnits) || sellUnits <= 0 ||
                !decimal.TryParse(cleanPrice, out decimal sellPrice) || sellPrice <= 0)
            {
                TempData["Error"] = "Jumlah unit atau harga tidak valid.";
                return RedirectToAction(nameof(Index));
            }

            if (sellUnits > investment.TotalUnits)
            {
                TempData["Error"] = "Jumlah unit yang dijual melebihi unit yang dimiliki.";
                return RedirectToAction(nameof(Index));
            }

            // AvgBuyPrice TIDAK berubah saat jual, cuma TotalUnits berkurang
            investment.TotalUnits -= sellUnits;

            _context.InvestmentTransactions.Add(new InvestmentTransaction
            {
                InvestmentId = investment.Id,
                Type = "Sell",
                Units = sellUnits,
                PricePerUnit = sellPrice
            });

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // 🔥 UPDATE HARGA SEKARANG
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePrice(int id, string newPrice)
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            var investment = await _context.Investments
                .FirstOrDefaultAsync(i => i.Id == id && i.UserId == uid);

            if (investment == null)
                return NotFound();

            var clean = CleanDecimalString(newPrice);

            if (!decimal.TryParse(clean, out decimal price) || price < 0)
            {
                TempData["Error"] = "Harga tidak valid.";
                return RedirectToAction(nameof(Index));
            }

            investment.CurrentPrice = price;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // 🔥 HISTORY
        public async Task<IActionResult> History(int id)
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            var investment = await _context.Investments
                .FirstOrDefaultAsync(i => i.Id == id && i.UserId == uid);

            if (investment == null)
                return NotFound();

            var history = await _context.InvestmentTransactions
                .Where(t => t.InvestmentId == id)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();

            ViewBag.Investment = investment;

            return View(history);
        }

        // 🔥 DELETE GET
        public async Task<IActionResult> Delete(int? id)
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var item = await _context.Investments
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

            var item = await _context.Investments
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid);

            if (item == null)
                return NotFound();

            _context.Investments.Remove(item);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // helper: bersihkan input angka dari karakter non-digit/non-titik-desimal
        private string CleanDecimalString(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "";

            var chars = input.Where(c => char.IsDigit(c) || c == '.').ToArray();
            return new string(chars);
        }
    }
}