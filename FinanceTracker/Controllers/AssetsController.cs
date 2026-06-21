using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinanceTracker.Data;
using FinanceTracker.Models;

namespace FinanceTracker.Controllers
{
    public class AssetsController : BaseController
    {
        private readonly AppDbContext _context;

        public AssetsController(AppDbContext context)
        {
            _context = context;
        }

        // 🔥 INDEX
        public async Task<IActionResult> Index()
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            var data = await _context.Assets
                .Where(a => a.UserId == uid)
                .OrderByDescending(a => a.CurrentValue)
                .ToListAsync();

            ViewBag.TotalValue = data.Sum(a => a.CurrentValue);

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

        // 🔥 CREATE POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Asset model)
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            model.UserId = uid.Value;

            if (ModelState.IsValid)
            {
                _context.Assets.Add(model);
                await _context.SaveChangesAsync(); // supaya model.Id terisi

                // catat nilai awal sebagai entry history pertama
                _context.AssetValueHistories.Add(new AssetValueHistory
                {
                    AssetId = model.Id,
                    Value = model.CurrentValue
                });
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // 🔥 UPDATE VALUE (catat perubahan nilai baru)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateValue(int id, string newValue)
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            var asset = await _context.Assets
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == uid);

            if (asset == null)
                return NotFound();

            var cleanValue = new string((newValue ?? "").Where(char.IsDigit).ToArray());

            if (!decimal.TryParse(cleanValue, out decimal parsedValue) || parsedValue < 0)
            {
                TempData["Error"] = "Nilai tidak valid.";
                return RedirectToAction(nameof(Index));
            }

            asset.CurrentValue = parsedValue;

            _context.AssetValueHistories.Add(new AssetValueHistory
            {
                AssetId = asset.Id,
                Value = parsedValue
            });

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // 🔥 HISTORY
        public async Task<IActionResult> History(int id)
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            var asset = await _context.Assets
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == uid);

            if (asset == null)
                return NotFound();

            var history = await _context.AssetValueHistories
                .Where(h => h.AssetId == id)
                .OrderByDescending(h => h.RecordedDate)
                .ToListAsync();

            ViewBag.Asset = asset;

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

            var item = await _context.Assets
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

            var item = await _context.Assets
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid);

            if (item == null)
                return NotFound();

            // AssetValueHistory otomatis ikut terhapus (Cascade di database)
            _context.Assets.Remove(item);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}