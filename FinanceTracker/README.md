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

### Sedang Dikerjakan 🔄

- Security Testing — coba pakai Burp Suite, test input aneh, cek endpoint tanpa login
- Rapihkan UI fitur-fitur yang sudah jalan (separator, desain, dll)
- Dashboard user dengan data dinamis (tidak hardcode)

### Rencana Selanjutnya

- Ekspansi ke platform Mobile
- Ekspansi ke Service API

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
4. Jalankan migration lewat Package Manager Console:
   ```
   Add-Migration InitialCreate
   Update-Database
   ```
5. Run project (F5).

### Menambahkan Fitur CRUD Baru (via Scaffold)

1. Klik kanan folder `Controllers` → **Add** → **New Scaffolded Item**
2. Pilih **MVC Controller with views, using Entity Framework**
3. Tentukan Model class, Data context (`AppDbContext`), dan nama controller
4. Visual Studio otomatis generate Controller + Views + integrasi EF Core

## Catatan

Project ini murni untuk pembelajaran pribadi, jadi beberapa fitur masih dalam proses penyempurnaan. Kontribusi, masukan, atau diskusi sangat terbuka lewat *Issues*.

Catatan pembelajaran lebih lengkap (konsep EF Core, scaffolding, User Secrets, dll) ada di [`docs/notes.md`](docs/notes.md).
