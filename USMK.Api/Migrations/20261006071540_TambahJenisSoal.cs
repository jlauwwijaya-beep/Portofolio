using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace USMK.Api.Migrations
{
    /// <inheritdoc />
    public partial class TambahJenisSoal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JawabanBenar",
                table: "Soals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Jenis",
                table: "Soals",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LabelSkalaMax",
                table: "Soals",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LabelSkalaMin",
                table: "Soals",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SkalaMax",
                table: "Soals",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SkalaMin",
                table: "Soals",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NilaiBenar",
                table: "Opsis",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JawabanTeks",
                table: "JawabanPesertas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Soals_Jenis",
                table: "Soals",
                column: "Jenis");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Soals_Jenis",
                table: "Soals");

            migrationBuilder.DropColumn(
                name: "JawabanBenar",
                table: "Soals");

            migrationBuilder.DropColumn(
                name: "Jenis",
                table: "Soals");

            migrationBuilder.DropColumn(
                name: "LabelSkalaMax",
                table: "Soals");

            migrationBuilder.DropColumn(
                name: "LabelSkalaMin",
                table: "Soals");

            migrationBuilder.DropColumn(
                name: "SkalaMax",
                table: "Soals");

            migrationBuilder.DropColumn(
                name: "SkalaMin",
                table: "Soals");

            migrationBuilder.DropColumn(
                name: "NilaiBenar",
                table: "Opsis");

            migrationBuilder.DropColumn(
                name: "JawabanTeks",
                table: "JawabanPesertas");
        }
    }
}
