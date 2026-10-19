namespace USMK.Web.Services;

/// <summary>
/// Meneruskan cookie sesi milik pengguna ke API USMK supaya API bisa
/// memvalidasi sesi yang sama. Tanpa ini, API hanya melihat request anonim.
///
/// Hanya cookie sesi yang diteruskan — cookie lain milik browser (misalnya
/// cookie antiforgery) tidak ikut keluar dari server.
///
/// Catatan: <see cref="IHttpContextAccessor"/> tidak selalu punya nilai saat
/// rendering interaktif, jadi ketersediaannya dicek dulu dan ketiadaannya
/// dilogkan supaya gampang didiagnosis, bukan diam-diam jadi request anonim.
/// </summary>
public sealed class ForwardAuthCookieHandler : DelegatingHandler
{
    public const string NamaCookieSesi = "usmk.auth";

    private readonly IHttpContextAccessor _accessor;
    private readonly ILogger<ForwardAuthCookieHandler> _log;

    public ForwardAuthCookieHandler(IHttpContextAccessor accessor, ILogger<ForwardAuthCookieHandler> log)
    {
        _accessor = accessor;
        _log = log;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var sesi = SesiPengguna();

        if (sesi is null)
            _log.LogDebug("HttpContext tidak tersedia saat memanggil {Url}; cookie sesi tidak diteruskan.",
                request.RequestUri);

        else if (!request.Headers.Contains("Cookie"))
            request.Headers.TryAddWithoutValidation("Cookie", $"{NamaCookieSesi}={sesi}");

        return base.SendAsync(request, cancellationToken);
    }

    /// <summary>
    /// Mengambil nilai cookie sesi saja dari header Cookie milik request browser.
    /// </summary>
    private string? SesiPengguna()
    {
        var header = _accessor.HttpContext?.Request.Headers.Cookie.ToString();
        if (string.IsNullOrWhiteSpace(header))
            return null;

        foreach (var bagian in header.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var pemisah = bagian.IndexOf('=');
            if (pemisah > 0 && string.Equals(bagian[..pemisah], NamaCookieSesi, StringComparison.Ordinal))
                return bagian[(pemisah + 1)..];
        }

        return null;
    }
}
