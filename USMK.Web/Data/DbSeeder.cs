using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace USMK.Web.Data;

public static class DbSeeder
{
    public const string AdminRole = "Admin";
    public const string PesertaRole = "Peserta";

    public static async Task SeedAsync(IServiceProvider services, IConfiguration config, ILogger logger)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        var db = provider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { AdminRole, PesertaRole })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));
                if (!result.Succeeded)
                    logger.LogError("Gagal membuat role {Role}: {Error}", role, string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }

        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();

        await EnsureUserAsync(
            userManager,
            logger,
            config["Seed:AdminEmail"] ?? "admin@usmk.id",
            config["Seed:AdminPassword"],
            config["Seed:AdminNama"] ?? "Administrator USMK",
            AdminRole);

        var pesertaEmail = config["Seed:PesertaEmail"];
        if (!string.IsNullOrWhiteSpace(pesertaEmail))
        {
            await EnsureUserAsync(
                userManager,
                logger,
                pesertaEmail,
                config["Seed:PesertaPassword"],
                config["Seed:PesertaNama"] ?? "Peserta Demo",
                PesertaRole,
                config["Seed:PesertaNis"] ?? "10001",
                config["Seed:PesertaKelas"] ?? "XII RPL 1");
        }
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        ILogger logger,
        string email,
        string? configuredPassword,
        string namaLengkap,
        string role,
        string? nis = null,
        string? kelas = null)
    {
        if (await userManager.FindByEmailAsync(email) is not null)
            return;

        var generated = string.IsNullOrWhiteSpace(configuredPassword);
        var password = generated ? GeneratePassword() : configuredPassword!;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            NamaLengkap = namaLengkap,
            Nis = nis,
            Kelas = kelas
        };

        var created = await userManager.CreateAsync(user, password);
        if (!created.Succeeded)
        {
            logger.LogError("Gagal membuat user {Email}: {Error}", email, string.Join("; ", created.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(user, role);

        if (generated)
            logger.LogWarning("Password sementara untuk {Email} ({Role}): {Password} - segera ganti setelah login.", email, role, password);
        else
            logger.LogInformation("User {Email} dibuat dengan role {Role}.", email, role);
    }

    private static string GeneratePassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#";
        var random = Random.Shared;
        return "Usmk" + new string(Enumerable.Range(0, 10).Select(_ => chars[random.Next(chars.Length)]).ToArray()) + "1!";
    }
}