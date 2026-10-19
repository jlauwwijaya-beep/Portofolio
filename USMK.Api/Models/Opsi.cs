// Models/Opsi.cs
namespace USMK.Api.Models;

public class Opsi
{
    public int Id { get; set; }
    public int SoalId { get; set; }
    public string Teks { get; set; } = "";
    public bool IsBenar { get; set; }

    /// <summary>
    /// Nilai benar untuk baris MatrixSkala (berapa kolom skala yang benar).
    /// Pada soal pilihan ganda/dropdown/checkbox tidak dipakai.
    /// </summary>
    public int? NilaiBenar { get; set; }

    public Soal? Soal { get; set; }
}
