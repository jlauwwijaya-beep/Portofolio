using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace USMK.Api.Migrations
{
    /// <inheritdoc />
    public partial class GabungUjian : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Skor",
                table: "PengerjaanUjians",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UrutanAcak",
                table: "PengerjaanUjians",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AcakUrutan",
                table: "PaketUjians",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "DurasiMenit",
                table: "PaketUjians",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "WaktuBuka",
                table: "PaketUjians",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "WaktuTutup",
                table: "PaketUjians",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsBenar",
                table: "OpsiButirs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "NilaiBenar",
                table: "OpsiButirs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NilaiManual",
                table: "JawabanButirs",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Bobot",
                table: "ButirSoals",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<string>(
                name: "JawabanBenar",
                table: "ButirSoals",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Skor",
                table: "PengerjaanUjians");

            migrationBuilder.DropColumn(
                name: "UrutanAcak",
                table: "PengerjaanUjians");

            migrationBuilder.DropColumn(
                name: "AcakUrutan",
                table: "PaketUjians");

            migrationBuilder.DropColumn(
                name: "DurasiMenit",
                table: "PaketUjians");

            migrationBuilder.DropColumn(
                name: "WaktuBuka",
                table: "PaketUjians");

            migrationBuilder.DropColumn(
                name: "WaktuTutup",
                table: "PaketUjians");

            migrationBuilder.DropColumn(
                name: "IsBenar",
                table: "OpsiButirs");

            migrationBuilder.DropColumn(
                name: "NilaiBenar",
                table: "OpsiButirs");

            migrationBuilder.DropColumn(
                name: "NilaiManual",
                table: "JawabanButirs");

            migrationBuilder.DropColumn(
                name: "Bobot",
                table: "ButirSoals");

            migrationBuilder.DropColumn(
                name: "JawabanBenar",
                table: "ButirSoals");
        }
    }
}
