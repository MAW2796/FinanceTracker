# Catatan Pembelajaran

Catatan ini berisi rangkuman konsep yang dipelajari selama mengembangkan project FinanceTracker — seputar Entity Framework Core, scaffolding, dan pengamanan konfigurasi di ASP.NET Core.

## Entity Framework Core (EF Core)

EF Core adalah *Object-Relational Mapper (ORM)* dari Microsoft yang memungkinkan interaksi dengan database tanpa menulis SQL secara manual. Class C# digunakan sebagai representasi tabel database, sehingga operasi CRUD (Create, Read, Update, Delete) bisa dilakukan langsung lewat kode C#.

### Konsep Utama

| Konsep | Penjelasan |
| --- | --- |
| `DbContext` | Kelas utama untuk koneksi ke database, mengatur query dan perubahan data |
| `DbSet` | Representasi tabel di database |
| Entity | Class model yang mewakili satu tabel |
| Migration | Fitur untuk membuat dan memperbarui struktur database dari model C# |

### Kelebihan EF Core

- Lebih cepat dibanding versi EF sebelumnya
- Cross-platform (Windows, Linux, macOS)
- Mendukung banyak database (SQL Server, MySQL, PostgreSQL, dll)
- Terintegrasi penuh dengan .NET Core

### Langkah Migration (lewat Package Manager Console)

1. Buka menu **Tools**
2. Pilih **NuGet Package Manager**
3. Pilih **Package Manager Console**
4. Jalankan `Add-Migration InitialCreate`
5. Jalankan `Update-Database`

## CRUD dengan Scaffolding

Scaffolding adalah fitur ASP.NET Core yang otomatis membuat Controller, View, dan koneksi ke Model & Database (EF Core) tanpa perlu menulis semuanya dari nol. Cukup siapkan model dan `DbContext`, sisanya digenerate otomatis.

### Hasil dari Scaffold CRUD

1. **Controller** — berisi logic CRUD (Index, Details, Create, Edit, Delete)
2. **Views** — halaman UI untuk list data, form tambah, form edit, dan hapus
3. **Integrasi EF Core** — langsung terhubung ke database lewat `DbContext`

### Cara Generate

1. Klik kanan folder **Controllers**
2. Pilih **Add** → **New Scaffolded Item**
3. Pilih **MVC Controller with views, using Entity Framework**
4. Isi detail, contoh:
   - Model class: `Category`
   - Data context: `AppDbContext`
   - Controller name: `CategoriesController`

### Kesimpulan

Scaffolding mempercepat development dengan membuat fitur CRUD lengkap (Controller + View + Logic database) secara otomatis berdasarkan model dan `DbContext` yang sudah ada.

## Mengamankan Connection String dengan User Secrets

Connection string ke database biasanya berisi data sensitif (username, password, alamat server). Kalau ditulis langsung di `appsettings.json`, data ini ikut ter-commit ke Git dan bisa terlihat siapa saja yang punya akses ke repository — bahkan kalau dihapus belakangan, jejaknya masih ada di history commit.

Solusinya untuk development lokal adalah **.NET User Secrets**: secret disimpan di file `secrets.json` yang lokasinya di luar folder project (jadi otomatis tidak pernah ikut Git), tapi tetap otomatis terbaca oleh aplikasi saat berjalan di environment `Development`.

### Cara Setup

1. Pastikan command dijalankan di folder yang berisi file `.csproj` (folder project, bukan folder solution).
2. Inisialisasi secrets untuk project:
   ```bash
   dotnet user-secrets init
   ```
   Command ini menambahkan `UserSecretsId` (berupa GUID) ke file `.csproj` — aman untuk ikut di-commit, karena cuma identifier, bukan data rahasia.
3. Set value secret:
   ```bash
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=...;Database=...;User Id=...;Password=...;TrustServerCertificate=True;"
   ```
4. Verifikasi secret tersimpan:
   ```bash
   dotnet user-secrets list
   ```
5. Kosongkan value asli di `appsettings.json`, sisakan struktur key-nya saja:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": ""
     }
   }
   ```

Alternatif tanpa terminal: klik kanan project di Solution Explorer → **Manage User Secrets**, lalu edit `secrets.json` langsung dari editor Visual Studio.

### Catatan Tambahan

User Secrets hanya berlaku untuk development lokal. Untuk production (setelah aplikasi online), secret sebaiknya disimpan di environment variable pada server hosting, atau secret manager seperti Azure Key Vault — bukan di file config maupun di User Secrets.
