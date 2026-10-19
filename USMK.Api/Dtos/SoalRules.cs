using System.Globalization;
using USMK.Api.Models;

namespace USMK.Api.Dtos;

/// <summary>
/// Validasi khusus tiap jenis soal, dipakai oleh PaketUjianController.
/// Pesan dikembalikan dalam Bahasa Indonesia dan langsung dipakai sebagai BadRequest message.
/// </summary>
public static class SoalRules
{
    public static (string Jenis, string? Galat) Baca(string? jenisInput)
    {
        if (string.IsNullOrWhiteSpace(jenisInput))
            return (JenisSoal.Bawaan, null);

        var jenis = jenisInput.Trim();
        return JenisSoal.Dikenal(jenis)
            ? (jenis, null)
            : (jenis, $"Jenis soal '{jenis}' tidak dikenal.");
    }

    public static string? Skala(int min, int max)
    {
        if (min < 1)
            return "Skala minimal harus lebih besar dari 0.";
        if (max < min)
            return "Skala maksimal tidak boleh lebih kecil dari skala minimal.";
        if (max > 100)
            return "Skala maksimal terlalu besar (maksimal 100).";

        return null;
    }

    /// <summary>Validasi isi soal beserta kunci jawabannya.</summary>
    public static string? Cek(UpsertSoalRequest r, string jenis)
    {
        var opsis = r.Options ?? new List<OpsiButirRequest>();
        string? galatOpsi;

        if (string.IsNullOrWhiteSpace(r.Teks))
            return "Teks soal wajib diisi.";

        if (r.Bobot <= 0)
            return "Bobot soal harus lebih besar dari 0.";

        switch (jenis)
        {
            case JenisSoal.PilihanGanda:
            case JenisSoal.Dropdown:
                galatOpsi = CekOpsi(opsis, 2);
                if (galatOpsi != null)
                    return galatOpsi;
                if (opsis.Count(o => o.IsBenar) != 1)
                    return "Harus ada tepat satu jawaban yang benar.";
                return null;

            case JenisSoal.Checkbox:
                galatOpsi = CekOpsi(opsis, 2);
                if (galatOpsi != null)
                    return galatOpsi;
                if (!opsis.Any(o => o.IsBenar))
                    return "Pilih minimal satu jawaban yang benar.";
                return null;

            case JenisSoal.MatrixSkala:
            {
                var galatSkala = Skala(r.SkalaMin ?? 1, r.SkalaMax ?? 5);
                if (galatSkala != null)
                    return galatSkala;
                if (opsis.Count < 1)
                    return "Minimal satu baris untuk matriks.";
                if (opsis.Any(o => string.IsNullOrWhiteSpace(o.Teks)))
                    return "Semua baris matriks harus diisi.";

                var min = r.SkalaMin ?? 1;
                var max = r.SkalaMax ?? 5;
                if (opsis.Any(o => o.NilaiBenar is not null && (o.NilaiBenar < min || o.NilaiBenar > max)))
                    return $"Jawaban benar tiap baris harus antara {min} sampai {max}.";
                return null;
            }

            case JenisSoal.RateScale:
            {
                var galatSkala = Skala(r.SkalaMin ?? 1, r.SkalaMax ?? 5);
                if (galatSkala != null)
                    return galatSkala;
                if (string.IsNullOrWhiteSpace(r.JawabanBenar))
                    return null;             // kunci opsional: kosong berarti tidak dinilai
                if (!int.TryParse(r.JawabanBenar.Trim(), out var nilai))
                    return "Kunci jawaban harus berupa angka.";
                if (nilai < (r.SkalaMin ?? 1) || nilai > (r.SkalaMax ?? 5))
                    return $"Kunci jawaban harus antara {r.SkalaMin ?? 1} sampai {r.SkalaMax ?? 5}.";
                return null;
            }

            case JenisSoal.Angka:
                if (string.IsNullOrWhiteSpace(r.JawabanBenar))
                    return "Kunci jawaban wajib diisi.";
                if (!decimal.TryParse(r.JawabanBenar.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                    return "Kunci jawaban harus berupa angka yang valid.";
                return null;

            case JenisSoal.Tanggal:
                if (string.IsNullOrWhiteSpace(r.JawabanBenar))
                    return null;             // kunci opsional: kosong berarti tidak dinilai
                if (!DateTime.TryParse(r.JawabanBenar.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
                    && !DateTime.TryParse(r.JawabanBenar.Trim(), out _))
                    return "Kunci jawaban harus berupa tanggal yang valid.";
                return null;

            case JenisSoal.TeksSingkat:
                if (string.IsNullOrWhiteSpace(r.JawabanBenar))
                    return "Kunci jawaban wajib diisi.";
                return null;

            case JenisSoal.TeksPanjang:
                // Essay: kunci bersifat opsional (panduan pensilinan), tanpa opsi.
                return null;

            default:
                return $"Jenis soal '{jenis}' tidak dikenal.";
        }
    }

    /// <summary>Validasi daftar opsi: jumlah minimal dan tidak boleh ada yang kosong.</summary>
    private static string? CekOpsi(List<OpsiButirRequest> daftar, int minimal)
    {
        if (daftar.Count < minimal)
            return $"Minimal {minimal} pilihan jawaban.";
        if (daftar.Any(o => string.IsNullOrWhiteSpace(o.Teks)))
            return "Semua pilihan jawaban harus diisi.";
        return null;
    }
}
