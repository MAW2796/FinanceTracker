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
    public class InstallmentsController : BaseController
    {
        private readonly AppDbContext _context;

        public InstallmentsController(AppDbContext context)
        {
            _context = context;
        }

        // 🔥 INDEX
        public async Task<IActionResult> Index()
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            var data = await _context.Installments
                .Include(i => i.Category)
                .Where(i => i.UserId == uid)
                .OrderByDescending(i => i.IsActive)
                .ThenBy(i => i.StartDate)
                .ToListAsync();

            // hitung progress tiap cicilan (berapa kali sudah ke-generate)
            var progress = await _context.MonthlyPlannings
                .Where(p => p.InstallmentId != null && p.UserId == uid)
                .GroupBy(p => p.InstallmentId)
                .Select(g => new { InstallmentId = g.Key, Count = g.Count(), PaidCount = g.Count(p => p.IsPaid) })
                .ToListAsync();

            ViewBag.Progress = progress.ToDictionary(x => x.InstallmentId, x => x);

            return View(data);
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
        public async Task<IActionResult> Create(Installment model)
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            model.UserId = uid.Value;
            model.IsActive = true;

            if (ModelState.IsValid)
            {
                _context.Installments.Add(model);
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

        // 🔥 DELETE GET (konfirmasi)
        public async Task<IActionResult> Delete(int? id)
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var item = await _context.Installments
                .Include(i => i.Category)
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

            var item = await _context.Installments
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid);

            if (item == null)
                return NotFound();

            // Planning yang sudah ter-generate dari cicilan ini TETAP dibiarkan ada
            // (riwayat cicilan yang sudah/sedang berjalan tidak boleh hilang),
            // tapi referensinya dilepas (InstallmentId di-null-kan) supaya tidak nyangkut
            var relatedPlannings = await _context.MonthlyPlannings
                .Where(p => p.InstallmentId == item.Id && p.UserId == uid)
                .ToListAsync();

            foreach (var p in relatedPlannings)
            {
                p.InstallmentId = null;
            }

            _context.Installments.Remove(item);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}