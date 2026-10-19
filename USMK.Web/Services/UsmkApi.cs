using System.Net;
using System.Net.Http.Json;
using USMK.Web.Models;

namespace USMK.Web.Services;

/// <summary>
/// Satu-satunya tempat yang bicara dengan API USMK. Semua halaman memakai
/// layanan ini supaya tidak ada lagi penulisan HttpClient dan parsing error
/// yang berulang-ulang di tiap komponen.
/// </summary>
public sealed class UsmkApi(HttpClient http)
{
    // ---------- Paket Ujian (form builder) ----------

    public Task<ApiResult<List<PaketUjianBankDto>>> AmbilDaftarPaketAsync() =>
        Ambil<List<PaketUjianBankDto>>("/PaketUjian");

    public Task<ApiResult<PaketUjianDetailDto>> AmbilPaketAsync(int id) =>
        Ambil<PaketUjianDetailDto>($"/PaketUjian/{id}");

    public Task<ApiResult<IdPesanResponse>> BuatPaketAsync(PaketUjianUpsertRequest payload) =>
        Kirim<IdPesanResponse>(HttpMethod.Post, "/PaketUjian", payload);

    public Task<ApiResult<IdPesanResponse>> UbahPaketAsync(int id, PaketUjianUpsertRequest payload) =>
        Kirim<IdPesanResponse>(HttpMethod.Put, $"/PaketUjian/{id}", payload);

    public Task<ApiResult<IdPesanResponse>> DuplikatPaketAsync(int id) =>
        Kirim<IdPesanResponse>(HttpMethod.Post, $"/PaketUjian/{id}/duplikat", null);

    public Task<ApiResult> HapusPaketAsync(int id) => Kirim(HttpMethod.Delete, $"/PaketUjian/{id}");

    public Task<ApiResult> PublikasikanPaketAsync(int id) =>
        Kirim(HttpMethod.Post, $"/PaketUjian/{id}/publish");

    public Task<ApiResult> TutupPaketAsync(int id) =>
        Kirim(HttpMethod.Post, $"/PaketUjian/{id}/tutup");

    /// <summary>Rekap nilai per paket: statistik + daftar peserta beserta skor.</summary>
    public Task<ApiResult<PesertaPaketResponseDto>> AmbilPesertaPaketAsync(int id) =>
        Ambil<PesertaPaketResponseDto>($"/PaketUjian/{id}/peserta");

    /// <summary>Unduh rekap nilai peserta paket sebagai file Excel (.xlsx).</summary>
    public async Task<(bool Berhasil, byte[]? Data, string? Error)> UnduhRekapExcelAsync(int id)
    {
        try
        {
            using var resp = await KirimRaw(HttpMethod.Get, $"/PaketUjian/{id}/peserta/ekspor", null);
            if (!resp.IsSuccessStatusCode)
                return (false, null, await ApiError.BacaAsync(resp));

            return (true, await resp.Content.ReadAsByteArrayAsync(), null);
        }
        catch (Exception ex)
        {
            return (false, null, PesanJaringan(ex));
        }
    }

    /// <summary>Tutup &amp; nilai peserta yang waktunya sudah habis tapi masih berjalan.</summary>
    public Task<ApiResult> TutupKedaluwarsaPaketAsync(int paketId, TutupKedaluwarsaRequest payload) =>
        Kirim(HttpMethod.Post, $"/PaketUjian/{paketId}/tutup-kedaluwarsa", payload);

    // ---------- Bagian ----------

    public Task<ApiResult<IdPesanResponse>> TambahBagianAsync(int paketId, BagianUpsertRequest payload) =>
        Kirim<IdPesanResponse>(HttpMethod.Post, $"/PaketUjian/{paketId}/bagian", payload);

    public Task<ApiResult<IdPesanResponse>> UbahBagianAsync(int bagianId, BagianUpsertRequest payload) =>
        Kirim<IdPesanResponse>(HttpMethod.Put, $"/PaketUjian/bagian/{bagianId}", payload);

