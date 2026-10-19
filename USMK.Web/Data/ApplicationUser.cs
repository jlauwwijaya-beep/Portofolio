using Microsoft.AspNetCore.Identity;

namespace USMK.Web.Data;

public class ApplicationUser : IdentityUser
{
    public string? NamaLengkap { get; set; }

    public string? Nis { get; set; }

    public string? Kelas { get; set; }
}