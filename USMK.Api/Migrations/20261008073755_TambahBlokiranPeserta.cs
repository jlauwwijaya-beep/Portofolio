using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace USMK.Api.Migrations
{
    /// <inheritdoc />
    public partial class TambahBlokiranPeserta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BlokiranPesertas",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    JumlahKeluar = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Diblokir = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DiperbaruiPada = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlokiranPesertas", x => x.UserId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BlokiranPesertas_Diblokir",
                table: "BlokiranPesertas",
                column: "Diblokir");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlokiranPesertas");
        }
    }
}
