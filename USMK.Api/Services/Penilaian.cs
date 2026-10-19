using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using USMK.Api.Data;
using USMK.Api.Models;

namespace USMK.Api.Services;

/// <summary>
/// Mesin penilaian untuk paket ujian: menghitung batas waktu, menilai tiap soal
/// otomatis (berdasarkan kunci), dan merangkum skor 0-100 termasuk koreksi manual essay.
/// </summary>
public static class Penilaian
{
    /// <summary>
    /// Batas waktu pengerjaan = yang lebih awal antara (mulai + durasi) dan waktu tutup paket.
    /// Durasi 0/tanpa batas berarti batasnya waktu tutup paket.
    /// </summary>
    public static DateTime HitungBatas(PengerjaanUjian r)
    {
        var paket = r.PaketUjian!;
        var batasDurasi = paket.DurasiMenit > 0 ? r.WaktuMulai.AddMinutes(paket.DurasiMenit) : paket.WaktuTutup;
        return batasDurasi < paket.WaktuTutup ? batasDurasi : paket.WaktuTutup;
    }

    // ===== Penilaian per soal ===================================================

    private sealed class SoalUjian
    {
        public int SoalId { get; init; }
        public decimal Bobot { get; init; }
        public string Jenis { get; init; } = JenisSoal.PilihanGanda;
        public string? Kunci { get; init; }
        public int SkalaMin { get; init; } = 1;
        public int SkalaMax { get; init; } = 5;
        public HashSet<int> OpsiBenar { get; } = new();
        public List<(int OpsiId, int? NilaiBenar)> Baris { get; } = new();
        public HashSet<int> OpsiIds { get; } = new();
    }

    private sealed record JawabanSimpan(int? OpsiId, string? Teks, decimal? NilaiManual);

    public static bool Terisi(ButirSoal s, JawabanButir? j) => s.Jenis switch
    {
        JenisSoal.PilihanGanda or JenisSoal.Dropdown => j?.OpsiId is not null,
        JenisSoal.Checkbox => BacaDaftarOpsi(j?.JawabanTeks).Count > 0,
        JenisSoal.RateScale => !string.IsNullOrWhiteSpace(j?.JawabanTeks),
        JenisSoal.MatrixSkala => !string.IsNullOrWhiteSpace(j?.JawabanTeks),
        _ => !string.IsNullOrWhiteSpace(j?.JawabanTeks)
    };

    public static bool SudahDijawab(string jenis, JawabanButir? j)
    {
        if (j is null)
            return false;

        return jenis switch
        {
            JenisSoal.PilihanGanda or JenisSoal.Dropdown => j.OpsiId is not null,
            JenisSoal.Checkbox => BacaDaftarOpsi(j.JawabanTeks).Count > 0,
            _ => !string.IsNullOrWhiteSpace(j.JawabanTeks)
        };
    }

    /// <summary>Hasil otomatis per soal: null kalau soal tidak dinilai otomatis (essay / tanpa kunci).</summary>
    private static bool? Benar(SoalUjian s, JawabanSimpan? j)
    {
        if (!JenisSoal.OtomatisDinilai(s.Jenis))
            return null;                       // essay: dinilai manual oleh pengawas

        if (TanpaKunci(s))
            return null;                       // kunci tidak ada: tidak dihitung otomatis

        if (!TerisiBySoalUjian(s, j))
            return false;

        switch (s.Jenis)
        {
            case JenisSoal.PilihanGanda:
            case JenisSoal.Dropdown:
                return j!.OpsiId is not null && s.OpsiBenar.Contains(j.OpsiId.Value);

            case JenisSoal.Checkbox:
            {
                var dipilih = BacaDaftarOpsi(j!.Teks);
                return dipilih.Count > 0 && dipilih.SetEquals(s.OpsiBenar);
            }

            case JenisSoal.Angka:
            {
                var jawab = ParseDesimal(j!.Teks);
                var kunci = ParseDesimal(s.Kunci);
                return jawab is not null && kunci is not null && jawab == kunci;
            }

            case JenisSoal.Tanggal:
            {
                var jawab = ParseTanggal(j!.Teks);
                var kunci = ParseTanggal(s.Kunci);
                return jawab is not null && kunci is not null && jawab.Value.Date == kunci.Value.Date;
            }

            case JenisSoal.TeksSingkat:
            {
                var jawab = Normalisasi(j!.Teks);
                return s.Kunci!
                    .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Any(kunci => Normalisasi(kunci) == jawab);
            }

            case JenisSoal.RateScale:
                return int.TryParse(j!.Teks, out var nilaiRate)
                    && int.TryParse(s.Kunci, out var kunciRate)
                    && nilaiRate == kunciRate;

            case JenisSoal.MatrixSkala:
            {
                var jawab = BacaMatriks(j!.Teks);
                if (jawab.Count == 0)
                    return false;

                foreach (var (opsiId, nilaiBenar) in s.Baris)
                {
                    if (!jawab.TryGetValue(opsiId, out var nilai) || nilai != nilaiBenar)
                        return false;
                }

                return true;
            }

            default:
                return null;
        }
    }

