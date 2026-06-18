using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinanceTracker.Data;
using FinanceTracker.Models;
using FinanceTracker.Models.ViewModels;

namespace FinanceTracker.Controllers
{
    public class GoalsController : Controller
    {
        private readonly AppDbContext _context;

        public GoalsController(AppDbContext context)
        {
            _context = context;
        }

        private int? GetUserId()
        {
            var userId = HttpContext.Session.GetString("UserId");
            return userId != null ? int.Parse(userId) : null;
        }

        public async Task<IActionResult> Index()
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            var goals = await _context.Goals
                .Where(g => g.UserId == uid)
                .OrderBy(g => g.CurrentAmount >= g.TargetAmount)
                .ThenBy(g => g.TargetDate)
                .ToListAsync();

            var vm = new GoalIndexViewModel
            {
                Goals = goals,
                ActiveCount = goals.Count(g => g.CurrentAmount < g.TargetAmount),
                CompletedCount = goals.Count(g => g.CurrentAmount >= g.TargetAmount),
                TotalSaved = goals.Sum(g => g.CurrentAmount),
                TotalTarget = goals.Sum(g => g.TargetAmount)
            };

            return View(vm);
        }

        // DETAILS
        public async Task<IActionResult> Details(int id)
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            var goal = await _context.Goals
                .FirstOrDefaultAsync(g => g.Id == id && g.UserId == uid);

            if (goal == null)
                return NotFound();

            var contributions = await _context.GoalContributions
                .Where(c => c.GoalId == goal.Id)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var vm = new GoalDetailsViewModel
            {
                Goal = goal,
                Contributions = contributions
            };

            return View(vm);
        }

        // CREATE GET
        public IActionResult Create()
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            return View();
        }

        // CREATE POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Goal goal)
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            goal.UserId = uid.Value;
            goal.CurrentAmount = 0;

            if (goal.TargetAmount <= 0)
            {
                ModelState.AddModelError("TargetAmount", "Target harus lebih dari 0.");
            }

            if (ModelState.IsValid)
            {
                _context.Goals.Add(goal);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Goal berhasil ditambahkan.";
                return RedirectToAction(nameof(Index));
            }

            return View(goal);
        }

        // ADD AMOUNT + SIMPAN RIWAYAT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAmount(int id, decimal amount)
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            var goal = await _context.Goals
                .FirstOrDefaultAsync(g => g.Id == id && g.UserId == uid);

            if (goal == null)
                return NotFound();

            if (amount <= 0)
            {
                TempData["Error"] = "Nominal tabungan harus lebih dari 0.";
                return RedirectToAction(nameof(Index));
            }

            if (goal.CurrentAmount >= goal.TargetAmount)
            {
                TempData["Error"] = "Goal ini sudah tercapai.";
                return RedirectToAction(nameof(Index));
            }

            var remaining = goal.TargetAmount - goal.CurrentAmount;
            var finalAmount = amount > remaining ? remaining : amount;

            goal.CurrentAmount += finalAmount;

            var contribution = new GoalContribution
            {
                GoalId = goal.Id,
                Amount = finalAmount,
                CreatedAt = DateTime.Now
            };

            _context.GoalContributions.Add(contribution);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Tabungan berhasil ditambahkan.";
            return RedirectToAction(nameof(Index));
        }

        // EDIT GET
        public async Task<IActionResult> Edit(int id)
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            var goal = await _context.Goals
                .FirstOrDefaultAsync(g => g.Id == id && g.UserId == uid);

            if (goal == null)
                return NotFound();

            return View(goal);
        }

        // EDIT POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Goal model)
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            var goal = await _context.Goals
                .FirstOrDefaultAsync(g => g.Id == id && g.UserId == uid);

            if (goal == null)
                return NotFound();

            if (model.TargetAmount <= 0)
            {
                ModelState.AddModelError("TargetAmount", "Target harus lebih dari 0.");
            }

            if (ModelState.IsValid)
            {
                goal.Name = model.Name;
                goal.TargetAmount = model.TargetAmount;
                goal.TargetDate = model.TargetDate;

                if (goal.CurrentAmount > goal.TargetAmount)
                {
                    goal.CurrentAmount = goal.TargetAmount;
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Goal berhasil diperbarui.";
                return RedirectToAction(nameof(Details), new { id = goal.Id });
            }

            return View(model);
        }

        // DELETE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            var goal = await _context.Goals
                .FirstOrDefaultAsync(g => g.Id == id && g.UserId == uid);

            if (goal != null)
            {
                var contributions = await _context.GoalContributions
                    .Where(c => c.GoalId == goal.Id)
                    .ToListAsync();

                _context.GoalContributions.RemoveRange(contributions);
                _context.Goals.Remove(goal);

                await _context.SaveChangesAsync();
                TempData["Success"] = "Goal berhasil dihapus.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteContribution(int id)
        {
            var uid = GetUserId();
            if (uid == null)
                return RedirectToAction("Login", "Account");

            var contribution = await _context.GoalContributions
                .Include(c => c.Goal)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contribution == null)
                return NotFound();

            // pastikan goal milik user yang sedang login
            if (contribution.Goal.UserId != uid)
                return Unauthorized();

            var goal = contribution.Goal;

            // kurangi saldo goal
            goal.CurrentAmount -= contribution.Amount;

            // jaga-jaga biar tidak minus
            if (goal.CurrentAmount < 0)
            {
                goal.CurrentAmount = 0;
            }

            _context.GoalContributions.Remove(contribution);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Riwayat nabung berhasil dihapus.";
            return RedirectToAction(nameof(Details), new { id = goal.Id });
        }
    }
}