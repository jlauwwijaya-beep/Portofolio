using System.ComponentModel.DataAnnotations;

namespace USMK.Web.Models;

public sealed class LoginInput
{
    [Required(ErrorMessage = "Email wajib diisi")]
    [EmailAddress(ErrorMessage = "Format email tidak valid")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Password wajib diisi")]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }
}

public sealed class RegisterInput
{
    [Required(ErrorMessage = "Nama lengkap wajib diisi")]
    [StringLength(120, ErrorMessage = "Nama terlalu panjang")]
    public string NamaLengkap { get; set; } = "";

    [Required(ErrorMessage = "NIS wajib diisi")]
    [StringLength(40, ErrorMessage = "NIS terlalu panjang")]
    public string Nis { get; set; } = "";

    [StringLength(60, ErrorMessage = "Nama kelas terlalu panjang")]
    public string? Kelas { get; set; }

    [Required(ErrorMessage = "Email wajib diisi")]
    [EmailAddress(ErrorMessage = "Format email tidak valid")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Password wajib diisi")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password minimal 8 karakter")]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }
}