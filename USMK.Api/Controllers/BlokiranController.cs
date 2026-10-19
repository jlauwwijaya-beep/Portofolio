using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using USMK.Api.Data;
using USMK.Api.Models;

namespace USMK.Api.Controllers;

/// <summary>
/// Catatan peserta yang keluar dari ruang ujian. Peserta yang mencapai
/// <see cref="BlokiranPeserta.BatasKeluar"/> kali keluar diblokir sampai
/// pengelola memanggil reset.
/// </summary>
[ApiController]
[Route("[controller]")]
public class BlokiranController : ControllerBase
{
    private readonly AppDbContext _db;

    public BlokiranController(AppDbContext db)
    {
        _db = db;
    }

    private sealed record BlokiranDto(string UserId, int JumlahKeluar, bool Diblokir, DateTime DiperbaruiPada);

    private static BlokiranDto Ringkas(BlokiranPeserta b) =>
        new(b.UserId, b.JumlahKeluar, b.Diblokir, b.DiperbaruiPada);

    /// <summary>Semua catatan keluar, terbaru lebih dulu — dipakai halaman pengelola.</summary>
    [HttpGet]
    public async Task<IActionResult> Daftar() =>
        Ok(await _db.BlokiranPesertas
            .OrderByDescending(b => b.Diblokir)
            .ThenByDescending(b => b.DiperbaruiPada)
            .Select(b => new BlokiranDto(b.UserId, b.JumlahKeluar, b.Diblokir, b.DiperbaruiPada))
            .ToListAsync());

    /// <summary>Catatan satu peserta. Belum ada catatan tetap dikembalikan sebagai nilai nol.</summary>
    [HttpGet("{userId}")]
    public async Task<IActionResult> Cek(string userId)
    {
        var catatan = await _db.BlokiranPesertas.FindAsync(userId);

        return catatan is null
            ? Ok(new BlokiranDto(userId, 0, false, DateTime.MinValue))
            : Ok(Ringkas(catatan));
    }

    /// <summary>
    /// Reset catatan peserta: blokir dilepas sekaligus catatannya dihapus supaya
    /// tidak menumpuk. Peserta mulai dari nol bila keluar lagi.
    /// </summary>
    [HttpPost("{userId}/reset")]
    public async Task<IActionResult> Reset(string userId)
    {
        var catatan = await _db.BlokiranPesertas.FindAsync(userId);

        if (catatan is null)
            return NotFound("Tidak ada catatan pelanggaran untuk pengguna ini.");

        _db.BlokiranPesertas.Remove(catatan);
        await _db.SaveChangesAsync();

        return Ok(new BlokiranDto(userId, 0, false, DateTime.Now));
    }
}
