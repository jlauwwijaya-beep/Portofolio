namespace USMK.Api.Models;

public class PaketUjian
{
    public int Id { get; set; }
    public string Judul { get; set; } = "";
    public string? Deskripsi { get; set; }
    public string? MataPelajaran { get; set; }
    public string? Kelas { get; set; }
    public bool IsPublished { get; set; }
    public DateTime DibuatPada { get; set; } = DateTime.UtcNow;

    /// <summary>Durasi pengerjaan dalam menit. 0 atau kurang = tanpa batas durasi (sampai waktu tutup).</summary>
    public int DurasiMenit { get; set; }

    public DateTime WaktuBuka { get; set; } = new(2000, 1, 1);
    public DateTime WaktuTutup { get; set; } = new(2100, 1, 1);

    /// <summary>Acak urutan soal di dalam tiap bagian untuk tiap peserta.</summary>
    public bool AcakUrutan { get; set; } = true;

    public List<Bagian> Bagians { get; set; } = new();
}

public class Bagian
{
    public int Id { get; set; }
    public int PaketUjianId { get; set; }
    public string Judul { get; set; } = "";
    public string? Deskripsi { get; set; }
    public int Urutan { get; set; }

    public PaketUjian? PaketUjian { get; set; }
    public List<ButirSoal> Soals { get; set; } = new();
}

public class ButirSoal
{
    public int Id { get; set; }
    public int BagianId { get; set; }
    public string Teks { get; set; } = "";
    public string Jenis { get; set; } = JenisSoal.Bawaan;
    public bool Wajib { get; set; }
    public int Urutan { get; set; }
    public int SkalaMin { get; set; } = 1;
    public int SkalaMax { get; set; } = 5;
    public string? LabelSkalaMin { get; set; }
    public string? LabelSkalaMax { get; set; }

    /// <summary>
    /// Kunci jawaban untuk soal isian (teks singkat, angka, tanggal, rate scale).
    /// Untuk essay (TeksPanjang) bersifat opsional sebagai panduan pensilinan.
    /// </summary>
    public string? JawabanBenar { get; set; }

    /// <summary>Bobot penilaian soal ini relatif terhadap soal lain dalam paket.</summary>
    public decimal Bobot { get; set; } = 1;

    public Bagian? Bagian { get; set; }
    public List<OpsiButir> Options { get; set; } = new();
}

public class OpsiButir
{
    public int Id { get; set; }
    public int ButirSoalId { get; set; }
    public string Teks { get; set; } = "";
    public int Urutan { get; set; }

    /// <summary>Jawaban benar untuk pilihan ganda/dropdown/checkbox.</summary>
    public bool IsBenar { get; set; }

    /// <summary>Nilai benar baris MatrixSkala (kolom skala yang benar).</summary>
    public int? NilaiBenar { get; set; }

    public ButirSoal? ButirSoal { get; set; }
}

public enum StatusPengerjaan
{
    Berjalan,
    Selesai,
    WaktuHabis
}

public class PengerjaanUjian
{
    public int Id { get; set; }
    public int PaketUjianId { get; set; }
    public string UserId { get; set; } = "";
    public string? NamaSiswa { get; set; }
    public string? Kelas { get; set; }
    public DateTime WaktuMulai { get; set; }
    public DateTime? WaktuSelesai { get; set; }
    public StatusPengerjaan Status { get; set; } = StatusPengerjaan.Berjalan;

    /// <summary>Skor 0-100 hasil penilaian otomatis + koreksi manual. Null kalau belum dinilai.</summary>
    public decimal? Skor { get; set; }

    /// <summary>
    /// Urutan soal acak per peserta dalam format JSON: {"&lt;bagianId&gt;": [butirId, ...]}.
    /// Null kalau paket memakai urutan bawaan.
    /// </summary>
    public string? UrutanAcak { get; set; }

    public PaketUjian? PaketUjian { get; set; }
    public List<JawabanButir> Jawabans { get; set; } = new();
}

public class JawabanButir
{
    public int Id { get; set; }
    public int PengerjaanUjianId { get; set; }
    public int ButirSoalId { get; set; }

    public int? OpsiId { get; set; }

    public string? JawabanTeks { get; set; }

    /// <summary>Nilai manual (0 sampai bobot) untuk essay. Null = belum dikoreksi.</summary>
    public decimal? NilaiManual { get; set; }

    public PengerjaanUjian? PengerjaanUjian { get; set; }
}
