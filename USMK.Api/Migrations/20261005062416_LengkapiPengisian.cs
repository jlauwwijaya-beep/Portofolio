using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace USMK.Api.Migrations
{
    /// <inheritdoc />
    public partial class LengkapiPengisian : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pengisians",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UjianId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    WaktuMulai = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WaktuSelesai = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Skor = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pengisians", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pengisians_Ujians_UjianId",
                        column: x => x.UjianId,
                        principalTable: "Ujians",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JawabanPesertas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PengisianId = table.Column<int>(type: "int", nullable: false),
                    SoalId = table.Column<int>(type: "int", nullable: false),
                    OpsiId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JawabanPesertas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JawabanPesertas_Opsis_OpsiId",
                        column: x => x.OpsiId,
                        principalTable: "Opsis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JawabanPesertas_Pengisians_PengisianId",
                        column: x => x.PengisianId,
                        principalTable: "Pengisians",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JawabanPesertas_Soals_SoalId",
                        column: x => x.SoalId,
                        principalTable: "Soals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JawabanPesertas_OpsiId",
                table: "JawabanPesertas",
                column: "OpsiId");

            migrationBuilder.CreateIndex(
                name: "IX_JawabanPesertas_PengisianId_SoalId",
                table: "JawabanPesertas",
                columns: new[] { "PengisianId", "SoalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JawabanPesertas_SoalId",
                table: "JawabanPesertas",
                column: "SoalId");

            migrationBuilder.CreateIndex(
                name: "IX_Pengisians_UjianId_UserId",
                table: "Pengisians",
                columns: new[] { "UjianId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JawabanPesertas");

            migrationBuilder.DropTable(
                name: "Pengisians");
        }
    }
}
