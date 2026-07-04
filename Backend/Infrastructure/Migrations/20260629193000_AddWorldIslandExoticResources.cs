using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorldIslandExoticResources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorldIslandExoticResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorldIslandId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SlotIndex = table.Column<int>(type: "int", nullable: false),
                    ResourceType = table.Column<int>(type: "int", nullable: false),
                    Tier = table.Column<int>(type: "int", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateLastModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorldIslandExoticResources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorldIslandExoticResources_WorldIslands_WorldIslandId",
                        column: x => x.WorldIslandId,
                        principalTable: "WorldIslands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorldIslandExoticResources_Island_Slot",
                table: "WorldIslandExoticResources",
                columns: new[] { "WorldIslandId", "SlotIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorldIslandExoticResources_Island_Type",
                table: "WorldIslandExoticResources",
                columns: new[] { "WorldIslandId", "ResourceType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorldIslandExoticResources");
        }
    }
}
