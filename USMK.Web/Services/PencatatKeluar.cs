namespace USMK.Web.Services;

/// <summary>
/// Mencatat satu kali keluar dari ruang ujian ketika halaman pengerjaan
/// ditinggalkan (pindah halaman, refresh, atau tutup tab).
///
/// Pemanggilan terjadi dari <c>Dispose()</c> komponen, sehingga HttpClient milik
/// scope render yang mungkin sudah dibuang tidak boleh dipakai. Layanan ini
/// singleton dan selalu membuat HttpClient baru dari <see cref="IHttpClientFactory"/>,
/// yang aman dipanggil bahkan ketika circuit sedang ditutup.
/// </summary>
public sealed class PencatatKeluar(IHttpClientFactory factory, ILogger<PencatatKeluar> log)
{
    /// <summary>Nama client HTTP khusus pencatat keluar (daftarkan di Program.cs).</summary>
    public const string NamaKlien = "PencatatKeluar";

    public async Task CatatAsync(int pengerjaanId)
    {
        try
        {
            var http = factory.CreateClient(NamaKlien);

            using var resp = await http.PostAsync($"Pengerjaan/{pengerjaanId}/keluar", null);

            if (!resp.IsSuccessStatusCode)
                log.LogWarning("Gagal mencatat keluar ujian #{PengerjaanId}: HTTP {Status}.",
                    pengerjaanId, (int)resp.StatusCode);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Gagal mencatat keluar ujian #{PengerjaanId}.", pengerjaanId);
        }
    }
}
