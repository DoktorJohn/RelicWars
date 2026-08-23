using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceResearchPointsWithResearchRate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResearchPoints",
                table: "WorldPlayers");

            migrationBuilder.AddColumn<double>(
                name: "AppliedSpeedMultiplier",
                table: "Jobs",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastProgressAt",
                table: "Jobs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RemainingWorkSeconds",
                table: "Jobs",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TotalWorkSeconds",
                table: "Jobs",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AppliedSpeedMultiplier",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "LastProgressAt",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "RemainingWorkSeconds",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "TotalWorkSeconds",
                table: "Jobs");

            migrationBuilder.AddColumn<double>(
                name: "ResearchPoints",
                table: "WorldPlayers",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
