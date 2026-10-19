// Models/Ujian.cs
namespace USMK.Api.Models;

public class Ujian
{
    public int Id { get; set; }
    public string Judul { get; set; } = "";
    public string? Deskripsi { get; set; }
    public int DurasiMenit { get; set; }
    public DateTime WaktuBuka { get; set; }
    public DateTime WaktuTutup { get; set; }
    public bool IsPublished { get; set; }

    public List<UjianSoal> UjianSoals { get; set; } = new();
}