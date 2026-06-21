# FinanceTracker (MAWApps)

Personal finance tracking app yang dibangun sebagai project belajar teknologi **.NET** — mulai dari web, nantinya mobile, dan service API. Tujuannya untuk mencoba berbagai aspek development satu per satu sambil terus belajar.

## Tentang Project

Project ini dikembangkan secara bertahap menggunakan pendekatan **Mini Agile / Iterative Development**, karena sifatnya personal dan dibangun incremental. Setiap fitur melewati alur:

1. Requirement
2. Design
3. Database
4. Development
5. Testing

Dengan pendekatan ini, setiap fitur ditest sebelum lanjut ke fitur berikutnya, sehingga proses improvement lebih mudah dilakukan.

## Tech Stack

| Kategori | Teknologi |
| --- | --- |
| Backend | C# |
| Framework | ASP.NET Core MVC |
| ORM | Entity Framework Core |
| Database | SQL Server |
| UI | Razor & Bootstrap 5 |
| Chart | Chart.js |
| IDE | Visual Studio 2022 |

## Fitur

### Sudah Selesai ✅

**Foundation**
- Backend ASP.NET MVC
- Database dengan EF Core
- Login, Role, & Session System
- Dynamic Navbar
- CRUD Transactions & Categories (scaffold)
- Hash Password dengan BCrypt
- Unauthorized Access Handling

