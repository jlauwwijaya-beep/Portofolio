using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace USMK.Api.Migrations
{
    /// <inheritdoc />
    public partial class TambahUjian : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Ujians",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Judul = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Deskripsi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DurasiMenit = table.Column<int>(type: "int", nullable: false),
                    WaktuBuka = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WaktuTutup = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ujians", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UjianSoals",
                columns: table => new
                {
                    UjianId = table.Column<int>(type: "int", nullable: false),
                    SoalId = table.Column<int>(type: "int", nullable: false),
                    Urutan = table.Column<int>(type: "int", nullable: false),
                    Bobot = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UjianSoals", x => new { x.UjianId, x.SoalId });
                    table.ForeignKey(
                        name: "FK_UjianSoals_Soals_SoalId",
                        column: x => x.SoalId,
                        principalTable: "Soals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UjianSoals_Ujians_UjianId",
                        column: x => x.UjianId,
                        principalTable: "Ujians",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UjianSoals_SoalId",
                table: "UjianSoals",
                column: "SoalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UjianSoals");

            migrationBuilder.DropTable(
                name: "Ujians");
        }
    }
}
