namespace USMK.Api.Models;

/// <summary>
/// Catatan peserta yang keluar dari ruang ujian sebelum ujian dikumpulkan.
/// Setelah <see cref="BatasKeluar"/> kali keluar, peserta diblokir sampai
/// pengelola mereset catatannya.
/// </summary>
public class BlokiranPeserta
{
    /// <summary>Jumlah kali keluar yang menyebabkan peserta diblokir.</summary>
    public const int BatasKeluar = 3;

    public string UserId { get; set; } = "";

    public int JumlahKeluar { get; set; }

    public bool Diblokir { get; set; }

    public DateTime DiperbaruiPada { get; set; } = DateTime.Now;
}
