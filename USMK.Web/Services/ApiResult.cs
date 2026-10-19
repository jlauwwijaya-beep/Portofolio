using System.Net;
using System.Text.Json;

namespace USMK.Web.Services;

public sealed record ApiResult(bool Berhasil, string? Error, HttpStatusCode Status)
{
    public static ApiResult Sukses() => new(true, null, HttpStatusCode.OK);

    public static ApiResult Gagal(string error, HttpStatusCode status) => new(false, error, status);

    public bool Adalah(HttpStatusCode status) => Status == status;

    public bool WaktuSudahHabis => Adalah(HttpStatusCode.Conflict);
}

public sealed record ApiResult<T>(bool Berhasil, T? Data, string? Error, HttpStatusCode Status)
{
    public static ApiResult<T> Sukses(T data) => new(true, data, null, HttpStatusCode.OK);

    public static ApiResult<T> Gagal(string error, HttpStatusCode status) => new(false, default, error, status);

    public bool Adalah(HttpStatusCode status) => Status == status;

    public bool WaktuSudahHabis => Adalah(HttpStatusCode.Conflict);

    public ApiResult TanpaData() => new(Berhasil, Error, Status);
}

public static class ApiError
{
    public static async Task<string> BacaAsync(HttpResponseMessage resp)
    {
        var isi = await resp.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(isi))
            return PesanBawaan(resp.StatusCode);

        if (CobaParse(isi, out var pesan))
            return pesan!;

        return isi.Length > 400 ? isi[..400] + "..." : isi;
    }

    private static bool CobaParse(string isi, out string? pesan)
    {
        pesan = null;

        try
        {
            using var doc = JsonDocument.Parse(isi, new JsonDocumentOptions { AllowTrailingCommas = true });
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return false;

            var root = doc.RootElement;

            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                var daftar = errors.EnumerateObject()
                    .SelectMany(p => p.Value.ValueKind == JsonValueKind.Array
                        ? p.Value.EnumerateArray().Select(e => e.GetString())
                        : new[] { p.Value.GetString() })
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();

                if (daftar.Count > 0)
                {
                    pesan = string.Join(" ", daftar);
                    return true;
                }
            }

            foreach (var nama in new[] { "detail", "message", "title", "error" })
            {
                if (root.TryGetProperty(nama, out var nilai) && nilai.ValueKind == JsonValueKind.String)
                {
                    var teks = nilai.GetString();
                    if (!string.IsNullOrWhiteSpace(teks))
                    {
                        pesan = teks;
                        return true;
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Body bukan JSON, ditampilkan apa adanya.
        }

        return false;
    }

    private static string PesanBawaan(HttpStatusCode status) => status switch
    {
        HttpStatusCode.NotFound => "Data tidak ditemukan.",
        HttpStatusCode.Conflict => "Waktu ujian sudah habis.",
        HttpStatusCode.Forbidden => "Kamu tidak punya akses ke data ini.",
        HttpStatusCode.Unauthorized => "Sesi kamu sudah berakhir. Silakan login ulang.",
        _ => $"Terjadi kesalahan di server ({(int)status})."
    };
}