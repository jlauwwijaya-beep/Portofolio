using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace USMK.Web.Data;

/// <summary>
/// Dipakai `dotnet ef` saat membuat migration supaya tools tidak menjalankan
/// Program.cs (yang melakukan seeding ke database) pada waktu design-time.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var konfigurasi = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(konfigurasi.GetConnectionString("DefaultConnection")
                ?? "Server=localhost;Database=USMK;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new AppDbContext(options);
    }
}
