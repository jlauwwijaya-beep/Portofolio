// Models/Pengisian.cs
namespace USMK.Api.Models;

public enum StatusPengisian
{
    Berjalan,
    Selesai,
    WaktuHabis
}

public class Pengisian
{
    public int Id { get; set; }
    public int UjianId { get; set; }
    public string UserId { get; set; } = "";
    public DateTime WaktuMulai { get; set; }
    public DateTime? WaktuSelesai { get; set; }
    public decimal? Skor { get; set; }
    public StatusPengisian Status { get; set; } = StatusPengisian.Berjalan;

    public Ujian? Ujian { get; set; }
    public List<JawabanPeserta> Jawabans { get; set; } = new();
    public string? UrutanSoal { get; set; }
}