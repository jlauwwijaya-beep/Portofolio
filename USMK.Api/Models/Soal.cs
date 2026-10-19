// Models/Soal.cs
namespace USMK.Api.Models;

public class Soal
{
    public int Id { get; set; }
    public string Teks { get; set; } = "";
    public string? Kategori { get; set; }
    public DateTime DibuatPada { get; set; } = DateTime.UtcNow;

    /// <summary>Jenis soal, salah satu dari <see cref="JenisSoal"/>.</summary>
    public string Jenis { get; set; } = JenisSoal.Bawaan;

    /// <summary>
    /// Kunci jawaban untuk soal isian: teks singkat, angka, tanggal, rate scale.
    /// Untuk essay (TeksPanjang) bersifat opsional sebagai panduan pensilinan.
    /// </summary>
    public string? JawabanBenar { get; set; }

    // Rentang skala untuk RateScale dan MatrixSkala
    public int SkalaMin { get; set; } = 1;
    public int SkalaMax { get; set; } = 5;
    public string? LabelSkalaMin { get; set; }
    public string? LabelSkalaMax { get; set; }

    public List<Opsi> Opsis { get; set; } = new();
}
