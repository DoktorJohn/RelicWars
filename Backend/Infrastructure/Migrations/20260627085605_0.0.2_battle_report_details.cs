using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _002_battle_report_details : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AppliedModifiersJson",
                table: "BattleReports",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AttackerLossesJson",
                table: "BattleReports",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DefenderLossesJson",
                table: "BattleReports",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RevivedUnitsJson",
                table: "BattleReports",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AppliedModifiersJson",
                table: "BattleReports");

            migrationBuilder.DropColumn(
                name: "AttackerLossesJson",
                table: "BattleReports");

            migrationBuilder.DropColumn(
                name: "DefenderLossesJson",
                table: "BattleReports");

            migrationBuilder.DropColumn(
                name: "RevivedUnitsJson",
                table: "BattleReports");
        }
    }
}