    public Task<ApiResult> HapusBagianAsync(int bagianId) =>
        Kirim(HttpMethod.Delete, $"/PaketUjian/bagian/{bagianId}");

    // ---------- Soal dalam bagian ----------

    public Task<ApiResult<IdPesanResponse>> TambahSoalPaketAsync(int bagianId, SoalPaketUpsertRequest payload) =>
        Kirim<IdPesanResponse>(HttpMethod.Post, $"/PaketUjian/bagian/{bagianId}/soal", payload);

    public Task<ApiResult<IdPesanResponse>> UbahSoalPaketAsync(int soalId, SoalPaketUpsertRequest payload) =>
        Kirim<IdPesanResponse>(HttpMethod.Put, $"/PaketUjian/soal/{soalId}", payload);

    public Task<ApiResult> HapusSoalPaketAsync(int soalId) =>
        Kirim(HttpMethod.Delete, $"/PaketUjian/soal/{soalId}");

    // ---------- Pengerjaan paket ujian (siswa) ----------

    public Task<ApiResult<MulaiKerjakanResponse>> MulaiKerjakanAsync(MulaiKerjakanRequest payload) =>
        Kirim<MulaiKerjakanResponse>(HttpMethod.Post, "/Pengerjaan/mulai", payload);

    public Task<ApiResult<PengerjaanSoalDto>> AmbilSoalKerjaAsync(int pengerjaanId) =>
        Ambil<PengerjaanSoalDto>($"/Pengerjaan/{pengerjaanId}");

    public Task<ApiResult<JawabPesanResponse>> SimpanJawabanButirAsync(int pengerjaanId, SimpanJawabanButirRequest payload) =>
        Kirim<JawabPesanResponse>(HttpMethod.Put, $"/Pengerjaan/{pengerjaanId}/jawaban", payload);

    public Task<ApiResult<JawabPesanResponse>> SelesaiKerjakanAsync(int pengerjaanId) =>
        Kirim<JawabPesanResponse>(HttpMethod.Post, $"/Pengerjaan/{pengerjaanId}/selesai", null);

    public Task<ApiResult<HasilPengerjaanDto>> AmbilHasilKerjaAsync(int pengerjaanId) =>
        Ambil<HasilPengerjaanDto>($"/Pengerjaan/{pengerjaanId}/hasil");

    public Task<ApiResult<PreviewPengerjaanDto>> AmbilPreviewKerjaAsync(int pengerjaanId) =>
        Ambil<PreviewPengerjaanDto>($"/Pengerjaan/{pengerjaanId}/preview");

    public Task<ApiResult<List<RiwayatPengerjaanDto>>> AmbilRiwayatKerjaAsync(string userId) =>
        Ambil<List<RiwayatPengerjaanDto>>($"/Pengerjaan/riwayat?userId={Uri.EscapeDataString(userId)}");

    // ---------- Koreksi essay (admin) ----------

    public Task<ApiResult<KoreksiPengerjaanDto>> AmbilKoreksiAsync(int pengerjaanId) =>
        Ambil<KoreksiPengerjaanDto>($"/Pengerjaan/{pengerjaanId}/koreksi");

    public Task<ApiResult<JawabPesanResponse>> SimpanKoreksiAsync(int pengerjaanId, KoreksiRequest payload) =>
        Kirim<JawabPesanResponse>(HttpMethod.Post, $"/Pengerjaan/{pengerjaanId}/koreksi", payload);

    public Task<ApiResult> HapusPengerjaanAsync(int pengerjaanId) =>
        Kirim(HttpMethod.Delete, $"/Pengerjaan/{pengerjaanId}");

    // ---------- Blokiran peserta ----------

