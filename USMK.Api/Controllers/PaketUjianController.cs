using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using USMK.Api.Data;
using USMK.Api.Dtos;
using USMK.Api.Models;
using USMK.Api.Services;

namespace USMK.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class PaketUjianController : ControllerBase
{
    private readonly AppDbContext _db;

    public PaketUjianController(AppDbContext db)
    {
        _db = db;
    }

    // ===== Master data paket ujian ================================================

    [HttpGet]
    public async Task<IActionResult> Daftar()
    {
        var sekarang = DateTime.Now;

        var data = await _db.PaketUjians
            .OrderByDescending(p => p.Id)
            .Select(p => new
            {
                p.Id,
                p.Judul,
                p.Deskripsi,
                p.MataPelajaran,
                p.Kelas,
                p.IsPublished,
                p.DibuatPada,
                p.DurasiMenit,
                p.WaktuBuka,
                p.WaktuTutup,
                p.AcakUrutan,
                jumlahBagian = p.Bagians.Count,
                jumlahSoal = p.Bagians.SelectMany(b => b.Soals).Count(),
                jumlahPengerjaan = _db.PengerjaanUjians.Count(r => r.PaketUjianId == p.Id)
            })
            .ToListAsync();

        // Status jadwal dihitung di server supaya konsisten dengan waktu pemeriksaan saat mulai.
        var hasil = data.Select(p => new
        {
            p.Id,
            p.Judul,
            p.Deskripsi,
            p.MataPelajaran,
            p.Kelas,
            p.IsPublished,
            p.DibuatPada,
            p.DurasiMenit,
            p.WaktuBuka,
            p.WaktuTutup,
            p.AcakUrutan,
            p.jumlahBagian,
            p.jumlahSoal,
            p.jumlahPengerjaan,
            statusJadwal = !p.IsPublished ? "Draft"
                : sekarang < p.WaktuBuka ? "BelumDibuka"
                : sekarang >= p.WaktuTutup ? "SudahDitutup"
                : "Dibuka"
        });

        return Ok(hasil);
    }

    [HttpPost]
    public async Task<IActionResult> Buat(BuatPaketRequest request)
    {
        var galat = ValidasiJadwal(request);
        if (galat != null)
            return BadRequest(galat);

        var paket = new PaketUjian
        {
            Judul = request.Judul.Trim(),
            Deskripsi = Bersih(request.Deskripsi),
            MataPelajaran = Bersih(request.MataPelajaran),
            Kelas = Bersih(request.Kelas),
            DurasiMenit = request.DurasiMenit,
            WaktuBuka = request.WaktuBuka,
            WaktuTutup = request.WaktuTutup,
            AcakUrutan = request.AcakUrutan
        };

        _db.PaketUjians.Add(paket);
        await _db.SaveChangesAsync();

        return Ok(new { paket.Id, pesan = "Paket ujian berhasil dibuat." });
    }

