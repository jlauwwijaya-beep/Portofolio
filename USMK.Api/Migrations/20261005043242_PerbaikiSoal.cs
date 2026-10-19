using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace USMK.Api.Migrations
{
    /// <inheritdoc />
    public partial class PerbaikiSoal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Text",
                table: "Soals",
                newName: "Teks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Teks",
                table: "Soals",
                newName: "Text");
        }
    }
}
