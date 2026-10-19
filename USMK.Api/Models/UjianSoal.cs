// Models/UjianSoal.cs
namespace USMK.Api.Models;

public class UjianSoal
{
    public int UjianId { get; set; }
    public int SoalId { get; set; }
    public int Urutan { get; set; }
    public decimal Bobot { get; set; } = 1;

    public Ujian? Ujian { get; set; }
    public Soal? Soal { get; set; }
}