    private static string? ValidasiJadwal(BuatPaketRequest request)
    {
        if (request.WaktuBuka == default)
            request.WaktuBuka = new DateTime(2000, 1, 1);
        if (request.WaktuTutup == default)
            request.WaktuTutup = new DateTime(2100, 1, 1);

        if (request.WaktuTutup <= request.WaktuBuka)
            return "Waktu tutup harus setelah waktu buka.";
        if (request.DurasiMenit < 0)
            return "Durasi tidak boleh negatif.";

        return null;
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detail(int id)
    {
        var paket = await _db.PaketUjians
            .Where(p => p.Id == id)
            .Select(p => new
            {
                p.Id,
                p.Judul,
                p.Deskripsi,
                p.MataPelajaran,
                p.Kelas,
                p.IsPublished,
                p.DibuatPada,
                p.DurasiMenit,
                p.WaktuBuka,
                p.WaktuTutup,
                p.AcakUrutan,
                bagians = p.Bagians
                    .OrderBy(b => b.Urutan)
                    .Select(b => new
                    {
                        b.Id,
                        b.Judul,
                        b.Deskripsi,
                        b.Urutan,
                                soals = b.Soals
                                    .OrderBy(s => s.Urutan)
                                    .Select(s => new
                                    {
                                        s.Id,
                                        s.Teks,
                                        s.Jenis,
                                        s.Wajib,
                                        s.Urutan,
                                        s.SkalaMin,
                                        s.SkalaMax,
                                        s.LabelSkalaMin,
                                        s.LabelSkalaMax,
                                        s.JawabanBenar,
                                        s.Bobot,
                                        options = s.Options
                                            .OrderBy(o => o.Urutan)
                                            .Select(o => new { o.Id, o.Teks, o.Urutan, o.IsBenar, o.NilaiBenar })
                                    })
                    })
            })
            .FirstOrDefaultAsync();

        if (paket == null)
            return NotFound("Paket ujian tidak ditemukan.");

        return Ok(paket);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Ubah(int id, BuatPaketRequest request)
    {
        var paket = await _db.PaketUjians.FindAsync(id);

        if (paket == null)
            return NotFound("Paket ujian tidak ditemukan.");

        var galat = ValidasiJadwal(request);
        if (galat != null)
            return BadRequest(galat);

        paket.Judul = request.Judul.Trim();
        paket.Deskripsi = Bersih(request.Deskripsi);
        paket.MataPelajaran = Bersih(request.MataPelajaran);
        paket.Kelas = Bersih(request.Kelas);
        paket.DurasiMenit = request.DurasiMenit;
        paket.WaktuBuka = request.WaktuBuka;
        paket.WaktuTutup = request.WaktuTutup;
        paket.AcakUrutan = request.AcakUrutan;

        await _db.SaveChangesAsync();

        return Ok(new { paket.Id, pesan = "Paket ujian berhasil diperbarui." });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Hapus(int id)
    {
        var paket = await _db.PaketUjians.FindAsync(id);

        if (paket == null)
            return NotFound("Paket ujian tidak ditemukan.");

        var adaPengerjaan = await _db.PengerjaanUjians.AnyAsync(r => r.PaketUjianId == id);
        if (adaPengerjaan)
            return Conflict("Paket ujian tidak bisa dihapus karena sudah ada siswa yang mengerjakan.");

        _db.PaketUjians.Remove(paket);
        await _db.SaveChangesAsync();

        return Ok(new { pesan = "Paket ujian berhasil dihapus." });
    }

    [HttpPost("{id:int}/duplikat")]
    public async Task<IActionResult> Duplikat(int id)
    {
        var sumber = await _db.PaketUjians
            .Include(p => p.Bagians)
            .ThenInclude(b => b.Soals)
            .ThenInclude(s => s.Options)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (sumber == null)
            return NotFound("Paket ujian tidak ditemukan.");

        var salinan = new PaketUjian
        {
            Judul = JudulSalinan(sumber.Judul),
            Deskripsi = sumber.Deskripsi,
            MataPelajaran = sumber.MataPelajaran,
            Kelas = sumber.Kelas,
            IsPublished = false,
            DibuatPada = DateTime.UtcNow,
            DurasiMenit = sumber.DurasiMenit,
            WaktuBuka = sumber.WaktuBuka,
            WaktuTutup = sumber.WaktuTutup,
            AcakUrutan = sumber.AcakUrutan,
            Bagians = sumber.Bagians
                .OrderBy(b => b.Urutan)
                .Select(b => new Bagian
                {
                    Judul = b.Judul,
                    Deskripsi = b.Deskripsi,
                    Urutan = b.Urutan,
                    Soals = b.Soals
                        .OrderBy(s => s.Urutan)
                        .Select(s => new ButirSoal
                        {
                            Teks = s.Teks,
                            Jenis = s.Jenis,
                            Wajib = s.Wajib,
                            Urutan = s.Urutan,
                            SkalaMin = s.SkalaMin,
                            SkalaMax = s.SkalaMax,
                            LabelSkalaMin = s.LabelSkalaMin,
                            LabelSkalaMax = s.LabelSkalaMax,
                            JawabanBenar = s.JawabanBenar,
                            Bobot = s.Bobot,
                            Options = s.Options
                                .OrderBy(o => o.Urutan)
                                .Select(o => new OpsiButir
                                {
                                    Teks = o.Teks,
                                    Urutan = o.Urutan,
                                    IsBenar = o.IsBenar,
                                    NilaiBenar = o.NilaiBenar
                                })
                                .ToList()
                        })
                        .ToList()
                })
                .ToList()
        };

        _db.PaketUjians.Add(salinan);
        await _db.SaveChangesAsync();

        return Ok(new { id = salinan.Id, pesan = "Paket ujian berhasil diduplikasi sebagai draft." });
    }

    /// <summary>Judul salinan: tambah " (Salinan)" tanpa menumpuk dari salinan sebelumnya.</summary>
    private static string JudulSalinan(string judul)
    {
        const string sufiks = " (Salinan)";
        var dasar = judul.Trim();

        if (dasar.EndsWith(sufiks))
            dasar = dasar[..^sufiks.Length].TrimEnd();

        var hasil = dasar + sufiks;
        return hasil.Length > 200 ? hasil[..200] : hasil;
    }

    [HttpPost("{id:int}/publish")]
    public async Task<IActionResult> Publish(int id)
    {
        var paket = await _db.PaketUjians
            .Include(p => p.Bagians)
            .ThenInclude(b => b.Soals)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (paket == null)
            return NotFound("Paket ujian tidak ditemukan.");

        if (paket.IsPublished)
            return Conflict("Paket ujian sudah dipublikasikan.");

        if (paket.Bagians.SelectMany(b => b.Soals).Count() == 0)
            return BadRequest("Paket ujian belum punya soal.");

        if (paket.WaktuTutup <= DateTime.Now)
            return BadRequest("Waktu tutup sudah lewat. Ubah jadwal dulu sebelum publikasi.");

        paket.IsPublished = true;
        await _db.SaveChangesAsync();

        return Ok(new { pesan = "Paket ujian berhasil dipublikasikan." });
    }

    [HttpPost("{id:int}/tutup")]
    public async Task<IActionResult> Tutup(int id)
    {
        var paket = await _db.PaketUjians.FindAsync(id);

        if (paket == null)
            return NotFound("Paket ujian tidak ditemukan.");

        if (!paket.IsPublished)
            return Conflict("Paket ujian belum dipublikasikan.");

        paket.IsPublished = false;
        await _db.SaveChangesAsync();

        return Ok(new { pesan = "Paket ujian dikembalikan ke draft." });
    }

    [HttpGet("{id:int}/peserta")]
    public async Task<IActionResult> Peserta(int id)
    {
        var paket = await _db.PaketUjians.FindAsync(id);

        if (paket == null)
            return NotFound("Paket ujian tidak ditemukan.");

        var pengerjaan = await _db.PengerjaanUjians
            .Where(r => r.PaketUjianId == id)
            .OrderByDescending(r => r.WaktuMulai)
            .Select(r => new
            {
                r.Id,
                r.UserId,
                r.NamaSiswa,
                r.Kelas,
                status = r.Status.ToString(),
                r.WaktuMulai,
                r.WaktuSelesai,
                r.Skor,
                jumlahDijawab = r.Jawabans.Count
            })
            .ToListAsync();

        var sekarang = DateTime.Now;

        var daftar = pengerjaan.Select(r =>
        {
            // Batas waktu = yang lebih awal antara (mulai + durasi) dan waktu tutup paket
            var batas = paket.DurasiMenit > 0 ? r.WaktuMulai.AddMinutes(paket.DurasiMenit) : paket.WaktuTutup;
            if (batas > paket.WaktuTutup) batas = paket.WaktuTutup;

            return new
            {
                pengerjaanId = r.Id,
                r.UserId,
                r.NamaSiswa,
                r.Kelas,
                status = r.status,
                r.WaktuMulai,
                r.WaktuSelesai,
                r.Skor,
                jumlahDijawab = r.jumlahDijawab,
                belumDitutup = r.status == StatusPengerjaan.Berjalan.ToString() && sekarang >= batas
            };
        }).ToList();

        var skors = daftar.Where(r => r.Skor.HasValue).Select(r => r.Skor!.Value).ToList();

        return Ok(new
        {
            paket = new
            {
                paket.Id,
                paket.Judul,
                paket.MataPelajaran,
                paket.Kelas,
                paket.DurasiMenit,
                paket.WaktuBuka,
                paket.WaktuTutup
            },
            jumlahPengerjaan = daftar.Count,
            jumlahDinilai = skors.Count,
            rataRata = skors.Count > 0 ? Math.Round(skors.Average(), 2) : (decimal?)null,
            tertinggi = skors.Count > 0 ? skors.Max() : (decimal?)null,
            terendah = skors.Count > 0 ? skors.Min() : (decimal?)null,
            pengerjaan = daftar
        });
    }

    /// <summary>Unduh rekap nilai peserta paket sebagai file Excel (.xlsx).</summary>
    [HttpGet("{id:int}/peserta/ekspor")]
    public async Task<IActionResult> EksporRekap(int id)
    {
        var paket = await _db.PaketUjians.FindAsync(id);

        if (paket == null)
            return NotFound("Paket ujian tidak ditemukan.");

        var pengerjaan = await _db.PengerjaanUjians
            .Where(r => r.PaketUjianId == id)
            .OrderByDescending(r => r.WaktuMulai)
            .Select(r => new
            {
                r.Id,
                r.UserId,
                r.NamaSiswa,
                r.Kelas,
                status = r.Status.ToString(),
                r.WaktuMulai,
                r.WaktuSelesai,
                r.Skor,
                jumlahDijawab = r.Jawabans.Count
            })
            .ToListAsync();

        var sekarang = DateTime.Now;

        var daftar = pengerjaan.Select(r =>
        {
            var batas = paket.DurasiMenit > 0 ? r.WaktuMulai.AddMinutes(paket.DurasiMenit) : paket.WaktuTutup;
            if (batas > paket.WaktuTutup) batas = paket.WaktuTutup;

            return new EksporBaris(
                r.UserId,
                r.NamaSiswa ?? "",
                r.Kelas ?? "",
                r.status,
                r.WaktuMulai,
                r.WaktuSelesai,
                r.Skor,
                r.jumlahDijawab,
                r.status == StatusPengerjaan.Berjalan.ToString() && sekarang >= batas);
        }).ToList();

        var skors = daftar.Where(r => r.Skor.HasValue).Select(r => r.Skor!.Value).ToList();

        var file = BuatRekapXlsx(
            paket.Judul,
            paket.MataPelajaran,
            paket.Kelas,
            paket.DurasiMenit,
            daftar,
            skors.Count > 0 ? Math.Round(skors.Average(), 2) : (decimal?)null,
            skors.Count > 0 ? skors.Max() : (decimal?)null,
            skors.Count > 0 ? skors.Min() : (decimal?)null);

        return File(file,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Rekap-{Slug(paket.Judul)}.xlsx");
    }

    /// <summary>Tutup dan nilai peserta yang waktunya sudah habis tapi masih berjalan.</summary>
    [HttpPost("{id:int}/tutup-kedaluwarsa")]
    public async Task<IActionResult> TutupKedaluwarsa(int id, TutupKedaluwarsaRequest request)
    {
        var paket = await _db.PaketUjians.FindAsync(id);

        if (paket == null)
            return NotFound("Paket ujian tidak ditemukan.");

        var ids = (request.PengerjaanIds ?? new List<int>()).Distinct().ToList();

        var daftar = await _db.PengerjaanUjians
            .Where(r => r.PaketUjianId == id
                && ids.Contains(r.Id)
                && r.Status == StatusPengerjaan.Berjalan)
            .Include(r => r.PaketUjian)
            .ToListAsync();

        var sekarang = DateTime.Now;
        var ditutup = 0;

        foreach (var r in daftar)
        {
            if (sekarang < Penilaian.HitungBatas(r))
                continue;

            await Penilaian.FinalisasiAsync(_db, r, StatusPengerjaan.WaktuHabis, Penilaian.HitungBatas(r));
            ditutup++;
        }

        return Ok(new { pesan = $"{ditutup} peserta ditutup dan dinilai.", ditutup });
    }

    // ===== Bagian ==================================================================

    [HttpPost("{id:int}/bagian")]
    public async Task<IActionResult> TambahBagian(int id, UpsertBagianRequest request)
    {
        var paket = await _db.PaketUjians.FindAsync(id);

        if (paket == null)
            return NotFound("Paket ujian tidak ditemukan.");
        if (paket.IsPublished)
            return Conflict("Paket ujian sudah dipublikasikan, bagian tidak bisa ditambah.");

        var urutan = await _db.Bagians
            .Where(b => b.PaketUjianId == id)
            .Select(b => (int?)b.Urutan)
            .MaxAsync() ?? 0;

        var bagian = new Bagian
        {
            PaketUjianId = id,
            Judul = request.Judul.Trim(),
            Deskripsi = Bersih(request.Deskripsi),
            Urutan = urutan + 1
        };

        _db.Bagians.Add(bagian);
        await _db.SaveChangesAsync();

        return Ok(new { bagian.Id, pesan = "Bagian berhasil ditambahkan." });
    }

    [HttpPut("bagian/{bagianId:int}")]
    public async Task<IActionResult> UbahBagian(int bagianId, UpsertBagianRequest request)
    {
        var bagian = await _db.Bagians
            .Include(b => b.PaketUjian)
            .FirstOrDefaultAsync(b => b.Id == bagianId);

        if (bagian == null)
            return NotFound("Bagian tidak ditemukan.");
        if (bagian.PaketUjian!.IsPublished)
            return Conflict("Paket ujian sudah dipublikasikan, bagian tidak bisa diubah.");

        bagian.Judul = request.Judul.Trim();
        bagian.Deskripsi = Bersih(request.Deskripsi);

        await _db.SaveChangesAsync();

        return Ok(new { bagian.Id, pesan = "Bagian berhasil diperbarui." });
    }

    [HttpDelete("bagian/{bagianId:int}")]
    public async Task<IActionResult> HapusBagian(int bagianId)
    {
        var bagian = await _db.Bagians
            .Include(b => b.PaketUjian)
            .FirstOrDefaultAsync(b => b.Id == bagianId);

        if (bagian == null)
            return NotFound("Bagian tidak ditemukan.");
        if (bagian.PaketUjian!.IsPublished)
            return Conflict("Paket ujian sudah dipublikasikan, bagian tidak bisa dihapus.");

        var paketId = bagian.PaketUjianId;

        _db.Bagians.Remove(bagian);
        await _db.SaveChangesAsync();

        await RapikanBagianAsync(paketId);

        return Ok(new { pesan = "Bagian berhasil dihapus." });
    }

    // ===== Soal ====================================================================

    [HttpPost("bagian/{bagianId:int}/soal")]
    public async Task<IActionResult> TambahSoal(int bagianId, UpsertSoalRequest request)
    {
        var bagian = await _db.Bagians
            .Include(b => b.PaketUjian)
            .FirstOrDefaultAsync(b => b.Id == bagianId);

        if (bagian == null)
            return NotFound("Bagian tidak ditemukan.");
        if (bagian.PaketUjian!.IsPublished)
            return Conflict("Paket ujian sudah dipublikasikan, soal tidak bisa ditambah.");

        var (jenis, galat) = SoalRules.Baca(request.Jenis);
        if (galat != null)
            return BadRequest(galat);

        galat = SoalRules.Cek(request, jenis);
        if (galat != null)
            return BadRequest(galat);

        var urutan = await _db.ButirSoals
            .Where(s => s.BagianId == bagianId)
            .Select(s => (int?)s.Urutan)
            .MaxAsync() ?? 0;

        var soal = IsiSoal(new ButirSoal { BagianId = bagianId, Urutan = urutan + 1 }, request, jenis);

        _db.ButirSoals.Add(soal);
        await _db.SaveChangesAsync();

        return Ok(new { soal.Id, pesan = "Soal berhasil ditambahkan." });
    }

    [HttpPut("soal/{soalId:int}")]
    public async Task<IActionResult> UbahSoal(int soalId, UpsertSoalRequest request)
    {
        var soal = await _db.ButirSoals
            .Include(s => s.Options)
            .Include(s => s.Bagian)
            .ThenInclude(b => b!.PaketUjian)
            .FirstOrDefaultAsync(s => s.Id == soalId);

        if (soal == null)
            return NotFound("Soal tidak ditemukan.");
        if (soal.Bagian!.PaketUjian!.IsPublished)
            return Conflict("Paket ujian sudah dipublikasikan, soal tidak bisa diubah.");

        var (jenis, galat) = SoalRules.Baca(request.Jenis);
        if (galat != null)
            return BadRequest(galat);

        galat = SoalRules.Cek(request, jenis);
        if (galat != null)
            return BadRequest(galat);

        var opsiLama = soal.Options.ToList();

        IsiSoal(soal, request, jenis);

        _db.OpsiButirs.RemoveRange(opsiLama);

        if (JenisSoal.PakaiOpsi(jenis))
            soal.Options = OpsiDariRequest(request, soal.Id);

        await _db.SaveChangesAsync();

        return Ok(new { soal.Id, pesan = "Soal berhasil diperbarui." });
    }

    [HttpDelete("soal/{soalId:int}")]
    public async Task<IActionResult> HapusSoal(int soalId)
    {
        var soal = await _db.ButirSoals
            .Include(s => s.Bagian)
            .ThenInclude(b => b!.PaketUjian)
            .FirstOrDefaultAsync(s => s.Id == soalId);

        if (soal == null)
            return NotFound("Soal tidak ditemukan.");
        if (soal.Bagian!.PaketUjian!.IsPublished)
            return Conflict("Paket ujian sudah dipublikasikan, soal tidak bisa dihapus.");

        var bagianId = soal.BagianId;

        _db.ButirSoals.Remove(soal);
        await _db.SaveChangesAsync();

        await RapikanSoalAsync(bagianId);

        return Ok(new { pesan = "Soal berhasil dihapus." });
    }

    // ===== Util ====================================================================

    private static string? Bersih(string? teks) => string.IsNullOrWhiteSpace(teks) ? null : teks.Trim();

    private async Task RapikanBagianAsync(int paketId)
    {
        var bagians = await _db.Bagians
            .Where(b => b.PaketUjianId == paketId)
            .OrderBy(b => b.Urutan)
            .ToListAsync();

        var urutan = 1;
        foreach (var b in bagians)
            b.Urutan = urutan++;

        await _db.SaveChangesAsync();
    }

    private async Task RapikanSoalAsync(int bagianId)
    {
        var soals = await _db.ButirSoals
            .Where(s => s.BagianId == bagianId)
            .OrderBy(s => s.Urutan)
            .ToListAsync();

        var urutan = 1;
        foreach (var s in soals)
            s.Urutan = urutan++;

        await _db.SaveChangesAsync();
    }

    private static ButirSoal IsiSoal(ButirSoal soal, UpsertSoalRequest request, string jenis)
    {
        soal.Teks = request.Teks.Trim();
        soal.Jenis = jenis;
        soal.Wajib = request.Wajib;
        soal.SkalaMin = request.SkalaMin ?? 1;
        soal.SkalaMax = request.SkalaMax ?? 5;
        soal.LabelSkalaMin = Bersih(request.LabelSkalaMin);
        soal.LabelSkalaMax = Bersih(request.LabelSkalaMax);
        soal.JawabanBenar = Bersih(request.JawabanBenar);
        soal.Bobot = request.Bobot > 0 ? request.Bobot : 1;

        if (soal.Id == 0 && JenisSoal.PakaiOpsi(jenis))
            soal.Options = OpsiDariRequest(request, 0);

        return soal;
    }

    private static List<OpsiButir> OpsiDariRequest(UpsertSoalRequest request, int soalId) =>
        (request.Options ?? new List<OpsiButirRequest>())
            .Where(o => !string.IsNullOrWhiteSpace(o.Teks))
            .Select((o, i) => new OpsiButir
            {
                ButirSoalId = soalId,
                Teks = o.Teks.Trim(),
                Urutan = i + 1,
                IsBenar = o.IsBenar,
                NilaiBenar = o.NilaiBenar
            })
            .ToList();

    // ---------- Ekspor Excel ----------

    private sealed record EksporBaris(
        string UserId, string Nama, string Kelas, string Status,
        DateTime WaktuMulai, DateTime? WaktuSelesai, decimal? Skor,
        int JumlahDijawab, bool BelumDitutup);

    private static string Slug(string judul)
    {
        var bersih = new StringBuilder();
        foreach (var ch in judul)
        {
            bersih.Append(char.IsLetterOrDigit(ch) ? ch : '-');
        }
        var hasil = bersih.ToString().Trim('-');
        return hasil.Length > 40 ? hasil[..40] : hasil.Length == 0 ? "paket" : hasil;
    }

    private static string LabelStatusEkspor(EksporBaris b) =>
        b.BelumDitutup ? "Belum ditutup"
        : b.Status == "Selesai" ? "Selesai"
        : b.Status == "WaktuHabis" ? "Waktu habis"
        : "Mengerjakan";

    private static void TulisSelTeks(XmlWriter x, string? teks)
    {
        x.WriteStartElement("c");
        x.WriteAttributeString("t", "inlineStr");
        x.WriteStartElement("is");
        x.WriteElementString("t", teks ?? "");
        x.WriteEndElement();
        x.WriteEndElement();
    }

    private static void TulisSelAngka(XmlWriter x, string nilai) =>
        x.WriteRaw($"<c><v>{nilai}</v></c>");

    private static byte[] BuatRekapXlsx(
        string judul, string? mapel, string? kelasPaket, int durasi,
        List<EksporBaris> baris, decimal? rata, decimal? tertinggi, decimal? terendah)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            TulisEntri(zip, "[Content_Types].xml",
                "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
                "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/></Types>");

            TulisEntri(zip, "_rels/.rels",
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");

            TulisEntri(zip, "xl/workbook.xml",
                "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
                "<sheets><sheet name=\"Rekap\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");

            TulisEntri(zip, "xl/_rels/workbook.xml.rels",
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/></Relationships>");

            TulisEntri(zip, "xl/worksheets/sheet1.xml", PakaiWorksheet(judul, mapel, kelasPaket, durasi, baris, rata, tertinggi, terendah));
        }
        return ms.ToArray();
    }

    private static void TulisEntri(ZipArchive zip, string nama, string isi)
    {
        var entri = zip.CreateEntry(nama);
        using var sw = new StreamWriter(entri.Open(), new UTF8Encoding(false));
        sw.Write(isi);
    }

    private static string PakaiWorksheet(
        string judul, string? mapel, string? kelasPaket, int durasi,
        List<EksporBaris> baris, decimal? rata, decimal? tertinggi, decimal? terendah)
    {
        var sb = new StringBuilder();
        using (var x = XmlWriter.Create(sb, new XmlWriterSettings { OmitXmlDeclaration = true }))
        {
            x.WriteStartElement("worksheet", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
            x.WriteStartElement("sheetData");

            TulisBaris(x, $"Rekap Hasil Ujian: {judul}");
            TulisBaris(x,
                $"Mata pelajaran: {mapel ?? "-"} | Kelas: {kelasPaket ?? "-"} | Durasi: {(durasi > 0 ? durasi + " menit" : "tanpa batas")}");
            TulisBaris(x, "");

            TulisBaris(x, "No", "Nama Siswa", "User ID", "Kelas", "Status", "Skor", "Dijawab", "Mulai", "Selesai");

            var no = 1;
            foreach (var b in baris)
            {
                var urut = no++;
                TulisBarisAksi(x, sel =>
                {
                    TulisSelAngka(sel, urut.ToString());
                    TulisSelTeks(sel, b.Nama);
                    TulisSelTeks(sel, b.UserId);
                    TulisSelTeks(sel, b.Kelas);
                    TulisSelTeks(sel, LabelStatusEkspor(b));
                    TulisSelAngka(sel, b.Skor?.ToString("0.##", CultureInfo.InvariantCulture) ?? "-");
                    TulisSelAngka(sel, b.JumlahDijawab.ToString());
                    TulisSelTeks(sel, b.WaktuMulai.ToString("dd-MM-yyyy HH:mm"));
                    TulisSelTeks(sel, b.WaktuSelesai?.ToString("dd-MM-yyyy HH:mm") ?? "-");
                });
            }

            TulisBaris(x, "");
            TulisBarisAksi(x, sel => { TulisSelTeks(sel, "Jumlah Pengerjaan"); TulisSelAngka(sel, baris.Count.ToString()); });
            TulisBarisAksi(x, sel => { TulisSelTeks(sel, "Sudah Dinilai"); TulisSelAngka(sel, baris.Count(b => b.Skor.HasValue).ToString()); });
            TulisBarisAksi(x, sel => { TulisSelTeks(sel, "Rata-rata"); TulisSelAngka(sel, rata?.ToString("0.##", CultureInfo.InvariantCulture) ?? "-"); });
            TulisBarisAksi(x, sel => { TulisSelTeks(sel, "Tertinggi"); TulisSelAngka(sel, tertinggi?.ToString("0.##", CultureInfo.InvariantCulture) ?? "-"); });
            TulisBarisAksi(x, sel => { TulisSelTeks(sel, "Terendah"); TulisSelAngka(sel, terendah?.ToString("0.##", CultureInfo.InvariantCulture) ?? "-"); });

            x.WriteEndElement();
            x.WriteEndElement();
        }
        return sb.ToString();
    }

    private static void TulisBaris(XmlWriter x, params string[] teks)
    {
        x.WriteStartElement("row");
        foreach (var t in teks)
            TulisSelTeks(x, t);
        x.WriteEndElement();
    }

    private static void TulisBarisAksi(XmlWriter x, Action<XmlWriter> tulis)
    {
        x.WriteStartElement("row");
        tulis(x);
        x.WriteEndElement();
    }
}
