using System.ComponentModel.DataAnnotations;

namespace USMK.Api.Dtos;

public class BuatPaketRequest
{
    [Required(ErrorMessage = "Judul paket ujian wajib diisi")]
    [StringLength(200)]
    public string Judul { get; set; } = "";

    [StringLength(1000)]
    public string? Deskripsi { get; set; }

    [StringLength(120)]
    public string? MataPelajaran { get; set; }

    [StringLength(60)]
    public string? Kelas { get; set; }

    /// <summary>0 atau kurang = tanpa batas durasi (berlaku sampai waktu tutup).</summary>
    [Range(0, 600)]
    public int DurasiMenit { get; set; }

    public DateTime WaktuBuka { get; set; }

    public DateTime WaktuTutup { get; set; }

    public bool AcakUrutan { get; set; } = true;
}

public class UpsertBagianRequest
{
    [Required(ErrorMessage = "Judul bagian wajib diisi")]
    [StringLength(200)]
    public string Judul { get; set; } = "";

    [StringLength(1000)]
    public string? Deskripsi { get; set; }
}

public class OpsiButirRequest
{
    [Required(ErrorMessage = "Teks pilihan wajib diisi")]
    public string Teks { get; set; } = "";

    public bool IsBenar { get; set; }

    public int? NilaiBenar { get; set; }
}

public class UpsertSoalRequest
{
    [Required(ErrorMessage = "Teks soal wajib diisi")]
    [StringLength(2000)]
    public string Teks { get; set; } = "";

    public string? Jenis { get; set; }

    public bool Wajib { get; set; }

    public List<OpsiButirRequest> Options { get; set; } = new();

    [Range(1, 100)]
    public int? SkalaMin { get; set; }

    [Range(1, 100)]
    public int? SkalaMax { get; set; }

    [StringLength(60)]
    public string? LabelSkalaMin { get; set; }

    [StringLength(60)]
    public string? LabelSkalaMax { get; set; }

    [StringLength(2000)]
    public string? JawabanBenar { get; set; }

    [Range(0.1, 100)]
    public decimal Bobot { get; set; } = 1;
}

public class MulaiKerjakanRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "PaketUjianId wajib diisi")]
    public int PaketUjianId { get; set; }

    [Required(ErrorMessage = "UserId wajib diisi")]
    public string UserId { get; set; } = "";

    [StringLength(120)]
    public string? NamaSiswa { get; set; }

    [StringLength(60)]
    public string? Kelas { get; set; }
}

public class SimpanJawabanButirRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "ButirSoalId wajib diisi")]
    public int ButirSoalId { get; set; }

    public int? OpsiId { get; set; }

    public List<int>? OpsiIds { get; set; }

    public string? Teks { get; set; }

    public int? Nilai { get; set; }

    public Dictionary<int, int>? Matrix { get; set; }
}

public class KoreksiRequest
{
    [Range(1, int.MaxValue)]
    public int ButirSoalId { get; set; }

    [Range(0, 100)]
    public decimal Nilai { get; set; }
}

public class TutupKedaluwarsaRequest
{
    public List<int> PengerjaanIds { get; set; } = new();
}
