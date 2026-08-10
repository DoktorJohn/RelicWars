using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCityEdicts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cities_WorldPlayerId",
                table: "Cities");

            migrationBuilder.AddColumn<int>(
                name: "ActiveEdict",
                table: "Cities",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EdictEnactedAtUtc",
                table: "Cities",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cities_WorldPlayer_ActiveEdict",
                table: "Cities",
                columns: new[] { "WorldPlayerId", "ActiveEdict" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cities_WorldPlayer_ActiveEdict",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "ActiveEdict",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "EdictEnactedAtUtc",
                table: "Cities");

            migrationBuilder.CreateIndex(
                name: "IX_Cities_WorldPlayerId",
                table: "Cities",
                column: "WorldPlayerId");
        }
    }
}