    /// <summary>Catatan keluar milik satu peserta (angka nol kalau belum pernah keluar).</summary>
    public Task<ApiResult<BlokiranDto>> AmbilBlokiranAsync(string userId) =>
        Ambil<BlokiranDto>($"/Blokiran/{Uri.EscapeDataString(userId)}");

    /// <summary>Semua catatan keluar peserta, dipakai halaman pengelola.</summary>
    public Task<ApiResult<List<BlokiranDto>>> AmbilDaftarBlokiranAsync() =>
        Ambil<List<BlokiranDto>>("/Blokiran");

    /// <summary>Reset catatan peserta: penghitung di nol dan blokir dilepas.</summary>
    public Task<ApiResult> ResetBlokiranAsync(string userId) =>
        Kirim(HttpMethod.Post, $"/Blokiran/{Uri.EscapeDataString(userId)}/reset");

    /// <summary>Mencatat satu kali keluar dari ruang ujian saat halaman pengerjaan ditinggalkan.</summary>
    public Task<ApiResult<CatatKeluarResponse>> CatatKeluarAsync(int pengerjaanId) =>
        Kirim<CatatKeluarResponse>(HttpMethod.Post, $"/Pengerjaan/{pengerjaanId}/keluar", null);

    // ---------- Plumbing ----------

    private async Task<ApiResult<T>> Ambil<T>(string url)
    {
        try
        {
            using var resp = await http.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
                return ApiResult<T>.Gagal(await ApiError.BacaAsync(resp), resp.StatusCode);

            var data = await resp.Content.ReadFromJsonAsync<T>();
            return data is null
                ? ApiResult<T>.Gagal("Server mengembalikan data kosong.", resp.StatusCode)
                : ApiResult<T>.Sukses(data);
        }
        catch (Exception ex)
        {
            return GagalJaringan<T>(ex);
        }
    }

    private async Task<ApiResult<T>> Kirim<T>(HttpMethod method, string url, object? payload)
    {
        try
        {
            using var resp = await KirimRaw(method, url, payload);
            if (!resp.IsSuccessStatusCode)
                return ApiResult<T>.Gagal(await ApiError.BacaAsync(resp), resp.StatusCode);

            var data = await resp.Content.ReadFromJsonAsync<T>();
            return data is null
                ? ApiResult<T>.Gagal("Server mengembalikan data kosong.", resp.StatusCode)
                : ApiResult<T>.Sukses(data);
        }
        catch (Exception ex)
        {
            return GagalJaringan<T>(ex);
        }
    }

    private async Task<ApiResult> Kirim(HttpMethod method, string url, object? payload = null)
    {
        try
        {
            using var resp = await KirimRaw(method, url, payload);
            return resp.IsSuccessStatusCode
                ? ApiResult.Sukses()
                : ApiResult.Gagal(await ApiError.BacaAsync(resp), resp.StatusCode);
        }
        catch (Exception ex)
        {
            return ApiResult.Gagal(PesanJaringan(ex), ex is TaskCanceledException
                ? HttpStatusCode.RequestTimeout
                : HttpStatusCode.ServiceUnavailable);
        }
    }

    private Task<HttpResponseMessage> KirimRaw(HttpMethod method, string url, object? payload) =>
        payload is null
            ? http.SendAsync(new HttpRequestMessage(method, url))
            : http.SendAsync(new HttpRequestMessage(method, url) { Content = JsonContent.Create(payload) });

    private static ApiResult<T> GagalJaringan<T>(Exception ex) =>
        ApiResult<T>.Gagal(PesanJaringan(ex), ex is TaskCanceledException
            ? HttpStatusCode.RequestTimeout
            : HttpStatusCode.ServiceUnavailable);

    private static string PesanJaringan(Exception ex) => ex switch
    {
        TaskCanceledException => "Server API tidak merespons. Pastikan API USMK sedang berjalan.",
        HttpRequestException => "Tidak bisa menghubungi server API. Pastikan API USMK sedang berjalan.",
        _ => $"Gagal menghubungi server API: {ex.Message}"
    };
}