using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using USMK.Web;
using USMK.Web.Components;
using USMK.Web.Data;
using USMK.Web.Services;

var builder = WebApplication.CreateBuilder(args);

var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("ApiBaseUrl belum diisi di appsettings.json.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireDigit = true;
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddClaimsPrincipalFactory<UsmkClaimsPrincipalFactory>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

// Halaman login/logout hidup di Blazor, jadi path bawaan Identity dialihkan.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.AccessDeniedPath = "/access-denied";
    options.ReturnUrlParameter = "redirect";
    options.Cookie.Name = "usmk.auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    // Durasi ujian diizinkan sampai 600 menit (10 jam), jadi masa berlaku sesi
    // harus lebih panjang dari itu supaya peserta ujian panjang tidak keluar
    // di tengah pengerjaan. Sliding expiration tidak menolong di sini karena
    // rendering interaktif tidak mengirim request HTTP baru.
    options.ExpireTimeSpan = TimeSpan.FromHours(12);
    options.SlidingExpiration = true;
});

// Cookie sesi ikut diteruskan ke API agar API bisa memvalidasi pengguna yang sama.
builder.Services.AddTransient<ForwardAuthCookieHandler>();
builder.Services.AddHttpClient<UsmkApi>(client => client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/"))
    .AddHttpMessageHandler(sp => sp.GetRequiredService<ForwardAuthCookieHandler>());

// Singleton supaya aman dipanggil dari Dispose() komponen saat circuit ditutup.
builder.Services.AddSingleton<PencatatKeluar>();

// Client terpisah untuk PencatatKeluar: dipakai dari Dispose(), jadi tidak boleh
// bergantung pada HttpClient milik scope render yang mungkin sudah dibuang.
builder.Services.AddHttpClient(PencatatKeluar.NamaKlien, client => client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/"))
    .AddHttpMessageHandler<ForwardAuthCookieHandler>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapAuthEndpoints();

var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Usmk.Seed");

try
{
    await DbSeeder.SeedAsync(app.Services, app.Configuration, logger);
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Gagal menyiapkan database aplikasi.");
    throw;
}

app.Run();