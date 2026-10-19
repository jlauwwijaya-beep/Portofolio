using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using USMK.Web.Data;
using USMK.Web.Models;

namespace USMK.Web.Services;

/// <summary>
/// Endpoint untuk login, register, dan logout. Form-nya dirender sebagai
/// static SSR lalu di-post ke sini, jadi tidak bergantung pada Blazor WebSocket.
/// </summary>
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/account/login", async Task<IResult> (
            HttpContext http,
            [FromForm] LoginInput input,
            SignInManager<ApplicationUser> signIn,
            UserManager<ApplicationUser> users) =>
        {
            var hasil = await signIn.PasswordSignInAsync(
                input.Email?.Trim() ?? "", input.Password ?? "", isPersistent: true, lockoutOnFailure: true);
            if (!hasil.Succeeded)
            {
                var pesan = hasil.IsLockedOut
                    ? "Akun terkunci sementara karena terlalu banyak percobaan gagal."
                    : "Email atau password salah.";

                return Results.Redirect($"/login?error={Uri.EscapeDataString(pesan)}");
            }

            var user = await users.FindByEmailAsync(input.Email!.Trim());
            var admin = user is not null && await users.IsInRoleAsync(user, DbSeeder.AdminRole);

            return Results.Redirect(BersihkanReturnUrl(input.ReturnUrl) ?? (admin ? "/" : "/kerjakan-ujian"));
        })
        // Token antiforgery terikat pada identitas user saat halaman dirender.
        // Kalau identitas berubah sebelum submit (sesi kedaluwarsa, logout di tab
        // lain, atau login di tab lain), validasi gagal dan login jadi 400.
        // Antiforgery dimatikan di endpoint autentikasi; logout memang tidak
        // memvalidasinya, dan risiko CSRF login/register sangat kecil.
        .DisableAntiforgery();

        app.MapPost("/account/register", async Task<IResult> (
            HttpContext http,
            [FromForm] RegisterInput input,
            UserManager<ApplicationUser> users,
            SignInManager<ApplicationUser> signIn,
            ILoggerFactory loggerFactory) =>
        {
            var log = loggerFactory.CreateLogger("Usmk.Register");

            var email = input.Email?.Trim() ?? "";
            var password = input.Password ?? "";

            if (Periksa(input) is { } invalid)
                return GagalDaftar(string.Join(" ", invalid));

            var nis = input.Nis.Trim();

            // Index unik di kolom Nis tidak dipetakan Identity ke pesan yang ramah,
            // jadi dicek dulu supaya pendaftaran NIS ganda tidak berujung error 500.
            if (await users.Users.AnyAsync(u => u.Nis == nis))
                return GagalDaftar("NIS sudah dipakai oleh peserta lain. Gunakan NIS-mu sendiri.");

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                NamaLengkap = input.NamaLengkap.Trim(),
                Nis = nis,
                Kelas = string.IsNullOrWhiteSpace(input.Kelas) ? null : input.Kelas.Trim()
            };

            IdentityResult dibuat;
            try
            {
                dibuat = await users.CreateAsync(user, password);
            }
            catch (DbUpdateException)
            {
                // Balapan dengan pendaftaran lain yang memakai NIS sama.
                log.LogWarning("Pendaftaran {Email} gagal: NIS {Nis} sudah terpakai.", email, nis);
                return GagalDaftar("NIS sudah dipakai oleh peserta lain. Gunakan NIS-mu sendiri.");
            }

            if (!dibuat.Succeeded)
            {
                var pesan = string.Join(" ", dibuat.Errors.Select(e => e.Description));
                log.LogWarning("Pendaftaran {Email} gagal: {Alasan}", email, pesan);
                return GagalDaftar(pesan);
            }

            await users.AddToRoleAsync(user, DbSeeder.PesertaRole);
            await signIn.SignInAsync(user, isPersistent: true);

            return Results.Redirect(BersihkanReturnUrl(input.ReturnUrl) ?? "/kerjakan-ujian");
        })
        .DisableAntiforgery();

        app.MapPost("/account/logout", async Task<IResult> (
            HttpContext http,
            SignInManager<ApplicationUser> signIn) =>
        {
            await signIn.SignOutAsync();
            return Results.Redirect("/login");
        });
    }

    private static IResult GagalDaftar(string pesan) =>
        Results.Redirect($"/register?error={Uri.EscapeDataString(pesan)}");

    /// <summary>
    /// Validasi memakai atribut DataAnnotations pada model supaya aturannya
    /// hanya ada di satu tempat.
    /// </summary>
    private static List<ValidationResult>? Periksa(object model)
    {
        var hasil = new List<ValidationResult>();
        return Validator.TryValidateObject(model, new ValidationContext(model), hasil, validateAllProperties: true)
            ? null
            : hasil;
    }

    /// <summary>
    /// Hanya menerima path internal supaya tidak menjadi celah open redirect.
    /// Backslash ditolak karena peramban menormalkan "/\evil.com" menjadi
    /// "//evil.com" sehingga bisa berujung ke situs lain.
    /// </summary>
    private static string? BersihkanReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
            return null;

        if (returnUrl[0] != '/' || returnUrl.StartsWith("//", StringComparison.Ordinal))
            return null;

        if (returnUrl.Contains('\\') || returnUrl.Any(char.IsControl))
            return null;

        // Root biar tetap memakai tujuan bawaan per role.
        if (returnUrl == "/")
            return null;

        return returnUrl;
    }
}