    /// <summary>
    /// Jenis soal yang kuncinya hilang/tidak diisi dianggap tidak dinilai otomatis
    /// (tidak ikut pembilang maupun penyebut skor).
    /// </summary>
    private static bool TanpaKunci(SoalUjian s) => s.Jenis switch
    {
        JenisSoal.PilihanGanda or JenisSoal.Dropdown or JenisSoal.Checkbox => false,
        JenisSoal.MatrixSkala => s.Baris.Count == 0 || s.Baris.Any(b => b.NilaiBenar is null),
        _ => string.IsNullOrWhiteSpace(s.Kunci)
    };

    private static bool TerisiBySoalUjian(SoalUjian s, JawabanSimpan? j) => s.Jenis switch
    {
        JenisSoal.PilihanGanda or JenisSoal.Dropdown => j?.OpsiId is not null,
        JenisSoal.Checkbox => BacaDaftarOpsi(j?.Teks).Count > 0,
        JenisSoal.RateScale => !string.IsNullOrWhiteSpace(j?.Teks),
        JenisSoal.MatrixSkala => !string.IsNullOrWhiteSpace(j?.Teks),
        _ => !string.IsNullOrWhiteSpace(j?.Teks)
    };

    // ===== Muat data penilaian ==================================================

    private static async Task<List<SoalUjian>> MuatSoal(AppDbContext db, int paketUjianId)
    {
        var soals = await db.ButirSoals
            .Where(s => s.Bagian!.PaketUjianId == paketUjianId)
            .Select(s => new
            {
                s.Id,
                s.Jenis,
                s.JawabanBenar,
                s.Bobot,
                s.SkalaMin,
                s.SkalaMax,
                options = s.Options.Select(o => new { o.Id, o.IsBenar, o.NilaiBenar })
            })
            .ToListAsync();

        var daftar = soals.Select(s => new SoalUjian
        {
            SoalId = s.Id,
            Bobot = s.Bobot,
            Jenis = string.IsNullOrWhiteSpace(s.Jenis) ? JenisSoal.PilihanGanda : s.Jenis,
            Kunci = s.JawabanBenar,
            SkalaMin = s.SkalaMin,
            SkalaMax = s.SkalaMax
        }).ToList();

        foreach (var s in daftar)
        {
            var opsis = soals.First(x => x.Id == s.SoalId).options.OrderBy(x => x.Id).ToList();
            foreach (var o in opsis)
            {
                if (o.IsBenar)
                    s.OpsiBenar.Add(o.Id);
                s.OpsiIds.Add(o.Id);
                s.Baris.Add((o.Id, o.NilaiBenar));
            }
        }

        return daftar;
    }

    private static async Task<Dictionary<int, JawabanSimpan>> MuatJawaban(AppDbContext db, int pengerjaanId)
    {
        return await db.JawabanButirs
            .Where(j => j.PengerjaanUjianId == pengerjaanId)
            .Select(j => new { j.ButirSoalId, j.OpsiId, j.JawabanTeks, j.NilaiManual })
            .ToDictionaryAsync(j => j.ButirSoalId, j => new JawabanSimpan(j.OpsiId, j.JawabanTeks, j.NilaiManual));
    }

    // ===== Perhitungan skor =====================================================

