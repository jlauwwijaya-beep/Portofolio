namespace USMK.Api.Models;

/// <summary>
/// Daftar jenis soal yang didukung. Disimpan sebagai teks di kolom Soal.Jenis
/// supaya gampang ditambah tanpa harus mengubah tipe enum di database.
/// </summary>
public static class JenisSoal
{
    public const string PilihanGanda = "PilihanGanda";
    public const string Dropdown = "Dropdown";
    public const string Checkbox = "Checkbox";
    public const string TeksSingkat = "TeksSingkat";
    public const string TeksPanjang = "TeksPanjang";
    public const string Angka = "Angka";
    public const string Tanggal = "Tanggal";
    public const string RateScale = "RateScale";
    public const string MatrixSkala = "MatrixSkala";

    public const string Bawaan = PilihanGanda;

    public static readonly string[] Semua =
    {
        PilihanGanda, Dropdown, Checkbox, TeksSingkat, TeksPanjang,
        Angka, Tanggal, RateScale, MatrixSkala
    };

    public static bool Dikenal(string? jenis) =>
        !string.IsNullOrWhiteSpace(jenis) && Semua.Contains(jenis.Trim(), StringComparer.Ordinal);

    public static string Normalisasi(string? jenis) =>
        string.IsNullOrWhiteSpace(jenis) ? Bawaan : jenis.Trim();

    /// <summary>Soal yang memakai daftar opsi (pilihan ganda, dropdown, checkbox, matriks).</summary>
    public static bool PakaiOpsi(string jenis) =>
        jenis is PilihanGanda or Dropdown or Checkbox or MatrixSkala;

    /// <summary>Soal yang bisa dinilai otomatis oleh sistem.</summary>
    public static bool OtomatisDinilai(string jenis) => jenis != TeksPanjang;

    public static bool ButuhSkala(string jenis) => jenis is RateScale or MatrixSkala;
}
