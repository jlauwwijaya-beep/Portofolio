using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using USMK.Api.Data;
using USMK.Api.Dtos;
using USMK.Api.Models;
using USMK.Api.Services;

namespace USMK.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class PengerjaanController : ControllerBase
{
    private readonly AppDbContext _db;

    public PengerjaanController(AppDbContext db)
    {
        _db = db;
    }

    private sealed record OpsiInfo(int Id, string Teks, bool IsBenar, int? NilaiBenar);

    private const string PesanBlokir =
        "Akun Anda diblokir karena keluar dari ruang ujian 3 kali. Hubungi pengelola ujian untuk mereset akun Anda.";

    /// <summary>True kalau peserta sudah keluar dari ruang ujian sampai batas dan belum direset.</summary>
    private async Task<bool> DiblokirAsync(string userId) =>
        !string.IsNullOrWhiteSpace(userId)
        && await _db.BlokiranPesertas.AnyAsync(b => b.UserId == userId && b.Diblokir);

    private ObjectResult Diblokir() => StatusCode(StatusCodes.Status403Forbidden, PesanBlokir);

    /// <summary>Mencatat satu kali keluar dari ruang ujian; blokir terjadi pada keluar ketiga.</summary>
    [HttpPost("{id:int}/keluar")]
    public async Task<IActionResult> Keluar(int id)
    {
        var pengerjaan = await _db.PengerjaanUjians.FindAsync(id);

        if (pengerjaan == null)
            return NotFound("Pengerjaan tidak ditemukan.");

        // Ujian sudah dikumpulkan/dinilai: keluar tidak lagi dihitung.
        if (pengerjaan.Status != StatusPengerjaan.Berjalan)
            return Ok(new
            {
                userId = pengerjaan.UserId,
                jumlahKeluar = 0,
                diblokir = false,
                batas = BlokiranPeserta.BatasKeluar
            });

        var catatan = await _db.BlokiranPesertas.FindAsync(pengerjaan.UserId);

        if (catatan == null)
        {
            catatan = new BlokiranPeserta { UserId = pengerjaan.UserId };
            _db.BlokiranPesertas.Add(catatan);
        }

        catatan.JumlahKeluar++;
        catatan.Diblokir = catatan.JumlahKeluar >= BlokiranPeserta.BatasKeluar;
        catatan.DiperbaruiPada = DateTime.Now;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            userId = catatan.UserId,
            jumlahKeluar = catatan.JumlahKeluar,
            diblokir = catatan.Diblokir,
            batas = BlokiranPeserta.BatasKeluar
        });
    }

    [HttpPost("mulai")]
    public async Task<IActionResult> Mulai(MulaiKerjakanRequest request)
    {
        if (await DiblokirAsync(request.UserId))
            return Diblokir();

        var paket = await _db.PaketUjians.FindAsync(request.PaketUjianId);

        if (paket == null)
            return NotFound("Paket ujian tidak ditemukan.");
        if (!paket.IsPublished)
            return BadRequest("Paket ujian belum dipublikasikan.");

        var adaSoal = await _db.ButirSoals
            .AnyAsync(s => s.Bagian!.PaketUjianId == paket.Id);

        if (!adaSoal)
            return BadRequest("Paket ujian belum punya soal.");

        var sekarang = DateTime.Now;

        if (sekarang < paket.WaktuBuka)
            return BadRequest("Paket ujian belum dibuka.");
        if (sekarang >= paket.WaktuTutup)
            return BadRequest("Paket ujian sudah ditutup.");

        var pengerjaan = await _db.PengerjaanUjians
            .Include(r => r.PaketUjian)
            .FirstOrDefaultAsync(r => r.PaketUjianId == paket.Id && r.UserId == request.UserId);

        if (pengerjaan != null)
        {
            // Sudah pernah mulai: boleh lanjut kalau masih berjalan dan waktunya belum habis
            if (pengerjaan.Status != StatusPengerjaan.Berjalan)
                return Conflict("Kamu sudah mengerjakan paket ujian ini.");
            if (sekarang >= Penilaian.HitungBatas(pengerjaan))
                return Conflict("Waktu pengerjaan sudah habis.");
        }
        else
        {
            pengerjaan = new PengerjaanUjian
            {
                PaketUjianId = paket.Id,
                UserId = request.UserId,
                NamaSiswa = string.IsNullOrWhiteSpace(request.NamaSiswa) ? null : request.NamaSiswa.Trim(),
                Kelas = string.IsNullOrWhiteSpace(request.Kelas) ? null : request.Kelas.Trim(),
                WaktuMulai = sekarang,
                PaketUjian = paket,
                UrutanAcak = paket.AcakUrutan ? await BuatAcakanAsync(paket.Id) : null
            };

            _db.PengerjaanUjians.Add(pengerjaan);
            await _db.SaveChangesAsync();
        }

        var batas = Penilaian.HitungBatas(pengerjaan);

        return Ok(new
        {
            pengerjaanId = pengerjaan.Id,
            judul = paket.Judul,
            batasWaktu = batas,
            sisaDetik = SisaDetik(batas, sekarang)
        });
    }

    /// <summary>Detik tersisa sebelum batas, dibatasi ke rentang int agar tidak meluap untuk jadwal "tanpa batas" (BatasWaktu 2100).</summary>
    private static int SisaDetik(DateTime batas, DateTime sekarang) =>
        (int)Math.Clamp((batas - sekarang).TotalSeconds, 0, int.MaxValue / 2);

    /// <summary>Acak urutan soal di dalam tiap bagian, disimpan sebagai JSON per pengerjaan.</summary>
    private async Task<string?> BuatAcakanAsync(int paketUjianId)
    {
        var daftar = await _db.Bagians
            .Where(b => b.PaketUjianId == paketUjianId)
            .Select(b => new
            {
                b.Id,
                soalIds = b.Soals.Select(s => s.Id).ToList()
            })
            .ToListAsync();

        if (daftar.Count == 0)
            return null;

        var acakan = daftar.ToDictionary(
            b => b.Id.ToString(),
            b => b.soalIds.OrderBy(_ => Guid.NewGuid()).ToList());

        return JsonSerializer.Serialize(acakan);
    }

    private Dictionary<int, List<int>> BacaAcakan(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new Dictionary<int, List<int>>();

        try
        {
            return JsonSerializer.Deserialize<Dictionary<int, List<int>>>(json)
                   ?? new Dictionary<int, List<int>>();
        }
        catch (JsonException)
        {
            return new Dictionary<int, List<int>>();
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Soal(int id)
    {
        var pengerjaan = await _db.PengerjaanUjians
            .Include(r => r.PaketUjian)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (pengerjaan == null)
            return NotFound("Pengerjaan tidak ditemukan.");
        if (await DiblokirAsync(pengerjaan.UserId))
            return Diblokir();
        if (pengerjaan.Status != StatusPengerjaan.Berjalan)
            return Conflict("Pengerjaan ini sudah selesai.");

        var sekarang = DateTime.Now;
        var batas = Penilaian.HitungBatas(pengerjaan);

        if (sekarang >= batas)
        {
            await Penilaian.FinalisasiAsync(_db, pengerjaan, StatusPengerjaan.WaktuHabis, batas);
            return Conflict("Waktu pengerjaan sudah habis.");
        }

        var jawaban = await _db.JawabanButirs
            .Where(j => j.PengerjaanUjianId == id)
            .ToDictionaryAsync(j => j.ButirSoalId, j => j);

        var bagians = await _db.Bagians
            .Where(b => b.PaketUjianId == pengerjaan.PaketUjianId)
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
                        id = s.Id,
                        s.Teks,
                        jenis = string.IsNullOrWhiteSpace(s.Jenis) ? JenisSoal.Bawaan : s.Jenis,
                        s.Wajib,
                        s.Urutan,
                        s.SkalaMin,
                        s.SkalaMax,
                        s.LabelSkalaMin,
                        s.LabelSkalaMax,
                        options = s.Options.OrderBy(o => o.Urutan).Select(o => new { o.Id, o.Teks })
                    })
                    .ToList()
            })
            .ToListAsync();

        var acakan = BacaAcakan(pengerjaan.UrutanAcak);

        var hasil = bagians.Select(b => new
        {
            b.Id,
            b.Judul,
            b.Deskripsi,
            b.Urutan,
            soals = Urutkan(b.soals, acakan, b.Id, s => s.id).Select(s =>
            {
                jawaban.TryGetValue(s.id, out var j);
                var jenis = s.jenis;

                return new
                {
                    s.id,
                    s.Teks,
                    jenis,
                    s.Wajib,
                    s.Urutan,
                    s.SkalaMin,
                    s.SkalaMax,
                    s.LabelSkalaMin,
                    s.LabelSkalaMax,
                    s.options,
                    opsiId = jenis is JenisSoal.PilihanGanda or JenisSoal.Dropdown ? j?.OpsiId : null,
                    opsiIds = jenis == JenisSoal.Checkbox ? BacaDaftar(j?.JawabanTeks) : null,
                    teksJawaban = jenis is JenisSoal.TeksSingkat or JenisSoal.TeksPanjang or JenisSoal.Angka or JenisSoal.Tanggal
                        ? j?.JawabanTeks
                        : null,
                    nilai = jenis == JenisSoal.RateScale
                        ? (int.TryParse(j?.JawabanTeks, out var n) ? n : (int?)null)
                        : null,
                    matrix = jenis == JenisSoal.MatrixSkala ? BacaMatriks(j?.JawabanTeks) : null
                };
            })
        });

        return Ok(new
        {
            pengerjaanId = id,
            judul = pengerjaan.PaketUjian!.Judul,
            deskripsi = pengerjaan.PaketUjian.Deskripsi,
            mataPelajaran = pengerjaan.PaketUjian.MataPelajaran,
            kelas = pengerjaan.PaketUjian.Kelas,
            namaSiswa = pengerjaan.NamaSiswa,
            status = pengerjaan.Status.ToString(),
            batasWaktu = batas,
            sisaDetik = SisaDetik(batas, sekarang),
            bagians = hasil
        });
    }

    private static List<T> Urutkan<T>(IEnumerable<T> soal, Dictionary<int, List<int>> acakan, int bagianId, Func<T, int> pilihId)
    {
        if (acakan.TryGetValue(bagianId, out var urutan) && urutan.Count > 0)
            return soal.OrderBy(s => UrutanSoal(pilihId(s), urutan)).ToList();

        return soal.ToList();
    }

    private static int UrutanSoal(int id, List<int> urutan)
    {
        var i = urutan.IndexOf(id);
        return i < 0 ? int.MaxValue : i;
    }

    [HttpPut("{id:int}/jawaban")]
    public async Task<IActionResult> SimpanJawaban(int id, SimpanJawabanButirRequest request)
    {
        var pengerjaan = await _db.PengerjaanUjians
            .Include(r => r.PaketUjian)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (pengerjaan == null)
            return NotFound("Pengerjaan tidak ditemukan.");
        if (await DiblokirAsync(pengerjaan.UserId))
            return Diblokir();
        if (pengerjaan.Status != StatusPengerjaan.Berjalan)
            return Conflict("Pengerjaan ini sudah selesai.");

        var sekarang = DateTime.Now;
        var batas = Penilaian.HitungBatas(pengerjaan);

        if (sekarang >= batas)
        {
            await Penilaian.FinalisasiAsync(_db, pengerjaan, StatusPengerjaan.WaktuHabis, batas);
            return Conflict("Waktu pengerjaan sudah habis.");
        }

        var soal = await _db.ButirSoals
            .Include(s => s.Options)
            .Where(s => s.Id == request.ButirSoalId && s.Bagian!.PaketUjianId == pengerjaan.PaketUjianId)
            .FirstOrDefaultAsync();

        if (soal == null)
            return BadRequest("Soal ini bukan bagian dari paket ujian ini.");

        var jenis = string.IsNullOrWhiteSpace(soal.Jenis) ? JenisSoal.Bawaan : soal.Jenis;
        int? opsiId = null;
        string? jawabanTeks = null;

        switch (jenis)
        {
            case JenisSoal.PilihanGanda:
            case JenisSoal.Dropdown:
                if (request.OpsiId is not null)
                {
                    if (!soal.Options.Any(o => o.Id == request.OpsiId.Value))
                        return BadRequest("Pilihan tidak valid untuk soal ini.");
                    opsiId = request.OpsiId;
                }
                break;

            case JenisSoal.Checkbox:
            {
                var dipilih = (request.OpsiIds ?? new List<int>()).Distinct().ToList();

                if (dipilih.Any(pid => !soal.Options.Any(o => o.Id == pid)))
                    return BadRequest("Pilihan tidak valid untuk soal ini.");

                if (dipilih.Count > 0)
                    jawabanTeks = JsonSerializer.Serialize(dipilih);
                break;
            }

            case JenisSoal.RateScale:
            {
                if (request.Nilai is not null)
                {
                    if (request.Nilai < soal.SkalaMin || request.Nilai > soal.SkalaMax)
                        return BadRequest($"Nilai harus antara {soal.SkalaMin} sampai {soal.SkalaMax}.");
                    jawabanTeks = request.Nilai.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }
                break;
            }

            case JenisSoal.MatrixSkala:
            {
                var matrix = request.Matrix ?? new Dictionary<int, int>();
                var barisIds = soal.Options.Select(o => o.Id).ToHashSet();

                if (matrix.Keys.Any(k => !barisIds.Contains(k)))
                    return BadRequest("Baris matriks tidak valid untuk soal ini.");
                if (matrix.Values.Any(v => v < soal.SkalaMin || v > soal.SkalaMax))
                    return BadRequest($"Nilai matriks harus antara {soal.SkalaMin} sampai {soal.SkalaMax}.");

                if (matrix.Count > 0)
                    jawabanTeks = JsonSerializer.Serialize(matrix);
                break;
            }

            case JenisSoal.Angka:
            {
                var teks = request.Teks?.Trim();
                if (!string.IsNullOrEmpty(teks) && ParseDesimal(teks) is null)
                    return BadRequest("Jawaban harus berupa angka yang valid.");

                jawabanTeks = string.IsNullOrEmpty(teks) ? null : teks;
                break;
            }

            case JenisSoal.Tanggal:
            {
                var teks = request.Teks?.Trim();
                if (!string.IsNullOrEmpty(teks) && ParseTanggal(teks) is null)
                    return BadRequest("Jawaban harus berupa tanggal yang valid.");

                jawabanTeks = string.IsNullOrEmpty(teks) ? null : teks;
                break;
            }

            default:
            {
                var teks = request.Teks?.Trim();
                jawabanTeks = string.IsNullOrEmpty(teks) ? null : teks;
                break;
            }
        }

        var jawaban = await _db.JawabanButirs
            .FirstOrDefaultAsync(j => j.PengerjaanUjianId == id && j.ButirSoalId == request.ButirSoalId);

        if (jawaban == null)
        {
            _db.JawabanButirs.Add(new JawabanButir
            {
                PengerjaanUjianId = id,
                ButirSoalId = request.ButirSoalId,
                OpsiId = opsiId,
                JawabanTeks = jawabanTeks
            });
        }
        else
        {
            jawaban.OpsiId = opsiId;
            jawaban.JawabanTeks = jawabanTeks;
        }

        await _db.SaveChangesAsync();

        return Ok(new { pesan = "Jawaban tersimpan.", sisaDetik = SisaDetik(batas, sekarang) });
    }

    [HttpPost("{id:int}/selesai")]
    public async Task<IActionResult> Selesai(int id)
    {
        var pengerjaan = await _db.PengerjaanUjians
            .Include(r => r.PaketUjian)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (pengerjaan == null)
            return NotFound("Pengerjaan tidak ditemukan.");
        if (pengerjaan.Status != StatusPengerjaan.Berjalan)
            return Conflict("Pengerjaan ini sudah selesai.");

        var sekarang = DateTime.Now;
        var batas = Penilaian.HitungBatas(pengerjaan);

        if (sekarang >= batas)
        {
            await Penilaian.FinalisasiAsync(_db, pengerjaan, StatusPengerjaan.WaktuHabis, batas);
            return Ok(new
            {
                pesan = "Waktu pengerjaan habis, ujian dikumpulkan otomatis.",
                status = pengerjaan.Status.ToString(),
                skor = pengerjaan.Skor
            });
        }

        // Selesai manual: wajib cek daftar soal yang belum dijawab.
        var kosong = await SoalWajibKosong(id, pengerjaan.PaketUjianId);
        if (kosong.Count > 0)
            return BadRequest($"Masih ada {kosong.Count} soal wajib yang belum dijawab.");

        await Penilaian.FinalisasiAsync(_db, pengerjaan, StatusPengerjaan.Selesai, sekarang);

        return Ok(new
        {
            pesan = "Ujian selesai.",
            pengerjaanId = pengerjaan.Id,
            status = pengerjaan.Status.ToString(),
            skor = pengerjaan.Skor
        });
    }

    [HttpGet("{id:int}/hasil")]
    public async Task<IActionResult> Hasil(int id)
    {
        var pengerjaan = await _db.PengerjaanUjians
            .Include(r => r.PaketUjian)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (pengerjaan == null)
            return NotFound("Pengerjaan tidak ditemukan.");

        if (pengerjaan.Status == StatusPengerjaan.Berjalan)
        {
            var batas = Penilaian.HitungBatas(pengerjaan);

            if (DateTime.Now < batas)
                return Conflict("Ujian masih berjalan.");

            await Penilaian.FinalisasiAsync(_db, pengerjaan, StatusPengerjaan.WaktuHabis, batas);
        }

        var rincian = await Penilaian.HitungSkor(_db, pengerjaan);

        return Ok(new
        {
            judul = pengerjaan.PaketUjian!.Judul,
            status = pengerjaan.Status.ToString(),
            skor = pengerjaan.Skor,
            jumlahSoal = rincian.Total,
            jumlahDijawab = rincian.Dijawab,
            jumlahBenar = rincian.Benar,
            essayDinilai = rincian.EssayDinilai,
            essayBelumDinilai = rincian.EssayBelumDinilai,
            waktuMulai = pengerjaan.WaktuMulai,
            waktuSelesai = pengerjaan.WaktuSelesai
        });
    }

    // ===== Koreksi essay (admin) =================================================

    [HttpGet("{id:int}/koreksi")]
    public async Task<IActionResult> Koreksi(int id)
    {
        var pengerjaan = await _db.PengerjaanUjians
            .Include(r => r.PaketUjian)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (pengerjaan == null)
            return NotFound("Pengerjaan tidak ditemukan.");

        var jawaban = await _db.JawabanButirs
            .Where(j => j.PengerjaanUjianId == id)
            .ToDictionaryAsync(j => j.ButirSoalId, j => j);

        var opsiTeks = await _db.OpsiButirs
            .Where(o => o.ButirSoal!.Bagian!.PaketUjianId == pengerjaan.PaketUjianId)
            .ToDictionaryAsync(o => o.Id, o => o.Teks);

        var bagians = await _db.Bagians
            .Where(b => b.PaketUjianId == pengerjaan.PaketUjianId)
            .OrderBy(b => b.Urutan)
            .Select(b => new
            {
                b.Id,
                b.Judul,
                b.Urutan,
                soals = b.Soals.OrderBy(s => s.Urutan).Select(s => new
                {
                    id = s.Id,
                    s.Teks,
                    jenis = string.IsNullOrWhiteSpace(s.Jenis) ? JenisSoal.Bawaan : s.Jenis,
                    s.JawabanBenar,
                    s.Bobot,
                    s.Wajib,
                    options = s.Options.Select(o => new OpsiInfo(o.Id, o.Teks, o.IsBenar, o.NilaiBenar)).ToList()
                }).ToList()
            })
            .ToListAsync();

        var hasil = bagians.Select(b => new
        {
            b.Id,
            b.Judul,
            b.Urutan,
            soals = b.soals.Select(s =>
            {
                jawaban.TryGetValue(s.id, out var j);

                return new
                {
                    butirSoalId = s.id,
                    s.Teks,
                    s.jenis,
                    s.Wajib,
                    s.Bobot,
                    kunci = s.JawabanBenar,
                    jawaban = TampilkanJawaban(s.jenis, j, opsiTeks, s.options, s.JawabanBenar),
                    dijawab = Penilaian.SudahDijawab(s.jenis, j),
                    benar = s.jenis == JenisSoal.TeksPanjang
                        ? (bool?)null
                        : AksiBenar(s.jenis, j, s.options, s.JawabanBenar),
                    nilaiManual = j?.NilaiManual
                };
            })
        });

        return Ok(new
        {
            pengerjaanId = id,
            judul = pengerjaan.PaketUjian!.Judul,
            namaSiswa = pengerjaan.NamaSiswa,
            kelas = pengerjaan.Kelas,
            userId = pengerjaan.UserId,
            status = pengerjaan.Status.ToString(),
            skor = pengerjaan.Skor,
            bagians = hasil
        });
    }

    // ===== Hapus riwayat pengerjaan (admin) =====================================
    // Dipakai supaya siswa bisa mengerjakan ulang kalau gagal di tengah jalan.

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Hapus(int id)
    {
        var pengerjaan = await _db.PengerjaanUjians.FindAsync(id);

        if (pengerjaan == null)
            return NotFound("Pengerjaan tidak ditemukan.");

        var jawaban = await _db.JawabanButirs
            .Where(j => j.PengerjaanUjianId == id)
            .ToListAsync();

        _db.JawabanButirs.RemoveRange(jawaban);
        _db.PengerjaanUjians.Remove(pengerjaan);
        await _db.SaveChangesAsync();

        return Ok(new { pesan = "Riwayat pengerjaan dihapus. Siswa dapat mengerjakan ulang." });
    }

    [HttpPost("{id:int}/koreksi")]
    public async Task<IActionResult> SimpanKoreksi(int id, KoreksiRequest request)
    {
        var pengerjaan = await _db.PengerjaanUjians.FindAsync(id);

        if (pengerjaan == null)
            return NotFound("Pengerjaan tidak ditemukan.");

        var jawaban = await _db.JawabanButirs
            .FirstOrDefaultAsync(j => j.PengerjaanUjianId == id && j.ButirSoalId == request.ButirSoalId);

        // Koreksi bisa diisi walau soal essay belum dijawab (nilai 0 untuk kosong).
        if (jawaban == null)
        {
            jawaban = new JawabanButir
            {
                PengerjaanUjianId = id,
                ButirSoalId = request.ButirSoalId
            };
            _db.JawabanButirs.Add(jawaban);
        }

        jawaban.NilaiManual = Math.Clamp(request.Nilai, 0, 100);
        await _db.SaveChangesAsync();

        var rincian = await Penilaian.HitungSkor(_db, pengerjaan);

        pengerjaan.Skor = rincian.Skor;
        await _db.SaveChangesAsync();

        return Ok(new
        {
            pesan = "Nilai essay tersimpan.",
            skor = pengerjaan.Skor
        });
    }

    [HttpGet("{id:int}/preview")]
    public async Task<IActionResult> Preview(int id)
    {
        var pengerjaan = await _db.PengerjaanUjians
            .Include(r => r.PaketUjian)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (pengerjaan == null)
            return NotFound("Pengerjaan tidak ditemukan.");

        var jawaban = await _db.JawabanButirs
            .Where(j => j.PengerjaanUjianId == id)
            .ToDictionaryAsync(j => j.ButirSoalId, j => j);

        var opsiTeks = await _db.OpsiButirs
            .Where(o => o.ButirSoal!.Bagian!.PaketUjianId == pengerjaan.PaketUjianId)
            .ToDictionaryAsync(o => o.Id, o => o.Teks);

        var bagians = await _db.Bagians
            .Where(b => b.PaketUjianId == pengerjaan.PaketUjianId)
            .OrderBy(b => b.Urutan)
            .Select(b => new
            {
                b.Id,
                b.Judul,
                b.Deskripsi,
                b.Urutan,
                soals = b.Soals.OrderBy(s => s.Urutan).Select(s => new
                {
                    id = s.Id,
                    s.Teks,
                    jenis = string.IsNullOrWhiteSpace(s.Jenis) ? JenisSoal.Bawaan : s.Jenis,
                    s.Wajib,
                    s.Urutan,
                    options = s.Options.OrderBy(o => o.Urutan).Select(o => new OpsiInfo(o.Id, o.Teks, false, null)).ToList()
                }).ToList()
            })
            .ToListAsync();

        var acakan = BacaAcakan(pengerjaan.UrutanAcak);

        var hasil = bagians.Select(b => new
        {
            b.Id,
            b.Judul,
            b.Deskripsi,
            b.Urutan,
            soals = Urutkan(b.soals, acakan, b.Id, s => s.id).Select(s =>
            {
                jawaban.TryGetValue(s.id, out var j);
                return new
                {
                    s.id,
                    s.Teks,
                    s.jenis,
                    s.Wajib,
                    s.Urutan,
                    jawaban = TampilkanJawaban(s.jenis, j, opsiTeks, s.options, kunci: null),
                    dijawab = Penilaian.SudahDijawab(s.jenis, j)
                };
            })
        });

        return Ok(new
        {
            pengerjaanId = id,
            judul = pengerjaan.PaketUjian!.Judul,
            mataPelajaran = pengerjaan.PaketUjian.MataPelajaran,
            kelas = pengerjaan.PaketUjian.Kelas,
            namaSiswa = pengerjaan.NamaSiswa,
            userId = pengerjaan.UserId,
            status = pengerjaan.Status.ToString(),
            skor = pengerjaan.Skor,
            waktuMulai = pengerjaan.WaktuMulai,
            waktuSelesai = pengerjaan.WaktuSelesai,
            bagians = hasil
        });
    }

    [HttpGet("riwayat")]
    public async Task<IActionResult> Riwayat([FromQuery] string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest("UserId wajib diisi.");

        var data = await _db.PengerjaanUjians
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.WaktuMulai)
            .Select(r => new
            {
                pengerjaanId = r.Id,
                paketUjianId = r.PaketUjianId,
                judul = r.PaketUjian!.Judul,
                mataPelajaran = r.PaketUjian.MataPelajaran,
                status = r.Status.ToString(),
                r.WaktuMulai,
                r.WaktuSelesai,
                r.Skor
            })
            .ToListAsync();

        return Ok(data);
    }

    private async Task<List<int>> SoalWajibKosong(int pengerjaanId, int paketUjianId)
    {
        var wajib = await _db.ButirSoals
            .Where(s => s.Wajib && s.Bagian!.PaketUjianId == paketUjianId)
            .Select(s => s.Id)
            .ToListAsync();

        if (wajib.Count == 0)
            return new List<int>();

        var dijawab = await _db.JawabanButirs
            .Where(j => j.PengerjaanUjianId == pengerjaanId && wajib.Contains(j.ButirSoalId))
            .Where(j => j.OpsiId != null || (j.JawabanTeks != null && j.JawabanTeks != ""))
            .Select(j => j.ButirSoalId)
            .ToListAsync();

        return wajib.Except(dijawab).ToList();
    }

    // ===== Util ==================================================================

    private static string TampilkanJawaban(
        string jenis,
        JawabanButir? j,
        Dictionary<int, string> opsiTeks,
        List<OpsiInfo> options,
        string? kunci)
    {
        if (!Penilaian.SudahDijawab(jenis, j ?? new JawabanButir()))
            return "(tidak dijawab)";

        switch (jenis)
        {
            case JenisSoal.PilihanGanda:
            case JenisSoal.Dropdown:
                return j!.OpsiId is not null && opsiTeks.TryGetValue(j.OpsiId.Value, out var t) ? t : "(tidak dijawab)";

            case JenisSoal.Checkbox:
            {
                var ids = Penilaian.BacaDaftarOpsi(j!.JawabanTeks);
                var teks = ids.Where(opsiTeks.ContainsKey).Select(id => opsiTeks[id]).ToList();
                return teks.Count > 0 ? string.Join(", ", teks) : "(tidak dijawab)";
            }

            case JenisSoal.RateScale:
                return $"{j!.JawabanTeks?.Trim() ?? "-"} dari rentang skala";

            case JenisSoal.MatrixSkala:
            {
                var matrix = Penilaian.BacaMatriks(j!.JawabanTeks);

                if (matrix.Count == 0)
                    return "(tidak dijawab)";

                var baris = new List<string>();
                foreach (var kv in matrix)
                {
                    var label = opsiTeks.TryGetValue(kv.Key, out var teksBaris) ? teksBaris : $"#{kv.Key}";
                    baris.Add($"{label} = {kv.Value}");
                }
                return string.Join("; ", baris);
            }

            default:
                return j?.JawabanTeks ?? "(tidak dijawab)";
        }
    }

    private static bool? AksiBenar(
        string jenis,
        JawabanButir? j,
        List<OpsiInfo> options,
        string? kunci)
    {
        if (!Penilaian.SudahDijawab(jenis, j ?? new JawabanButir()))
            return false;

        switch (jenis)
        {
            case JenisSoal.PilihanGanda:
            case JenisSoal.Dropdown:
            {
                var benar = options.FirstOrDefault(o => o.IsBenar);
                return benar is not null && benar.Id == j!.OpsiId;
            }

            case JenisSoal.Checkbox:
            {
                var idsBenar = options.Where(o => o.IsBenar).Select(o => o.Id).ToHashSet();
                return Penilaian.BacaDaftarOpsi(j!.JawabanTeks).SetEquals(idsBenar);
            }

            case JenisSoal.RateScale:
                return int.TryParse(j!.JawabanTeks, out var nilai) && nilai.ToString() == (kunci ?? "");

            case JenisSoal.Angka:
                return ParseDesimal(j!.JawabanTeks) == ParseDesimal(kunci) && ParseDesimal(kunci) is not null;

            case JenisSoal.Tanggal:
                return ParseTanggal(j!.JawabanTeks) is { } t1
                    && ParseTanggal(kunci) is { } t2
                    && t1.Date == t2.Date;

            case JenisSoal.TeksSingkat:
            {
                var jawab = Normalisasi(j!.JawabanTeks);
                return (kunci ?? "")
                    .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Any(k => Normalisasi(k) == jawab);
            }

            case JenisSoal.MatrixSkala:
            {
                var matrix = Penilaian.BacaMatriks(j!.JawabanTeks);
                foreach (var o in options)
                {
                    if (o.NilaiBenar is null)
                        return null;
                    if (!matrix.TryGetValue(o.Id, out var nilaiMatriks) || nilaiMatriks != o.NilaiBenar)
                        return false;
                }
                return matrix.Count > 0;
            }

            default:
                return null;
        }
    }

    private static List<int> BacaDaftar(string? teks) =>
        Penilaian.BacaDaftarOpsi(teks).ToList();

    private static Dictionary<int, int> BacaMatriks(string? teks) =>
        Penilaian.BacaMatriks(teks);

    private static string Normalisasi(string? teks) =>
        string.IsNullOrWhiteSpace(teks)
            ? ""
            : string.Join(" ", teks.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();

    private static decimal? ParseDesimal(string? teks) =>
        decimal.TryParse(teks, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d
        : decimal.TryParse(teks, out d) ? d
        : null;

    private static DateTime? ParseTanggal(string? teks) =>
        DateTime.TryParse(teks, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var t) ? t
        : DateTime.TryParse(teks, out t) ? t
        : null;
}