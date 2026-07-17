using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyObjectives : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyObjectiveSets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorldPlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayStartUtc = table.Column<DateTime>(type: "date", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateLastModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyObjectiveSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyObjectiveSets_WorldPlayers_WorldPlayerId",
                        column: x => x.WorldPlayerId,
                        principalTable: "WorldPlayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DailyObjectiveAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DailyObjectiveSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefinitionId = table.Column<int>(type: "int", nullable: false),
                    Slot = table.Column<int>(type: "int", nullable: false),
                    Target = table.Column<double>(type: "float", nullable: false),
                    Progress = table.Column<double>(type: "float", nullable: false),
                    IsComplete = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateLastModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyObjectiveAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyObjectiveAssignments_DailyObjectiveSets_DailyObjectiveSetId",
                        column: x => x.DailyObjectiveSetId,
                        principalTable: "DailyObjectiveSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyObjectiveAssignments_DailyObjectiveSetId_DefinitionId",
                table: "DailyObjectiveAssignments",
                columns: new[] { "DailyObjectiveSetId", "DefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyObjectiveAssignments_DailyObjectiveSetId_Slot",
                table: "DailyObjectiveAssignments",
                columns: new[] { "DailyObjectiveSetId", "Slot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyObjectiveSets_WorldPlayerId",
                table: "DailyObjectiveSets",
                column: "WorldPlayerId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyObjectiveAssignments");

            migrationBuilder.DropTable(
                name: "DailyObjectiveSets");
        }
    }
}
