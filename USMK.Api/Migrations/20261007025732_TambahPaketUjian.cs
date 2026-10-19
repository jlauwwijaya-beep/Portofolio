using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace USMK.Api.Migrations
{
    /// <inheritdoc />
    public partial class TambahPaketUjian : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaketUjians",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Judul = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Deskripsi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MataPelajaran = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Kelas = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    DibuatPada = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaketUjians", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Bagians",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaketUjianId = table.Column<int>(type: "int", nullable: false),
                    Judul = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Deskripsi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Urutan = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bagians", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bagians_PaketUjians_PaketUjianId",
                        column: x => x.PaketUjianId,
                        principalTable: "PaketUjians",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PengerjaanUjians",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaketUjianId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NamaSiswa = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Kelas = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    WaktuMulai = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WaktuSelesai = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PengerjaanUjians", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PengerjaanUjians_PaketUjians_PaketUjianId",
                        column: x => x.PaketUjianId,
                        principalTable: "PaketUjians",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ButirSoals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BagianId = table.Column<int>(type: "int", nullable: false),
                    Teks = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Jenis = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Wajib = table.Column<bool>(type: "bit", nullable: false),
                    Urutan = table.Column<int>(type: "int", nullable: false),
                    SkalaMin = table.Column<int>(type: "int", nullable: false),
                    SkalaMax = table.Column<int>(type: "int", nullable: false),
                    LabelSkalaMin = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LabelSkalaMax = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ButirSoals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ButirSoals_Bagians_BagianId",
                        column: x => x.BagianId,
                        principalTable: "Bagians",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OpsiButirs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ButirSoalId = table.Column<int>(type: "int", nullable: false),
                    Teks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Urutan = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpsiButirs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpsiButirs_ButirSoals_ButirSoalId",
                        column: x => x.ButirSoalId,
                        principalTable: "ButirSoals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JawabanButirs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PengerjaanUjianId = table.Column<int>(type: "int", nullable: false),
                    ButirSoalId = table.Column<int>(type: "int", nullable: false),
                    OpsiId = table.Column<int>(type: "int", nullable: true),
                    JawabanTeks = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JawabanButirs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JawabanButirs_ButirSoals_ButirSoalId",
                        column: x => x.ButirSoalId,
                        principalTable: "ButirSoals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JawabanButirs_OpsiButirs_OpsiId",
                        column: x => x.OpsiId,
                        principalTable: "OpsiButirs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JawabanButirs_PengerjaanUjians_PengerjaanUjianId",
                        column: x => x.PengerjaanUjianId,
                        principalTable: "PengerjaanUjians",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bagians_PaketUjianId",
                table: "Bagians",
                column: "PaketUjianId");

            migrationBuilder.CreateIndex(
                name: "IX_ButirSoals_BagianId",
                table: "ButirSoals",
                column: "BagianId");

            migrationBuilder.CreateIndex(
                name: "IX_JawabanButirs_ButirSoalId",
                table: "JawabanButirs",
                column: "ButirSoalId");

            migrationBuilder.CreateIndex(
                name: "IX_JawabanButirs_OpsiId",
                table: "JawabanButirs",
                column: "OpsiId");

            migrationBuilder.CreateIndex(
                name: "IX_JawabanButirs_PengerjaanUjianId_ButirSoalId",
                table: "JawabanButirs",
                columns: new[] { "PengerjaanUjianId", "ButirSoalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpsiButirs_ButirSoalId",
                table: "OpsiButirs",
                column: "ButirSoalId");

            migrationBuilder.CreateIndex(
                name: "IX_PengerjaanUjians_PaketUjianId_UserId",
                table: "PengerjaanUjians",
                columns: new[] { "PaketUjianId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JawabanButirs");

            migrationBuilder.DropTable(
                name: "OpsiButirs");

            migrationBuilder.DropTable(
                name: "PengerjaanUjians");

            migrationBuilder.DropTable(
                name: "ButirSoals");

            migrationBuilder.DropTable(
                name: "Bagians");

            migrationBuilder.DropTable(
                name: "PaketUjians");
        }
    }
}
