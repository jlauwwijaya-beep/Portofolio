using Microsoft.EntityFrameworkCore;
using USMK.Api.Models;

namespace USMK.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Soal> Soals => Set<Soal>();
    public DbSet<Opsi> Opsis => Set<Opsi>();
    public DbSet<Ujian> Ujians => Set<Ujian>();
    public DbSet<UjianSoal> UjianSoals => Set<UjianSoal>();
    public DbSet<Pengisian> Pengisians => Set<Pengisian>();
    public DbSet<JawabanPeserta> JawabanPesertas => Set<JawabanPeserta>();

    public DbSet<PaketUjian> PaketUjians => Set<PaketUjian>();
    public DbSet<Bagian> Bagians => Set<Bagian>();
    public DbSet<ButirSoal> ButirSoals => Set<ButirSoal>();
    public DbSet<OpsiButir> OpsiButirs => Set<OpsiButir>();
    public DbSet<PengerjaanUjian> PengerjaanUjians => Set<PengerjaanUjian>();
    public DbSet<JawabanButir> JawabanButirs => Set<JawabanButir>();
    public DbSet<BlokiranPeserta> BlokiranPesertas => Set<BlokiranPeserta>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // ===== Soal =====
        b.Entity<Soal>().Property(s => s.Jenis).HasMaxLength(30).IsRequired();
        b.Entity<Soal>().Property(s => s.LabelSkalaMin).HasMaxLength(60);
        b.Entity<Soal>().Property(s => s.LabelSkalaMax).HasMaxLength(60);

        b.Entity<Soal>()
            .HasIndex(s => s.Jenis);

        // ===== Ujian & soal =====
        b.Entity<UjianSoal>().HasKey(x => new { x.UjianId, x.SoalId });
        b.Entity<UjianSoal>().Property(x => x.Bobot).HasPrecision(5, 2);

        b.Entity<UjianSoal>()
            .HasOne(x => x.Soal)
            .WithMany()
            .HasForeignKey(x => x.SoalId)
            .OnDelete(DeleteBehavior.Restrict);

        // ===== Pengisian =====
        b.Entity<Pengisian>().Property(x => x.Skor).HasPrecision(6, 2);

        // Status disimpan sebagai teks ("Berjalan"), bukan angka
        b.Entity<Pengisian>().Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

        // Satu peserta hanya boleh mengerjakan satu ujian satu kali
        b.Entity<Pengisian>().HasIndex(x => new { x.UjianId, x.UserId }).IsUnique();

        b.Entity<Pengisian>()
            .HasOne(x => x.Ujian)
            .WithMany()
            .HasForeignKey(x => x.UjianId)
            .OnDelete(DeleteBehavior.Restrict);

        // ===== Jawaban =====
        b.Entity<JawabanPeserta>().HasIndex(x => new { x.PengisianId, x.SoalId }).IsUnique();

        b.Entity<JawabanPeserta>()
            .HasOne<Soal>().WithMany()
            .HasForeignKey(x => x.SoalId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<JawabanPeserta>()
            .HasOne<Opsi>().WithMany()
            .HasForeignKey(x => x.OpsiId)
            .OnDelete(DeleteBehavior.Restrict);

        // ===== Paket Ujian (form builder) =====
        b.Entity<PaketUjian>().Property(x => x.Judul).HasMaxLength(200).IsRequired();
        b.Entity<PaketUjian>().Property(x => x.MataPelajaran).HasMaxLength(120);
        b.Entity<PaketUjian>().Property(x => x.Kelas).HasMaxLength(60);
        b.Entity<PaketUjian>().Property(x => x.DurasiMenit).HasDefaultValue(0);
        b.Entity<PaketUjian>().Property(x => x.AcakUrutan).HasDefaultValue(true);

        b.Entity<Bagian>().Property(x => x.Judul).HasMaxLength(200).IsRequired();
        b.Entity<Bagian>()
            .HasOne(x => x.PaketUjian)
            .WithMany(p => p.Bagians)
            .HasForeignKey(x => x.PaketUjianId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<ButirSoal>().Property(x => x.Teks).HasMaxLength(2000).IsRequired();
        b.Entity<ButirSoal>().Property(x => x.Jenis).HasMaxLength(30).IsRequired();
        b.Entity<ButirSoal>().Property(x => x.JawabanBenar).HasMaxLength(2000);
        b.Entity<ButirSoal>().Property(x => x.Bobot).HasPrecision(5, 2).HasDefaultValue(1m);
        b.Entity<ButirSoal>()
            .HasOne(x => x.Bagian)
            .WithMany(g => g.Soals)
            .HasForeignKey(x => x.BagianId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<OpsiButir>().Property(x => x.Teks).HasMaxLength(1000).IsRequired();
        b.Entity<OpsiButir>()
            .HasOne(x => x.ButirSoal)
            .WithMany(s => s.Options)
            .HasForeignKey(x => x.ButirSoalId)
            .OnDelete(DeleteBehavior.Cascade);

        // ===== Pengerjaan ujian (transaksi) =====
        b.Entity<PengerjaanUjian>().Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Entity<PengerjaanUjian>().Property(x => x.NamaSiswa).HasMaxLength(120);
        b.Entity<PengerjaanUjian>().Property(x => x.Kelas).HasMaxLength(60);
        b.Entity<PengerjaanUjian>().Property(x => x.Skor).HasPrecision(6, 2);
        b.Entity<PengerjaanUjian>().Property(x => x.UrutanAcak).HasMaxLength(8000);

        b.Entity<JawabanButir>().Property(x => x.NilaiManual).HasPrecision(5, 2);

        // Satu siswa hanya mengerjakan satu paket satu kali
        b.Entity<PengerjaanUjian>().HasIndex(x => new { x.PaketUjianId, x.UserId }).IsUnique();

        b.Entity<PengerjaanUjian>()
            .HasOne(x => x.PaketUjian)
            .WithMany()
            .HasForeignKey(x => x.PaketUjianId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<JawabanButir>().HasIndex(x => new { x.PengerjaanUjianId, x.ButirSoalId }).IsUnique();

        b.Entity<JawabanButir>()
            .HasOne(x => x.PengerjaanUjian)
            .WithMany(r => r.Jawabans)
            .HasForeignKey(x => x.PengerjaanUjianId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<JawabanButir>()
            .HasOne<ButirSoal>()
            .WithMany()
            .HasForeignKey(x => x.ButirSoalId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<JawabanButir>()
            .HasOne<OpsiButir>()
            .WithMany()
            .HasForeignKey(x => x.OpsiId)
            .OnDelete(DeleteBehavior.Restrict);

        // ===== Blokiran peserta (keluar ruang ujian berulang) =====
        b.Entity<BlokiranPeserta>().HasKey(x => x.UserId);
        b.Entity<BlokiranPeserta>().Property(x => x.UserId).HasMaxLength(45).ValueGeneratedNever();
        b.Entity<BlokiranPeserta>().Property(x => x.JumlahKeluar).HasDefaultValue(0);
        b.Entity<BlokiranPeserta>().Property(x => x.Diblokir).HasDefaultValue(false);
        b.Entity<BlokiranPeserta>().HasIndex(x => x.Diblokir);
    }
}