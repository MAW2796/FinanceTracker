using FinanceTracker.Data;
using FinanceTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using System.Net.Http.Json;

namespace FinanceTracker.Controllers
{
    public class AiChatController : BaseController
    {
        private readonly AppDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<AiChatController> _logger;

        public AiChatController(AppDbContext context, IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<AiChatController> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _config = config;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        private async Task<string> BuildUserContext(int userId)
        {
            var twelveMonthsAgo = new DateTime(DateTime.Now.AddMonths(-11).Year, DateTime.Now.AddMonths(-11).Month, 1);
            var oneMonthAgo = DateTime.Now.AddMonths(-1);

            // 1. Ringkasan agregat per bulan (12 bulan terakhir)
            var allTransactions = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.UserId == userId && t.Date >= twelveMonthsAgo)
                .ToListAsync();

            var monthlySummary = allTransactions
                .GroupBy(t => new { t.Date.Year, t.Date.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new
                {
                    Period = new DateTime(g.Key.Year, g.Key.Month, 1),
                    Income = g.Where(t => t.Category?.Type == "Income").Sum(t => t.Amount),
                    Expense = g.Where(t => t.Category?.Type == "Expense").Sum(t => t.Amount)
                });

            // 2. Detail transaksi 1 bulan terakhir
            var recentTransactions = allTransactions
                .Where(t => t.Date >= oneMonthAgo)
                .OrderByDescending(t => t.Date)
                .ToList();

            // 3. Planning yang belum dibayar
            var plannings = await _context.MonthlyPlannings
                .Include(p => p.Category)
                .Where(p => p.UserId == userId && !p.IsPaid)
                .OrderBy(p => p.DueDate)
                .ToListAsync();

            var sb = new StringBuilder();

            sb.AppendLine("=== RINGKASAN BULANAN (12 BULAN TERAKHIR) ===");
            if (!monthlySummary.Any())
            {
                sb.AppendLine("Tidak ada data.");
            }
            else
            {
                foreach (var m in monthlySummary)
                {
                    sb.AppendLine($"- {m.Period:MMMM yyyy} | Pemasukan: Rp{m.Income:N0} | Pengeluaran: Rp{m.Expense:N0} | Saldo: Rp{(m.Income - m.Expense):N0}");
                }
            }

            sb.AppendLine();
            sb.AppendLine("=== DETAIL TRANSAKSI (1 BULAN TERAKHIR) ===");
            if (!recentTransactions.Any())
            {
                sb.AppendLine("Tidak ada transaksi.");
            }
            else
            {
                foreach (var t in recentTransactions)
                {
                    sb.AppendLine($"- {t.Date:yyyy-MM-dd} | {t.Category?.Type} | {t.Category?.Name} | Rp{t.Amount:N0} | {t.Description}");
                }
            }

            sb.AppendLine();
            sb.AppendLine("=== PLANNING YANG BELUM DIBAYAR ===");
            if (!plannings.Any())
            {
                sb.AppendLine("Tidak ada planning aktif.");
            }
            else
            {
                foreach (var p in plannings)
                {
                    sb.AppendLine($"- {p.Title} | Rp{p.Amount:N0} | Jatuh tempo: {p.DueDate:yyyy-MM-dd} | Kategori: {p.Category?.Name}");
                }
            }

            return sb.ToString();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        {
            var uid = GetUserId();
            if (uid == null)
                return Json(new ChatResponse { Success = false, Error = "Sesi habis, silakan login kembali." });

            if (string.IsNullOrWhiteSpace(request?.Message))
                return Json(new ChatResponse { Success = false, Error = "Pesan tidak boleh kosong." });

            if (request.Message.Length > 1000)
                return Json(new ChatResponse { Success = false, Error = "Pesan terlalu panjang (maksimal 1000 karakter)." });

            var apiKey = _config["Groq:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                return Json(new ChatResponse { Success = false, Error = "AI belum dikonfigurasi." });

            var context = await BuildUserContext(uid.Value);

            var systemPrompt = "Kamu adalah asisten keuangan pribadi di aplikasi FinanceTracker. " +
                "Jawab pertanyaan user HANYA berdasarkan data keuangan yang diberikan di bawah ini. " +
                "Jangan pernah mengarang data yang tidak ada. " +
                "Jika data tidak cukup untuk menjawab, katakan dengan jujur. " +
                "Jawab dalam Bahasa Indonesia, gunakan format Markdown (boleh pakai tabel, bold, list) supaya mudah dibaca. " +
                "Kamu hanya boleh memberikan analisis dan saran, kamu tidak bisa mengubah, menghapus, atau menambah data apapun.\n\n" +
                context;

            var payload = new
            {
                model = "llama-3.3-70b-versatile",
                messages = new[]
                {
            new { role = "system", content = systemPrompt },
            new { role = "user", content = request.Message }
        },
                temperature = 0.4
            };

            var client = _httpClientFactory.CreateClient("Groq");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            try
            {
                var response = await client.PostAsJsonAsync("chat/completions", payload);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Groq API gagal. Status: {Status}, Body: {Body}", response.StatusCode, errorBody);
                    return Json(new ChatResponse { Success = false, Error = "AI sedang tidak bisa diakses, coba lagi nanti." });
                }

                var raw = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(raw);

                var reply = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                return Json(new ChatResponse { Success = true, Reply = reply });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gagal memproses chat AI untuk UserId {UserId}", uid);
                return Json(new ChatResponse { Success = false, Error = "Terjadi kesalahan saat menghubungi AI." });
            }
        }
    }
}