**Security**
- Session Security (HttpOnly, Secure, SameSite)
- Authorization (filter data by UserId)
- Input Validation (Model Validation)
- Anti SQL Injection (EF / Parameterized Query)
- HTTPS Enforcement & Content Security Policy (CSP)
- Anti CSRF Protection
- Hide Sensitive Error Message & Basic Error Logging
- Connection string & secret dipisah dari repo (lihat bagian [Keamanan & Konfigurasi](#keamanan--konfigurasi))

**UI**
- Dark Mode
- Multi Language (ID / EN)
- Rapihin Transactions, Categories, & Admin UI
- Dashboard Menu (Pengunjung / User / Admin)
- Fitur Planning (create, edit, delete, sinkron otomatis dengan Transactions)
- Kategori dibatasi hanya tipe expense untuk planning
- Dashboard user dengan data dinamis (summary cards, filter bulan, tabel transaksi terbaru, bar chart 3 bulan terakhir, pie chart distribusi pengeluaran per kategori — semua dari database, tidak ada lagi hardcode)
- AI Chat Assistant (Groq, model `llama-3.3-70b-versatile`) untuk diskusi & saran keuangan, dengan context data transaksi (ringkasan 12 bulan + detail 1 bulan terakhir) dan planning aktif milik user
- Fitur Cicilan (`Installment`): plafon, cicilan per bulan, tenor, dan tanggal mulai. Otomatis generate entry di Monthly Planning saat mendekati 15 hari jatuh tempo (dengan catch-up kalau aplikasi tidak dibuka beberapa bulan), progress lunas/berjalan per cicilan, terhubung ke Planning lewat `InstallmentId`
- Fitur Utang Piutang (`DebtReceivable` + `DebtPayment`): halaman terpisah dari Planning (bukan expense-only), mendukung Utang (kamu yang bayar) maupun Piutang (orang lain bayar ke kamu), bisa bayar sebagian (partial payment), badge "Mendekati jatuh tempo" (≤15 hari), tiap pembayaran otomatis bikin Transaction (Utang → expense, Piutang → income)

### Sedang Dikerjakan 🔄

- Security Testing — coba pakai Burp Suite, test input aneh, cek endpoint tanpa login
- Rapihkan UI fitur-fitur yang sudah jalan (separator, desain, dll)
- Aset (daftar aset + riwayat perubahan nilai)

### Rencana Selanjutnya

- Ekspansi ke platform Mobile
- Ekspansi ke Service API

## Perubahan Terbaru

- Tambah fitur Utang Piutang (`DebtReceivablesController` + model `DebtReceivable`/`DebtPayment`). Mendukung pembayaran sebagian (partial payment) lewat modal di halaman index, tiap pembayaran otomatis bikin `Transaction` (Utang = expense, Piutang = income). Delete `DebtReceivable` ikut menghapus `DebtPayment` terkait (cascade), tapi Transaction historis tetap dipertahankan.
- **Bugfix penting**: nominal pembayaran sebelumnya dikirim sebagai `decimal` langsung dari form, rawan salah parsing kalau request culture `en-US` aktif (titik ribuan dibaca sebagai desimal, contoh "100.000" terbaca 100). Sekarang nominal dikirim sebagai `string`, dibersihkan manual di server (`Where(char.IsDigit)`) lalu di-parse — tidak lagi tergantung culture sama sekali.
- **Bugfix**: ViewBag berisi `List<AnonymousType>` tidak bisa di-cast ke `List<dynamic>` di Razor view (selalu jadi `null` secara diam-diam). Diganti pakai ViewModel asli (`DebtItemViewModel`) untuk data gabungan entity + hasil kalkulasi.
- Tambah fitur Cicilan (`InstallmentsController` + `Installment` model). Generator (`GenerateInstallmentPlannings`) dipanggil tiap kali halaman Monthly Planning dibuka: cek semua cicilan aktif milik user, generate row `MonthlyPlanning` baru (dengan `InstallmentId` terisi) untuk periode yang jatuh temponya sudah dalam 15 hari ke depan atau lewat, pakai while-loop supaya catch-up kalau ada periode yang terlewat. Cicilan otomatis `IsActive = false` setelah semua periode (`TenorMonths`) ke-generate. Delete cicilan tidak ikut menghapus Planning yang sudah ter-generate (riwayat tetap aman), hanya melepas relasinya (`InstallmentId = null`).
- Redesign UI halaman AI Chat Assistant: tema "ledger/struk keuangan" (garis halus ala kertas pembukuan, tabel balasan AI pakai font monospace, efek garis sobekan di atas input), quick-prompt chips, typing indicator, dan dukungan dark mode. CSS dipisah ke `wwwroot/css/aichat.css` (di-load lewat `@section Styles` baru di `_Layout.cshtml`), tidak dicampur ke `site.css` global.
- Tambah fitur AI Chat Assistant (`AiChatController`) yang memanggil Groq API (`llama-3.3-70b-versatile`) lewat `IHttpClientFactory`, dengan context dibangun dari data transaksi & planning user (tidak pernah lintas user, selalu difilter `UserId`).
- API key Groq disimpan via .NET User Secrets (`Groq:ApiKey`), tidak pernah masuk ke repo.
- Balasan AI dalam format Markdown, dirender ke HTML di client pakai `marked.js`, lalu disanitasi pakai `DOMPurify` sebelum ditampilkan untuk mencegah XSS dari kemungkinan prompt injection.
- Endpoint `SendMessage` dilindungi `[ValidateAntiForgeryToken]`, validasi panjang pesan (maks 1000 karakter), dan logging error pakai `ILogger` (tanpa membocorkan detail exception ke response).
- Dashboard (`TransactionsController.Dashboard`) diubah dari hardcode jadi full dynamic: query Saldo/Pemasukan/Pengeluaran per bulan terpilih, distribusi pengeluaran per kategori (pie chart), tren 3 bulan terakhir (bar chart), dan transaksi terbaru — semua dari `AppDbContext`.
- Filter bulan di Dashboard pakai `<input type="month">`, redirect dengan query parameter `month`, parsing pakai `DateTime.TryParse` (bukan `Parse`) supaya input tidak valid tidak bikin halaman crash.
- Tambah loading indicator sederhana (disable input + teks "Memuat...") saat filter bulan diganti.
- Refactor `BaseController` agar berisi helper bersama `GetUserId()` untuk otentikasi session.
- Menghapus duplikasi metode `GetUserId()` di controller turunannya (`Categories`, `Goals`, `MonthlyPlannings`, `Transactions`).
- Memperbaiki `HomeController` supaya membaca role dari session key yang benar: `UserRole`.
- Memperbaiki nama file yang salah dari `BaseController .cs` menjadi `BaseController.cs`.
- Validasi berhasil dengan `dotnet build FinanceTracker.sln` tanpa error compile.

## Struktur Project

```
FinanceTracker/
├── Controllers/    # MVC Controllers
├── Data/           # DbContext & konfigurasi EF Core
├── docs/           # Catatan pembelajaran & dokumentasi tambahan
├── Migrations/     # EF Core migrations
├── Models/         # Entity & ViewModel
├── Properties/     # Launch settings
├── Resources/      # Resource untuk multi-language (ID/EN)
├── Views/          # Razor Views
├── wwwroot/        # Static files (CSS, JS, images)
├── Program.cs
└── SharedResource.cs
```

## Keamanan & Konfigurasi

Connection string dan secret lain **tidak disimpan di repository**. Untuk development lokal, project ini pakai [.NET User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) — secret tersimpan di luar folder project, jadi tidak ikut ke-commit walau ada di history Git.

`appsettings.json` di repo ini hanya berisi struktur key tanpa value asli:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": ""
  }
}
```

Untuk production nanti, secret sebaiknya dipindah ke environment variable atau secret manager (misalnya Azure Key Vault), bukan disimpan di file config.

## Getting Started

### Prerequisites
- .NET SDK (sesuai versi project)
- SQL Server (LocalDB / Express / instance lain)
- Visual Studio 2022

### Instalasi

1. Clone repository
   ```bash
   git clone https://github.com/username/FinanceTracker.git
   ```
2. Buka `FinanceTracker.sln` di Visual Studio.
3. Setup connection string lewat User Secrets (jalankan di folder yang ada file `.csproj`):
   ```bash
   dotnet user-secrets init
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=...;Database=...;User Id=...;Password=...;TrustServerCertificate=True;"
   ```
4. Setup Groq API key (gratis, daftar di [console.groq.com](https://console.groq.com)) untuk fitur AI Chat Assistant:
   ```bash
   dotnet user-secrets set "Groq:ApiKey" "isi-api-key-groq-kamu"
   ```
5. Jalankan migration lewat Package Manager Console:
   ```
   Add-Migration InitialCreate
   Update-Database
   ```
6. Run project (F5).

### Menambahkan Fitur CRUD Baru (via Scaffold)

1. Klik kanan folder `Controllers` → **Add** → **New Scaffolded Item**
2. Pilih **MVC Controller with views, using Entity Framework**
3. Tentukan Model class, Data context (`AppDbContext`), dan nama controller
4. Visual Studio otomatis generate Controller + Views + integrasi EF Core

## Catatan

Project ini murni untuk pembelajaran pribadi, jadi beberapa fitur masih dalam proses penyempurnaan. Kontribusi, masukan, atau diskusi sangat terbuka lewat *Issues*.

Catatan pembelajaran lebih lengkap (konsep EF Core, scaffolding, User Secrets, dll) ada di [`docs/notes.md`](docs/notes.md).