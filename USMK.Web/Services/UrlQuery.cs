namespace USMK.Web.Services;

/// <summary>Pembaca query string dari URL navigasi Blazor (mis. ?ujian=5&amp;pesan=...).</summary>
public static class UrlQuery
{
    public static string? Baca(string uri, string nama)
    {
        var query = new Uri(uri).Query.TrimStart('?');

        foreach (var pasangan in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = pasangan.Split('=', 2);
            if (Uri.UnescapeDataString(kv[0]) != nama)
                continue;

            return kv.Length > 1 ? Uri.UnescapeDataString(kv[1]) : "";
        }

        return null;
    }

    public static int BacaInt(string uri, string nama) =>
        int.TryParse(Baca(uri, nama), out var nilai) ? nilai : 0;
}
