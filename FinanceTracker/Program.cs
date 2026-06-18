using FinanceTracker.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

/// =======================
/// DATABASE
/// =======================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


/// =======================
/// LOCALIZATION
/// =======================
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();


/// =======================
/// SESSION (SECURE)
/// =======================
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);

    options.Cookie.HttpOnly = true; // tidak bisa diakses JS
    options.Cookie.SecurePolicy = CookieSecurePolicy.None; // hanya HTTPS
    options.Cookie.SameSite = SameSiteMode.Lax; // cegah CSRF
    options.Cookie.IsEssential = true;
});


/// =======================
/// LOGGING (BASIC)
/// =======================
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();


/// =======================
/// ERROR HANDLING
/// =======================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts(); // paksa HTTPS di browser
}


/// =======================
/// SECURITY HEADERS (CSP)
/// =======================
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("Content-Security-Policy",
        "default-src 'self'; " +
        "img-src 'self' data:; " +
        "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "style-src 'self' 'unsafe-inline'; " +
        "connect-src 'self' ws://localhost:* https://localhost:*;");

    await next();
});


/// =======================
/// HTTPS
/// =======================
app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();


/// =======================
/// LOCALIZATION
/// =======================
var supportedCultures = new[] { "id-ID", "en-US" };

var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("id-ID")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);


/// =======================
/// SESSION (HARUS SEBELUM AUTH)
/// =======================
app.UseSession();


/// =======================
/// AUTHORIZATION
/// =======================
app.UseAuthorization();


/// =======================
/// ROUTING
/// =======================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


/// =======================
app.Run();