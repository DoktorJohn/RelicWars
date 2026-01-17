using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Alphav002 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlliancesAtWar",
                table: "Alliances");

            migrationBuilder.DropColumn(
                name: "AlliancesPacted",
                table: "Alliances");

            migrationBuilder.DropColumn(
                name: "Members",
                table: "Alliances");

            migrationBuilder.AddColumn<int>(
                name: "AllianceRole",
                table: "WorldPlayers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AllianceAlliance",
                columns: table => new
                {
                    AlliancesAtWarId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AlliancesPactedId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllianceAlliance", x => new { x.AlliancesAtWarId, x.AlliancesPactedId });
                    table.ForeignKey(
                        name: "FK_AllianceAlliance_Alliances_AlliancesAtWarId",
                        column: x => x.AlliancesAtWarId,
                        principalTable: "Alliances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AllianceAlliance_Alliances_AlliancesPactedId",
                        column: x => x.AlliancesPactedId,
                        principalTable: "Alliances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AllianceAlliance_AlliancesPactedId",
                table: "AllianceAlliance",
                column: "AlliancesPactedId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AllianceAlliance");

            migrationBuilder.DropColumn(
                name: "AllianceRole",
                table: "WorldPlayers");

            migrationBuilder.AddColumn<string>(
                name: "AlliancesAtWar",
                table: "Alliances",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AlliancesPacted",
                table: "Alliances",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Members",
                table: "Alliances",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
