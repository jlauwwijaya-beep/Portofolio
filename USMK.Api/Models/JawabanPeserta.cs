// Models/JawabanPeserta.cs
namespace USMK.Api.Models;

public class JawabanPeserta
{
    public int Id { get; set; }
    public int PengisianId { get; set; }
    public int SoalId { get; set; }

    /// <summary>Terisi untuk pilihan ganda dan dropdown; null untuk jenis lain.</summary>
    public int? OpsiId { get; set; }

    /// <summary>
    /// Jawaban berbasis teks: teks singkat/panjang, angka, tanggal, nilai rate
    /// ("4"), daftar opsi checkbox ("[12,15]") dan jawaban matriks ({"12":3}).
    /// </summary>
    public string? JawabanTeks { get; set; }

    public Pengisian? Pengisian { get; set; }
}