    public sealed class Rincian
    {
        public decimal? Skor { get; init; }
        public int Benar { get; init; }
        public int Dijawab { get; init; }
        public int Total { get; init; }
        public int EssayDinilai { get; init; }
        public int EssayBelumDinilai { get; init; }
    }

    public static async Task<Rincian> HitungSkor(AppDbContext db, PengerjaanUjian pengerjaan)
    {
        var soals = await MuatSoal(db, pengerjaan.PaketUjianId);
        var jawaban = await MuatJawaban(db, pengerjaan.Id);

        var dijawab = 0;
        var benar = 0;
        decimal totalBobot = 0;
        decimal bobotDapat = 0;
        var essayDinilai = 0;
        var essayBelum = 0;

        foreach (var s in soals)
        {
            jawaban.TryGetValue(s.SoalId, out var j);

            if (s.Jenis == JenisSoal.TeksPanjang)
            {
                // Essay: ikut skor hanya kalau sudah dikoreksi manual.
                if (j?.NilaiManual is not null)
                {
                    essayDinilai++;
                    totalBobot += s.Bobot;
                    bobotDapat += Math.Clamp(j.NilaiManual.Value, 0, s.Bobot);
                }
                else if (SudahDijawab(s.Jenis, new JawabanButir { JawabanTeks = j?.Teks, OpsiId = j?.OpsiId }))
                {
                    essayBelum++;
                }
                continue;
            }

            if (TerisiBySoalUjian(s, j))
                dijawab++;

            var hasil = Benar(s, j);

            if (hasil is null)
                continue;                       // soal tanpa kunci: tidak ikut hitung

            totalBobot += s.Bobot;
            if (hasil.Value)
            {
                benar++;
                bobotDapat += s.Bobot;
            }
        }

        // Soal essay yang belum dikoreksi tidak ikut penyebut, jadi skor tetap 0-100.
        var skor = totalBobot == 0
            ? null
            : (decimal?)Math.Round(bobotDapat / totalBobot * 100, 2);

        return new Rincian
        {
            Skor = skor,
            Benar = benar,
            Dijawab = dijawab,
            Total = soals.Count,
            EssayDinilai = essayDinilai,
            EssayBelumDinilai = essayBelum
        };
    }

    public static async Task FinalisasiAsync(
        AppDbContext db,
        PengerjaanUjian pengerjaan,
        StatusPengerjaan status,
        DateTime waktuSelesai)
    {
        var rincian = await HitungSkor(db, pengerjaan);

        pengerjaan.Skor = rincian.Skor;
        pengerjaan.Status = status;
        pengerjaan.WaktuSelesai = waktuSelesai;

        await db.SaveChangesAsync();
    }

    // ===== Util parsing (sama dengan logika klasik) =============================

    private static string Normalisasi(string? teks) =>
        string.IsNullOrWhiteSpace(teks)
            ? ""
            : string.Join(" ", teks.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .ToLowerInvariant();

    private static decimal? ParseDesimal(string? teks) =>
        decimal.TryParse(teks, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d
        : decimal.TryParse(teks, out d) ? d
        : null;

    private static DateTime? ParseTanggal(string? teks) =>
        DateTime.TryParse(teks, CultureInfo.InvariantCulture, DateTimeStyles.None, out var t) ? t
        : DateTime.TryParse(teks, out t) ? t
        : null;

    public static HashSet<int> BacaDaftarOpsi(string? teks)
    {
        if (string.IsNullOrWhiteSpace(teks))
            return new HashSet<int>();

        try
        {
            var daftar = JsonSerializer.Deserialize<List<int>>(teks);
            return daftar is null ? new HashSet<int>() : daftar.ToHashSet();
        }
        catch (JsonException)
        {
            return new HashSet<int>();
        }
    }

    public static Dictionary<int, int> BacaMatriks(string? teks)
    {
        if (string.IsNullOrWhiteSpace(teks))
            return new Dictionary<int, int>();

        try
        {
            return JsonSerializer.Deserialize<Dictionary<int, int>>(teks)
                   ?? new Dictionary<int, int>();
        }
        catch (JsonException)
        {
            return new Dictionary<int, int>();
        }
    }
}