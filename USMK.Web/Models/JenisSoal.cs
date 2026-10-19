namespace USMK.Web.Models;

/// <summary>Daftar jenis soal yang didukung, dipakai bersama oleh API dan Web.</summary>
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

    public static readonly (string Nilai, string Label)[] Pilihan =
    {
        (PilihanGanda, "Pilihan ganda (radio button)"),
        (Dropdown, "Dropdown (combo box)"),
        (Checkbox, "Checkbox (bisa pilih lebih dari satu)"),
        (TeksSingkat, "Teks singkat"),
        (TeksPanjang, "Teks panjang (essay)"),
        (Angka, "Angka"),
        (Tanggal, "Tanggal"),
        (RateScale, "Rate scale (skala penilaian)"),
        (MatrixSkala, "Matrix skala (baris x skala)")
    };

    public static string Label(string? jenis) =>
        Pilihan.FirstOrDefault(p => p.Nilai == (jenis ?? Bawaan)).Label;

    public static string Singkat(string? jenis) => (jenis ?? Bawaan) switch
    {
        Dropdown => "Dropdown",
        Checkbox => "Checkbox",
        TeksSingkat => "Teks singkat",
        TeksPanjang => "Essay",
        Angka => "Angka",
        Tanggal => "Tanggal",
        RateScale => "Rate scale",
        MatrixSkala => "Matrix skala",
        _ => "Pilihan ganda"
    };

    public static bool Dikenal(string? jenis) =>
        jenis is PilihanGanda or Dropdown or Checkbox or TeksSingkat or TeksPanjang
            or Angka or Tanggal or RateScale or MatrixSkala;

    public static bool PakaiOpsi(string? jenis) =>
        jenis is PilihanGanda or Dropdown or Checkbox or MatrixSkala;

    public static bool BanyakBenar(string? jenis) => jenis == Checkbox;

    public static bool ButuhSkala(string? jenis) => jenis is RateScale or MatrixSkala;

    public static bool ButuhBaris(string? jenis) => jenis == MatrixSkala;

    public static bool OtomatisDinilai(string? jenis) => jenis != TeksPanjang;

    public static bool Isian(string? jenis) =>
        jenis is TeksSingkat or TeksPanjang or Angka or Tanggal;
}
