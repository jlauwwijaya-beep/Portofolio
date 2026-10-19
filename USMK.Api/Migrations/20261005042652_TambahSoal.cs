using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace USMK.Api.Migrations
{
    /// <inheritdoc />
    public partial class TambahSoal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Soals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Kategori = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DibuatPada = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Soals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Opsis",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SoalId = table.Column<int>(type: "int", nullable: false),
                    Teks = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsBenar = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Opsis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Opsis_Soals_SoalId",
                        column: x => x.SoalId,
                        principalTable: "Soals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Opsis_SoalId",
                table: "Opsis",
                column: "SoalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Opsis");

            migrationBuilder.DropTable(
                name: "Soals");
        }
    }
}
