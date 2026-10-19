using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace USMK.Web.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.NamaLengkap).HasMaxLength(120);
            entity.Property(u => u.Nis).HasMaxLength(40);
            entity.Property(u => u.Kelas).HasMaxLength(60);

            entity.HasIndex(u => u.Nis).IsUnique();
        });
    }
}