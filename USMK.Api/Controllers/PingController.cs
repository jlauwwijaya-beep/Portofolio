using Microsoft.AspNetCore.Mvc;

using System.ComponentModel.DataAnnotations;
namespace USMK.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class PingController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { pesan = "USMK API Alive", waktu = DateTime.Now });
    }

    [HttpPost]
    public IActionResult Post(SapaRequest request)
    {
        return Ok(new { pesan = $"Halo, {request.Nama}! Selamat Datang Di USMK." });
    }
}

public class SapaRequest
{
    [Required(ErrorMessage = "Nama wajib diisi")]
    [MinLength(3, ErrorMessage = "Nama minimal 3 huruf")]
    public string Nama { get; set; } = "";
}