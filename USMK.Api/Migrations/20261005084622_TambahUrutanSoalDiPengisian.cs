using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace USMK.Api.Migrations
{
    /// <inheritdoc />
    public partial class TambahUrutanSoalDiPengisian : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UrutanSoal",
                table: "Pengisians",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UrutanSoal",
                table: "Pengisians");
        }
    }
}
