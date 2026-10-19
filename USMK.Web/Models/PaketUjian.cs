namespace USMK.Web.Models;

public record PaketUjianBankDto(
    int Id,
    string Judul,
    string? Deskripsi,
    string? MataPelajaran,
    string? Kelas,
    bool IsPublished,
    DateTime DibuatPada,
    int DurasiMenit,
    DateTime WaktuBuka,
    DateTime WaktuTutup,
    bool AcakUrutan,
    int JumlahBagian,
    int JumlahSoal,
    int JumlahPengerjaan,
    string StatusJadwal)
{
    /// <summary>Jadwalkan sentinel: paket tanpa jadwal (selalu terbuka).</summary>
    public bool PakaiJadwal =>
        !(WaktuTutup == default
          || WaktuBuka == new DateTime(2000, 1, 1) && WaktuTutup == new DateTime(2100, 1, 1));
}

public record OpsiButirDto(int Id, string Teks, int Urutan = 0, bool IsBenar = false, int? NilaiBenar = null);

public record ButirSoalDto(
    int Id,
    string Teks,
    string Jenis,
    bool Wajib,
    int Urutan,
    int SkalaMin,
    int SkalaMax,
    string? LabelSkalaMin,
    string? LabelSkalaMax,
    string? JawabanBenar,
    decimal Bobot,
    List<OpsiButirDto> Options);

public record BagianDto(
    int Id,
    string Judul,
    string? Deskripsi,
    int Urutan,
    List<ButirSoalDto> Soals);

public record PaketUjianDetailDto(
    int Id,
    string Judul,
    string? Deskripsi,
    string? MataPelajaran,
    string? Kelas,
    bool IsPublished,
    DateTime DibuatPada,
    int DurasiMenit,
    DateTime WaktuBuka,
    DateTime WaktuTutup,
    bool AcakUrutan,
    List<BagianDto> Bagians);

public record PaketRingkasDto(int Id, string Judul, string? MataPelajaran, string? Kelas, int DurasiMenit = 0,
    DateTime WaktuBuka = default, DateTime WaktuTutup = default);

public record PesertaPengerjaanDto(
    int PengerjaanId,
    string UserId,
    string? NamaSiswa,
    string? Kelas,
    string Status,
    DateTime WaktuMulai,
    DateTime? WaktuSelesai,
    decimal? Skor,
    int JumlahDijawab,
    bool BelumDitutup);

public record PesertaPaketResponseDto(
    PaketRingkasDto Paket,
    int JumlahPengerjaan,
    int JumlahDinilai,
    decimal? RataRata,
    decimal? Tertinggi,
    decimal? Terendah,
    List<PesertaPengerjaanDto> Pengerjaan);

public record SoalKerjaDto(
    int Id,
    string Teks,
    string Jenis,
    bool Wajib,
    int Urutan,
    int SkalaMin,
    int SkalaMax,
    string? LabelSkalaMin,
    string? LabelSkalaMax,
    List<OpsiButirDto> Options,
    int? OpsiId,
    List<int>? OpsiIds,
    string? TeksJawaban,
    int? Nilai,
    Dictionary<int, int>? Matrix);

public record BagianKerjaDto(
    int Id,
    string Judul,
    string? Deskripsi,
    int Urutan,
    List<SoalKerjaDto> Soals);

public record PengerjaanSoalDto(
    int PengerjaanId,
    string Judul,
    string? Deskripsi,
    string? MataPelajaran,
    string? Kelas,
    string? NamaSiswa,
    string Status,
    DateTime BatasWaktu,
    int SisaDetik,
    List<BagianKerjaDto> Bagians);

public record SoalPreviewDto(
    int Id,
    string Teks,
    string Jenis,
    bool Wajib,
    int Urutan,
    string Jawaban,
    bool Dijawab);

public record BagianPreviewDto(
    int Id,
    string Judul,
    string? Deskripsi,
    int Urutan,
    List<SoalPreviewDto> Soals);

public record PreviewPengerjaanDto(
    int PengerjaanId,
    string Judul,
    string? MataPelajaran,
    string? Kelas,
    string? NamaSiswa,
    string UserId,
    string Status,
    decimal? Skor,
    DateTime WaktuMulai,
    DateTime? WaktuSelesai,
    List<BagianPreviewDto> Bagians);

public record RiwayatPengerjaanDto(
    int PengerjaanId,
    int PaketUjianId,
    string Judul,
    string? MataPelajaran,
    string Status,
    DateTime WaktuMulai,
    DateTime? WaktuSelesai,
    decimal? Skor);

public record HasilPengerjaanDto(
    string Judul,
    string Status,
    decimal? Skor,
    int JumlahSoal,
    int JumlahDijawab,
    int JumlahBenar,
    int EssayDinilai,
    int EssayBelumDinilai,
    DateTime WaktuMulai,
    DateTime? WaktuSelesai)
{
    public bool WaktuHabis => string.Equals(Status, "WaktuHabis", StringComparison.OrdinalIgnoreCase);
}

// ---------- Koreksi essay (admin) ----------

public record SoalKoreksiDto(
    int ButirSoalId,
    string Teks,
    string Jenis,
    bool Wajib,
    decimal Bobot,
    string? Kunci,
    string Jawaban,
    bool Dijawab,
    bool? Benar,
    decimal? NilaiManual);

public record BagianKoreksiDto(
    int Id,
    string Judul,
    int Urutan,
    List<SoalKoreksiDto> Soals);

public record KoreksiPengerjaanDto(
    int PengerjaanId,
    string Judul,
    string? NamaSiswa,
    string? Kelas,
    string UserId,
    string Status,
    decimal? Skor,
    List<BagianKoreksiDto> Bagians);

// ---------- Request ----------

public record PaketUjianUpsertRequest(
    string Judul,
    string? Deskripsi,
    string? MataPelajaran,
    string? Kelas,
    int DurasiMenit = 0,
    DateTime WaktuBuka = default,
    DateTime WaktuTutup = default,
    bool AcakUrutan = true);

public record BagianUpsertRequest(string Judul, string? Deskripsi);

public record OpsiButirRequest(string Teks, bool IsBenar = false, int? NilaiBenar = null);

public record SoalPaketUpsertRequest(
    string Teks,
    string? Jenis,
    bool Wajib,
    List<OpsiButirRequest> Options,
    int? SkalaMin = null,
    int? SkalaMax = null,
    string? LabelSkalaMin = null,
    string? LabelSkalaMax = null,
    string? JawabanBenar = null,
    decimal Bobot = 1);

public record MulaiKerjakanRequest(
    int PaketUjianId,
    string UserId,
    string? NamaSiswa = null,
    string? Kelas = null);

public record SimpanJawabanButirRequest(
    int ButirSoalId,
    int? OpsiId = null,
    List<int>? OpsiIds = null,
    string? Teks = null,
    int? Nilai = null,
    Dictionary<int, int>? Matrix = null);

public record MulaiKerjakanResponse(int PengerjaanId, string Judul, DateTime BatasWaktu = default, int SisaDetik = 0);

public record KoreksiRequest(int ButirSoalId, decimal Nilai);

public record TutupKedaluwarsaRequest(List<int> PengerjaanIds);

public record JawabPesanResponse(string? Pesan, int SisaDetik = 0, decimal? Skor = null);

// ---------- Blokiran peserta (keluar ruang ujian berulang) ----------

public record BlokiranDto(string UserId, int JumlahKeluar, bool Diblokir, DateTime DiperbaruiPada);

public record CatatKeluarResponse(string UserId, int JumlahKeluar, bool Diblokir, int Batas);

public record IdPesanResponse(int Id, string? Pesan);