using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using USMK.Web.Data;

namespace USMK.Web.Services;

/// <summary>
/// Menambahkan klaim profil (nama lengkap, NIS, kelas) ke dalam cookie sesi
/// supaya tidak perlu query database setiap kali halaman dirender.
/// Perubahan data profil berlaku setelah pengguna login ulang.
/// </summary>
public sealed class UsmkClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>(userManager, roleManager, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        Tambahkan(identity, "nama_lengkap", user.NamaLengkap);
        Tambahkan(identity, "nis", user.Nis);
        Tambahkan(identity, "kelas", user.Kelas);

        return identity;
    }

    private static void Tambahkan(ClaimsIdentity identity, string tipe, string? nilai)
    {
        if (!string.IsNullOrWhiteSpace(nilai))
            identity.AddClaim(new Claim(tipe, nilai));
    }
}